using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using StbImageSharp;

namespace Askaiser.Marionette.Imaging;

/// <summary>
/// Decodes PNG, JPEG and BMP images to BGRA and encodes BGRA to PNG.
/// </summary>
internal static class ImageCodec
{
    /// <summary>Gets the bytes every PNG file starts with.</summary>
    public static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly uint[] Crc32Table = CreateCrc32Table();

    /// <summary>
    /// Reads the dimensions of a PNG, JPEG or BMP image and decodes it to a BGRA array.
    /// </summary>
    public static byte[] DecodeToBgra(ReadOnlySpan<byte> encoded, out int width, out int height)
    {
        ImageResult result;

        try
        {
            unsafe
            {
                fixed (byte* ptr = encoded)
                {
                    using var stream = new UnmanagedMemoryStream(ptr, encoded.Length);
                    result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                }
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new ArgumentException(Messages.Image_Throw_CannotDecode.FormatInvariant(ex.Message), nameof(encoded), ex);
        }

        width = result.Width;
        height = result.Height;

        var pixels = result.Data;
        SwapRedAndBlue(pixels);
        return pixels;
    }

    /// <summary>
    /// Swaps the first and third byte of every 4-byte pixel, in place. Converts RGBA to BGRA and the other way around.
    /// </summary>
    public static void SwapRedAndBlue(Span<byte> pixels)
    {
        var pixels32 = MemoryMarshal.Cast<byte, uint>(pixels);
        var i = 0;

        if (Vector.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            var greenAlphaMask = new Vector<uint>(0xFF00FF00u);
            var redBlueMask = new Vector<uint>(0x00FF00FFu);

            for (; i <= pixels32.Length - Vector<uint>.Count; i += Vector<uint>.Count)
            {
                var v = new Vector<uint>(pixels32.Slice(i));
                var redBlue = v & redBlueMask;
                var swapped = Vector.ShiftLeft(redBlue, 16) | Vector.ShiftRightLogical(redBlue, 16);
                ((v & greenAlphaMask) | swapped).CopyTo(pixels32.Slice(i));
            }
        }

        for (var j = i * 4; j <= pixels.Length - 4; j += 4)
        {
            (pixels[j], pixels[j + 2]) = (pixels[j + 2], pixels[j]);
        }
    }

    public static byte[] EncodePng(ReadOnlySpan<byte> bgra, int width, int height)
    {
        using var stream = new MemoryStream(capacity: Math.Max(1024, bgra.Length / 8));
        EncodePng(bgra, width, height, stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Encodes BGRA pixels to PNG. Fully opaque images are written as 24-bit RGB, others as 32-bit RGBA.
    /// </summary>
    public static void EncodePng(ReadOnlySpan<byte> bgra, int width, int height, Stream destination)
    {
        var hasAlpha = HasTransparency(bgra.Slice(0, width * height * 4));
        var channels = hasAlpha ? 4 : 3;

        destination.Write(PngSignature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.Slice(4), height);
        header[8] = 8; // bit depth
        header[9] = (byte)(hasAlpha ? 6 : 2); // color type: RGBA or RGB
        header[10] = 0; // compression method
        header[11] = 0; // filter method
        header[12] = 0; // no interlacing
        WriteChunk(destination, "IHDR"u8, header);

        using (var compressed = new MemoryStream(capacity: Math.Max(1024, bgra.Length / 8)))
        {
            using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
            {
                WriteFilteredScanlines(bgra, width, height, channels, zlib);
            }

            WriteChunk(destination, "IDAT"u8, compressed.GetBuffer().AsSpan(0, (int)compressed.Length));
        }

        WriteChunk(destination, "IEND"u8, ReadOnlySpan<byte>.Empty);
    }

    private static void WriteFilteredScanlines(ReadOnlySpan<byte> bgra, int width, int height, int channels, Stream zlib)
    {
        // Every scanline uses the "Up" filter: it is cheap to compute with SIMD and very effective on screenshots,
        // where consecutive rows are often identical.
        const byte upFilter = 2;

        var rowLength = width * channels;
        var current = new byte[rowLength];
        var previous = new byte[rowLength];
        var filtered = new byte[rowLength + 1];
        filtered[0] = upFilter;

        for (var y = 0; y < height; y++)
        {
            var source = bgra.Slice(y * width * 4, width * 4);

            if (channels == 4)
            {
                source.CopyTo(current);
                SwapRedAndBlue(current);
            }
            else
            {
                for (int x = 0, d = 0; x < source.Length; x += 4, d += 3)
                {
                    current[d] = source[x + 2];
                    current[d + 1] = source[x + 1];
                    current[d + 2] = source[x];
                }
            }

            var i = 0;
            if (Vector.IsHardwareAccelerated)
            {
                for (; i <= rowLength - Vector<byte>.Count; i += Vector<byte>.Count)
                {
                    (new Vector<byte>(current, i) - new Vector<byte>(previous, i)).CopyTo(filtered, i + 1);
                }
            }

            for (; i < rowLength; i++)
            {
                filtered[i + 1] = (byte)(current[i] - previous[i]);
            }

            zlib.Write(filtered, 0, filtered.Length);
            (current, previous) = (previous, current);
        }
    }

    private static bool HasTransparency(ReadOnlySpan<byte> bgra)
    {
        var pixels = MemoryMarshal.Cast<byte, uint>(bgra);
        var i = 0;

        if (Vector.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            var alphaMask = new Vector<uint>(0xFF000000u);
            for (; i <= pixels.Length - Vector<uint>.Count; i += Vector<uint>.Count)
            {
                if (!Vector.EqualsAll(new Vector<uint>(pixels.Slice(i)) & alphaMask, alphaMask))
                {
                    return true;
                }
            }
        }

        for (var j = (i * 4) + 3; j < bgra.Length; j += 4)
        {
            if (bgra[j] != 0xFF)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Writes a PNG chunk: its length, its type, its data and the checksum of the last two.
    /// </summary>
    public static void WriteChunk(Stream destination, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        WriteChunk(destination, type, ReadOnlySpan<byte>.Empty, data);
    }

    /// <summary>
    /// Writes a PNG chunk whose data is made of two parts, which avoids copying them next to each other.
    /// </summary>
    public static void WriteChunk(Stream destination, ReadOnlySpan<byte> type, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> data)
    {
        Span<byte> buffer = stackalloc byte[4];

        BinaryPrimitives.WriteInt32BigEndian(buffer, prefix.Length + data.Length);
        destination.Write(buffer);
        destination.Write(type);
        destination.Write(prefix);
        destination.Write(data);

        var crc = UpdateCrc32(UpdateCrc32(UpdateCrc32(0xFFFFFFFFu, type), prefix), data) ^ 0xFFFFFFFFu;
        BinaryPrimitives.WriteUInt32BigEndian(buffer, crc);
        destination.Write(buffer);
    }

    private static uint UpdateCrc32(uint crc, ReadOnlySpan<byte> data)
    {
        var table = Crc32Table;
        foreach (var b in data)
        {
            crc = table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] CreateCrc32Table()
    {
        var table = new uint[256];
        for (uint n = 0; n < table.Length; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
