using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Askaiser.Marionette.Tests;

/// <summary>
/// An animated PNG file, decoded by the rules of the APNG specification rather than with the code that wrote it.
/// </summary>
internal sealed class AnimatedPngReader
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] CrcTable = CreateCrcTable();

    private AnimatedPngReader(int width, int height, Image defaultImage, List<AnimatedPngFrame> frames)
    {
        this.Width = width;
        this.Height = height;
        this.DefaultImage = defaultImage;
        this.Frames = frames;
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>Gets the image displayed by programs that do not play animations.</summary>
    public Image DefaultImage { get; }

    public IReadOnlyList<AnimatedPngFrame> Frames { get; }

    public TimeSpan Duration => this.Frames.Aggregate(TimeSpan.Zero, static (total, frame) => total + frame.Delay);

    public static AnimatedPngReader Read(ReadOnlySpan<byte> file)
    {
        Ensure(file.StartsWith(Signature), "The file does not start with the PNG signature.");
        var position = Signature.Length;

        var width = 0;
        var height = 0;
        var declaredFrameCount = -1;
        var expectedSequence = 0;
        byte[]? defaultImageData = null;
        FrameControl? control = null;
        var ended = false;

        byte[]? canvas = null;
        var frames = new List<AnimatedPngFrame>();

        while (position < file.Length)
        {
            Ensure(!ended, "There is data after the end of the image.");

            var length = BinaryPrimitives.ReadInt32BigEndian(file.Slice(position));
            var type = Encoding.ASCII.GetString(file.Slice(position + 4, 4));
            var data = file.Slice(position + 8, length);
            var crc = BinaryPrimitives.ReadUInt32BigEndian(file.Slice(position + 8 + length));
            Ensure(crc == ComputeCrc(file.Slice(position + 4, length + 4)), $"The checksum of a {type} chunk is wrong.");
            position += length + 12;

            switch (type)
            {
                case "IHDR":
                    width = BinaryPrimitives.ReadInt32BigEndian(data);
                    height = BinaryPrimitives.ReadInt32BigEndian(data.Slice(4));
                    Ensure(data[8] == 8 && data[9] == 6 && data[12] == 0, "Only 8-bit RGBA images without interlacing are expected.");
                    canvas = new byte[width * height * 4];
                    break;

                case "acTL":
                    Ensure(defaultImageData == null, "The animation must be declared before the image.");
                    declaredFrameCount = BinaryPrimitives.ReadInt32BigEndian(data);
                    break;

                case "IDAT":
                    Ensure(defaultImageData == null && control == null, "A single image that is not part of the animation is expected.");
                    defaultImageData = data.ToArray();
                    break;

                case "fcTL":
                    Ensure(control == null, "A frame has no pixels.");
                    Ensure(BinaryPrimitives.ReadInt32BigEndian(data) == expectedSequence++, "Animation chunks are out of sequence.");

                    control = new FrameControl(
                        BinaryPrimitives.ReadInt32BigEndian(data.Slice(4)),
                        BinaryPrimitives.ReadInt32BigEndian(data.Slice(8)),
                        BinaryPrimitives.ReadInt32BigEndian(data.Slice(12)),
                        BinaryPrimitives.ReadInt32BigEndian(data.Slice(16)),
                        BinaryPrimitives.ReadUInt16BigEndian(data.Slice(20)),
                        BinaryPrimitives.ReadUInt16BigEndian(data.Slice(22)),
                        data[24],
                        data[25]);

                    Ensure(control.Width > 0 && control.Height > 0 && control.Left >= 0 && control.Top >= 0, "A frame has an invalid region.");
                    Ensure(control.Left + control.Width <= width && control.Top + control.Height <= height, "A frame goes beyond the image.");
                    break;

                case "fdAT":
                    Ensure(control != null && canvas != null, "Pixels do not belong to a frame.");
                    Ensure(BinaryPrimitives.ReadInt32BigEndian(data) == expectedSequence++, "Animation chunks are out of sequence.");

                    var pixels = Decode(data.Slice(4), control!.Width, control.Height);
                    Draw(pixels, canvas!, width, control);
                    frames.Add(new AnimatedPngFrame(Image.FromRgba32(width, height, canvas), control.Delay));

                    if (control.DisposeOperation == 1)
                    {
                        for (var y = 0; y < control.Height; y++)
                        {
                            canvas.AsSpan((((control.Top + y) * width) + control.Left) * 4, control.Width * 4).Clear();
                        }
                    }
                    else
                    {
                        Ensure(control.DisposeOperation == 0, "Frames are not expected to restore what was displayed before them.");
                    }

                    control = null;
                    break;

                case "IEND":
                    ended = true;
                    break;

                default:
                    throw new InvalidDataException($"The chunk {type} is not expected.");
            }
        }

        Ensure(ended && control == null, "The file is truncated.");
        Ensure(defaultImageData != null, "The file has no image.");
        Ensure(declaredFrameCount == frames.Count, $"The file declares {declaredFrameCount} frames but contains {frames.Count}.");

        var defaultImage = Image.FromRgba32(width, height, Decode(defaultImageData, width, height));
        return new AnimatedPngReader(width, height, defaultImage, frames);
    }

    private static void Draw(byte[] pixels, byte[] canvas, int canvasWidth, FrameControl control)
    {
        for (var y = 0; y < control.Height; y++)
        {
            for (var x = 0; x < control.Width; x++)
            {
                var source = pixels.AsSpan(((y * control.Width) + x) * 4, 4);
                var alpha = source[3];

                // The "over" operation blends pixels. The files of this library only contain opaque and fully transparent ones.
                Ensure(alpha is 0 or 255, "Pixels are expected to be either opaque or fully transparent.");

                if (control.BlendOperation == 0 || alpha == 255)
                {
                    source.CopyTo(canvas.AsSpan((((control.Top + y) * canvasWidth) + control.Left + x) * 4, 4));
                }
            }
        }
    }

    private static byte[] Decode(ReadOnlySpan<byte> compressed, int width, int height)
    {
        const int bytesPerPixel = 4;
        var stride = width * bytesPerPixel;
        var raw = new byte[(stride + 1) * height];

        using (var zlib = new ZLibStream(new MemoryStream(compressed.ToArray()), CompressionMode.Decompress))
        {
            zlib.ReadExactly(raw);
            Ensure(zlib.ReadByte() == -1, "A frame contains more pixels than its size.");
        }

        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var filter = raw[y * (stride + 1)];
            var filtered = raw.AsSpan((y * (stride + 1)) + 1, stride);
            var row = pixels.AsSpan(y * stride, stride);

            for (var i = 0; i < stride; i++)
            {
                var left = i >= bytesPerPixel ? row[i - bytesPerPixel] : 0;
                var above = y > 0 ? pixels[((y - 1) * stride) + i] : 0;
                var aboveLeft = y > 0 && i >= bytesPerPixel ? pixels[((y - 1) * stride) + i - bytesPerPixel] : 0;

                var prediction = filter switch
                {
                    0 => 0,
                    1 => left,
                    2 => above,
                    3 => (left + above) / 2,
                    4 => Paeth(left, above, aboveLeft),
                    _ => throw new InvalidDataException($"The row filter {filter} does not exist."),
                };

                row[i] = (byte)(filtered[i] + prediction);
            }
        }

        return pixels;
    }

    private static int Paeth(int left, int above, int aboveLeft)
    {
        var estimate = left + above - aboveLeft;
        var distanceLeft = Math.Abs(estimate - left);
        var distanceAbove = Math.Abs(estimate - above);
        var distanceAboveLeft = Math.Abs(estimate - aboveLeft);

        return distanceLeft <= distanceAbove && distanceLeft <= distanceAboveLeft ? left : distanceAbove <= distanceAboveLeft ? above : aboveLeft;
    }

    private static uint ComputeCrc(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFFu;
    }

    private static uint[] CreateCrcTable()
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

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }

    private sealed record FrameControl(int Width, int Height, int Left, int Top, ushort DelayNumerator, ushort DelayDenominator, byte DisposeOperation, byte BlendOperation)
    {
        public TimeSpan Delay => TimeSpan.FromSeconds(this.DelayNumerator / (double)(this.DelayDenominator == 0 ? 100 : this.DelayDenominator));
    }
}
