using System;
using System.IO;
using Askaiser.Marionette.Imaging;

namespace Askaiser.Marionette;

/// <summary>
/// An immutable in-memory image, such as a screenshot or an image to look for on the screen.
/// Pixels are stored as 32-bit BGRA, top-down, without padding between rows. The type has no dependency on any
/// operating system imaging library, which makes it usable on Windows, Linux and macOS alike.
/// </summary>
public sealed class Image
{
    private readonly byte[] _bgra;

    internal Image(byte[] bgra, int width, int height)
    {
        this._bgra = bgra;
        this.Width = width;
        this.Height = height;
    }

    /// <summary>
    /// Gets the width of the image in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the image in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the pixels of the image: 4 bytes per pixel in blue, green, red, alpha order, row by row from the top-left corner.
    /// </summary>
    public ReadOnlySpan<byte> Pixels
    {
        get => this._bgra.AsSpan(0, this.Width * this.Height * PixelBuffer.BytesPerPixel);
    }

    internal byte[] PixelArray
    {
        get => this._bgra;
    }

    /// <summary>
    /// Loads a PNG, JPEG or BMP image from a file.
    /// </summary>
    public static Image FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return FromBytes(File.ReadAllBytes(path));
    }

    /// <summary>
    /// Loads a PNG, JPEG or BMP image from a stream. The stream is read until its end and is left open.
    /// </summary>
    public static Image FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var segment))
        {
            return FromBytes(segment.AsSpan((int)memoryStream.Position));
        }

        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return FromBytes(copy.GetBuffer().AsSpan(0, (int)copy.Length));
    }

    /// <summary>
    /// Loads an image from PNG, JPEG or BMP encoded bytes.
    /// </summary>
    public static Image FromBytes(ReadOnlySpan<byte> encodedImage)
    {
        var bgra = ImageCodec.DecodeToBgra(encodedImage, out var width, out var height);
        return new Image(bgra, width, height);
    }

    /// <summary>
    /// Creates an image from raw 32-bit BGRA pixels (blue, green, red, alpha), row by row from the top-left corner. The pixels are copied.
    /// </summary>
    public static Image FromBgra32(int width, int height, ReadOnlySpan<byte> pixels)
    {
        return new Image(ValidateAndCopy(width, height, pixels), width, height);
    }

    /// <summary>
    /// Creates an image from raw 32-bit RGBA pixels (red, green, blue, alpha), row by row from the top-left corner. The pixels are copied.
    /// </summary>
    public static Image FromRgba32(int width, int height, ReadOnlySpan<byte> pixels)
    {
        var bgra = ValidateAndCopy(width, height, pixels);
        ImageCodec.SwapRedAndBlue(bgra);
        return new Image(bgra, width, height);
    }

    /// <summary>
    /// Returns a new image made of the pixels inside the specified region.
    /// </summary>
    public Image Crop(Rectangle region)
    {
        ArgumentNullException.ThrowIfNull(region);

        if (region.Left < 0 || region.Top < 0 || region.Right > this.Width || region.Bottom > this.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(region), Messages.Image_Throw_RegionOutOfBounds.FormatInvariant(region, this.Width, this.Height));
        }

        if (region.Width == 0 || region.Height == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(region), Messages.Image_Throw_EmptyRegion);
        }

        var cropped = new byte[region.Width * region.Height * PixelBuffer.BytesPerPixel];
        using (var source = PixelBuffer.Wrap(this._bgra, this.Width, this.Height))
        {
            source.CopyRegionTo(region.Left, region.Top, region.Width, region.Height, cropped);
        }

        return new Image(cropped, region.Width, region.Height);
    }

    /// <summary>
    /// Encodes the image as PNG.
    /// </summary>
    public byte[] ToPng()
    {
        return ImageCodec.EncodePng(this._bgra, this.Width, this.Height);
    }

    /// <summary>
    /// Saves the image as a PNG file.
    /// </summary>
    public void Save(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var stream = File.Create(path);
        this.Save(stream);
    }

    /// <summary>
    /// Writes the image to a stream as PNG.
    /// </summary>
    public void Save(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ImageCodec.EncodePng(this._bgra, this.Width, this.Height, stream);
    }

    /// <summary>Returns the size of the image, formatted as <c>Image widthxheight</c>.</summary>
    public override string ToString()
    {
        return "Image " + this.Width.ToString(System.Globalization.CultureInfo.InvariantCulture) + "x" + this.Height.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static byte[] ValidateAndCopy(int width, int height, ReadOnlySpan<byte> pixels)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(width <= 0 ? nameof(width) : nameof(height), Messages.Image_Throw_InvalidDimensions);
        }

        var expectedLength = checked(width * height * PixelBuffer.BytesPerPixel);
        if (pixels.Length != expectedLength)
        {
            throw new ArgumentException(Messages.Image_Throw_InvalidPixelLength.FormatInvariant(expectedLength, width, height, pixels.Length), nameof(pixels));
        }

        return pixels.ToArray();
    }
}
