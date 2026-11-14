using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Askaiser.Marionette.Imaging;

/// <summary>
/// Writes animated PNG files (APNG), the format of the videos of the screen.
/// </summary>
/// <remarks>
/// <para>
/// An animated PNG is a PNG file with additional chunks. Every frame is a rectangle of pixels, compressed on its own, that is drawn over
/// the previous ones: a frame only has to cover what changed, and pixels that did not change inside of it are transparent, which compresses
/// to almost nothing. A screen mostly stays the same from one frame to the next, so a video is barely larger than a screenshot.
/// </para>
/// <para>
/// The format was chosen over GIF, which is limited to 256 colors and compresses poorly, and over actual video formats, which need an
/// encoder that .NET does not provide and blur text. It is lossless, and web browsers play it.
/// </para>
/// <para>
/// The image that a PNG file normally contains is still there. It is not part of the animation: image viewers that do not play animations
/// show it instead, see <see cref="WriteDefaultImage"/>.
/// </para>
/// </remarks>
internal static class AnimatedPng
{
    /// <summary>Web browsers slow down frames that are displayed for a shorter time than this.</summary>
    public static readonly TimeSpan MinimumDelay = TimeSpan.FromMilliseconds(20);

    private const int BytesPerPixel = 4;

    /// <summary>The "Up" filter of PNG, which stores the difference between a row and the one above. It is cheap and very effective on screens.</summary>
    private const byte UpFilter = 2;

    private const uint AlphaMask = 0xFF000000u;

    /// <summary>
    /// The second level of zlib. On screens, it makes videos a quarter to a third smaller than the first one, for one to three
    /// more milliseconds when a whole 1920x1080 screen changes. Higher levels take much longer for a few percent.
    /// </summary>
    private static readonly ZLibCompressionOptions Compression = new() { CompressionLevel = 2 };

    /// <summary>
    /// Writes the beginning of a file: the size of the animation and its number of frames.
    /// </summary>
    public static void WriteHeader(Stream destination, int width, int height, int frameCount)
    {
        destination.Write(ImageCodec.PngSignature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.Slice(4), height);
        header[8] = 8; // bit depth
        header[9] = 6; // color type: RGBA
        header[10] = 0; // compression method
        header[11] = 0; // filter method
        header[12] = 0; // no interlacing
        ImageCodec.WriteChunk(destination, "IHDR"u8, header);

        Span<byte> animation = stackalloc byte[8];
        BinaryPrimitives.WriteInt32BigEndian(animation, frameCount);
        BinaryPrimitives.WriteInt32BigEndian(animation.Slice(4), 0); // play in a loop
        ImageCodec.WriteChunk(destination, "acTL"u8, animation);
    }

    /// <summary>
    /// Writes the image displayed by programs that do not play animations. It has the size of the animation and is not one of its frames.
    /// </summary>
    public static void WriteDefaultImage(Stream destination, ReadOnlySpan<byte> compressedPixels)
    {
        ImageCodec.WriteChunk(destination, "IDAT"u8, compressedPixels);
    }

    /// <summary>
    /// Writes a frame of the animation.
    /// </summary>
    /// <param name="destination">The file.</param>
    /// <param name="sequence">The number of animation chunks written so far, which this method updates.</param>
    /// <param name="left">The position of the frame in the animation.</param>
    /// <param name="top">The position of the frame in the animation.</param>
    /// <param name="width">The width of the frame.</param>
    /// <param name="height">The height of the frame.</param>
    /// <param name="delay">How long the frame is displayed.</param>
    /// <param name="clearAfter">Whether the area of the frame becomes transparent once its delay has elapsed.</param>
    /// <param name="compressedPixels">The pixels of the frame, as returned by the compression methods of this class.</param>
    public static void WriteFrame(Stream destination, ref int sequence, int left, int top, int width, int height, TimeSpan delay, bool clearAfter, ReadOnlySpan<byte> compressedPixels)
    {
        // Delays are fractions of a second. Milliseconds fit in 16 bits up to about a minute, tenths of a second beyond.
        var milliseconds = (long)Math.Round(Math.Max(delay.TotalMilliseconds, MinimumDelay.TotalMilliseconds));
        var (numerator, denominator) = milliseconds <= ushort.MaxValue
            ? ((ushort)milliseconds, (ushort)1000)
            : ((ushort)Math.Min(milliseconds / 100, ushort.MaxValue), (ushort)10);

        Span<byte> control = stackalloc byte[26];
        BinaryPrimitives.WriteInt32BigEndian(control, sequence++);
        BinaryPrimitives.WriteInt32BigEndian(control.Slice(4), width);
        BinaryPrimitives.WriteInt32BigEndian(control.Slice(8), height);
        BinaryPrimitives.WriteInt32BigEndian(control.Slice(12), left);
        BinaryPrimitives.WriteInt32BigEndian(control.Slice(16), top);
        BinaryPrimitives.WriteUInt16BigEndian(control.Slice(20), numerator);
        BinaryPrimitives.WriteUInt16BigEndian(control.Slice(22), denominator);
        control[24] = (byte)(clearAfter ? 1 : 0); // dispose operation: none or background
        control[25] = 1; // blend operation: over, transparent pixels keep what is underneath
        ImageCodec.WriteChunk(destination, "fcTL"u8, control);

        Span<byte> dataSequence = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(dataSequence, sequence++);
        ImageCodec.WriteChunk(destination, "fdAT"u8, dataSequence, compressedPixels);
    }

    public static void WriteEnd(Stream destination)
    {
        ImageCodec.WriteChunk(destination, "IEND"u8, ReadOnlySpan<byte>.Empty);
    }

    /// <summary>
    /// Compresses a region of a BGRA image. When a previous image is given, pixels that are the same in both images become transparent.
    /// </summary>
    /// <param name="bgra">The image, of which <paramref name="stride"/> is the number of bytes per row.</param>
    /// <param name="previousBgra">The image displayed before, with the same size, or an empty span to write every pixel.</param>
    /// <param name="stride">The number of bytes per row of the images.</param>
    /// <param name="left">The left of the region.</param>
    /// <param name="top">The top of the region.</param>
    /// <param name="width">The width of the region.</param>
    /// <param name="height">The height of the region.</param>
    /// <param name="destination">Receives the compressed pixels.</param>
    public static void CompressRegion(ReadOnlySpan<byte> bgra, ReadOnlySpan<byte> previousBgra, int stride, int left, int top, int width, int height, Stream destination)
    {
        Compress(bgra, previousBgra, stride, left, top, width, height, width, height, isRgba: false, destination);
    }

    /// <summary>
    /// Compresses a BGRA image, surrounded on its right and at its bottom by transparent pixels up to a larger size.
    /// </summary>
    public static void CompressPadded(ReadOnlySpan<byte> bgra, int width, int height, int paddedWidth, int paddedHeight, Stream destination)
    {
        Compress(bgra, default, width * BytesPerPixel, 0, 0, width, height, paddedWidth, paddedHeight, isRgba: false, destination);
    }

    /// <summary>
    /// Compresses an image that is already in the RGBA layout of PNG, like the one <see cref="Apply"/> draws on.
    /// </summary>
    public static void CompressRgba(ReadOnlySpan<byte> rgba, int width, int height, Stream destination)
    {
        Compress(rgba, default, width * BytesPerPixel, 0, 0, width, height, width, height, isRgba: true, destination);
    }

    /// <summary>
    /// Compresses transparent pixels.
    /// </summary>
    public static void CompressTransparent(int width, int height, Stream destination)
    {
        Compress(default, default, 0, 0, 0, 0, 0, width, height, isRgba: true, destination);
    }

    /// <summary>
    /// Copies an opaque BGRA image to the RGBA layout of PNG.
    /// </summary>
    public static void ConvertToRgba(ReadOnlySpan<byte> bgra, Span<byte> rgba)
    {
        ConvertRow(bgra, default, rgba);
    }

    /// <summary>
    /// Draws compressed pixels over an RGBA image, like a program that plays the animation does: transparent pixels keep what is underneath.
    /// </summary>
    /// <param name="compressedPixels">The pixels of a region, as returned by the compression methods of this class.</param>
    /// <param name="rgba">The image to draw on, of which <paramref name="stride"/> is the number of bytes per row.</param>
    /// <param name="stride">The number of bytes per row of the image.</param>
    /// <param name="left">The left of the region.</param>
    /// <param name="top">The top of the region.</param>
    /// <param name="width">The width of the region.</param>
    /// <param name="height">The height of the region.</param>
    public static unsafe void Apply(ReadOnlySpan<byte> compressedPixels, Span<byte> rgba, int stride, int left, int top, int width, int height)
    {
        var rowLength = width * BytesPerPixel;
        var buffer = ArrayPool<byte>.Shared.Rent((rowLength * 2) + 1);

        try
        {
            // The filter of the first row refers to a row above made of zeros.
            var row = buffer.AsSpan(0, rowLength);
            var filtered = buffer.AsSpan(rowLength, rowLength + 1);
            row.Clear();

            fixed (byte* compressed = compressedPixels)
            {
                using var source = new UnmanagedMemoryStream(compressed, compressedPixels.Length);
                using var zlib = new ZLibStream(source, CompressionMode.Decompress);

                for (var y = 0; y < height; y++)
                {
                    zlib.ReadExactly(filtered);
                    if (filtered[0] != UpFilter)
                    {
                        throw new InvalidDataException();
                    }

                    // Adding the differences to the row above gives the row, which is the row above of the next one.
                    Add(row, filtered.Slice(1));
                    Blend(row, rgba.Slice(((top + y) * stride) + (left * BytesPerPixel), rowLength));
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void Compress(ReadOnlySpan<byte> pixels, ReadOnlySpan<byte> previous, int stride, int left, int top, int width, int height, int paddedWidth, int paddedHeight, bool isRgba, Stream destination)
    {
        var rowLength = width * BytesPerPixel;
        var paddedRowLength = paddedWidth * BytesPerPixel;
        var buffer = ArrayPool<byte>.Shared.Rent((paddedRowLength * 3) + 1);

        try
        {
            // Rows are only written up to the width of the region: what is on their right stays transparent.
            var current = buffer.AsSpan(0, paddedRowLength);
            var above = buffer.AsSpan(paddedRowLength, paddedRowLength);
            var filtered = buffer.AsSpan(paddedRowLength * 2, paddedRowLength + 1);
            current.Clear();
            above.Clear();
            filtered[0] = UpFilter;

            using var zlib = new ZLibStream(destination, Compression, leaveOpen: true);

            for (var y = 0; y < paddedHeight; y++)
            {
                if (y < height)
                {
                    var offset = ((top + y) * stride) + (left * BytesPerPixel);
                    var row = pixels.Slice(offset, rowLength);

                    if (isRgba)
                    {
                        row.CopyTo(current);
                    }
                    else
                    {
                        ConvertRow(row, previous.IsEmpty ? default : previous.Slice(offset, rowLength), current);
                    }
                }
                else
                {
                    current.Clear();
                }

                Subtract(current, above, filtered.Slice(1));
                zlib.Write(filtered);

                var swap = current;
                current = above;
                above = swap;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Converts a row of BGRA pixels to opaque RGBA pixels. Pixels that are the same as in the previous row, when there is one, become transparent.
    /// </summary>
    private static void ConvertRow(ReadOnlySpan<byte> bgra, ReadOnlySpan<byte> previousBgra, Span<byte> rgba)
    {
        var hasPrevious = !previousBgra.IsEmpty;
        var i = 0;

        if (BitConverter.IsLittleEndian)
        {
            var source = MemoryMarshal.Cast<byte, uint>(bgra);
            var previous = MemoryMarshal.Cast<byte, uint>(previousBgra);
            var destination = MemoryMarshal.Cast<byte, uint>(rgba);

            if (Vector.IsHardwareAccelerated)
            {
                var redBlueMask = new Vector<uint>(0x00FF00FFu);
                var greenMask = new Vector<uint>(0x0000FF00u);
                var alpha = new Vector<uint>(AlphaMask);

                for (; i <= source.Length - Vector<uint>.Count; i += Vector<uint>.Count)
                {
                    var pixels = new Vector<uint>(source.Slice(i));
                    var redBlue = pixels & redBlueMask;
                    var converted = (pixels & greenMask) | (redBlue << 16) | (redBlue >>> 16) | alpha;

                    if (hasPrevious)
                    {
                        converted = Vector.AndNot(converted, Vector.Equals(pixels, new Vector<uint>(previous.Slice(i))));
                    }

                    converted.CopyTo(destination.Slice(i));
                }
            }

            for (; i < source.Length; i++)
            {
                var pixel = source[i];
                destination[i] = hasPrevious && pixel == previous[i]
                    ? 0
                    : (pixel & 0x0000FF00u) | ((pixel & 0x00FF0000u) >> 16) | ((pixel & 0x000000FFu) << 16) | AlphaMask;
            }

            return;
        }

        for (; i < bgra.Length; i += BytesPerPixel)
        {
            if (hasPrevious && bgra.Slice(i, BytesPerPixel).SequenceEqual(previousBgra.Slice(i, BytesPerPixel)))
            {
                rgba.Slice(i, BytesPerPixel).Clear();
            }
            else
            {
                rgba[i] = bgra[i + 2];
                rgba[i + 1] = bgra[i + 1];
                rgba[i + 2] = bgra[i];
                rgba[i + 3] = 0xFF;
            }
        }
    }

    private static void Subtract(ReadOnlySpan<byte> current, ReadOnlySpan<byte> above, Span<byte> difference)
    {
        var i = 0;

        if (Vector.IsHardwareAccelerated)
        {
            for (; i <= current.Length - Vector<byte>.Count; i += Vector<byte>.Count)
            {
                (new Vector<byte>(current.Slice(i)) - new Vector<byte>(above.Slice(i))).CopyTo(difference.Slice(i));
            }
        }

        for (; i < current.Length; i++)
        {
            difference[i] = (byte)(current[i] - above[i]);
        }
    }

    private static void Add(Span<byte> row, ReadOnlySpan<byte> difference)
    {
        var i = 0;

        if (Vector.IsHardwareAccelerated)
        {
            for (; i <= row.Length - Vector<byte>.Count; i += Vector<byte>.Count)
            {
                (new Vector<byte>(row.Slice(i)) + new Vector<byte>(difference.Slice(i))).CopyTo(row.Slice(i));
            }
        }

        for (; i < row.Length; i++)
        {
            row[i] = (byte)(row[i] + difference[i]);
        }
    }

    /// <summary>
    /// Copies the pixels of a row that are not transparent.
    /// </summary>
    private static void Blend(ReadOnlySpan<byte> row, Span<byte> destination)
    {
        var i = 0;

        if (Vector.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            var source = MemoryMarshal.Cast<byte, uint>(row);
            var target = MemoryMarshal.Cast<byte, uint>(destination);
            var alpha = new Vector<uint>(AlphaMask);

            for (; i <= source.Length - Vector<uint>.Count; i += Vector<uint>.Count)
            {
                var pixels = new Vector<uint>(source.Slice(i));
                var isTransparent = Vector.Equals(pixels & alpha, Vector<uint>.Zero);
                Vector.ConditionalSelect(isTransparent, new Vector<uint>(target.Slice(i)), pixels).CopyTo(target.Slice(i));
            }

            i *= BytesPerPixel;
        }

        for (; i < row.Length; i += BytesPerPixel)
        {
            if (row[i + 3] != 0)
            {
                row.Slice(i, BytesPerPixel).CopyTo(destination.Slice(i));
            }
        }
    }
}
