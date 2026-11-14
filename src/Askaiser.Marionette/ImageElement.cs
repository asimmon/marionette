using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Threading;
using Askaiser.Marionette.Recognition.Images;

namespace Askaiser.Marionette;

/// <summary>
/// An image to look for on the screen.
/// </summary>
[DebuggerDisplay("{Name}")]
public sealed class ImageElement : IElement
{
    internal const decimal DefaultThreshold = 0.95m;

    private readonly string _name = string.Empty;
    private readonly decimal _threshold;
    private readonly bool _grayscale;
    private byte[]? _content;
    private Image? _image;
    private ImageTemplate? _template;
    private bool _templateCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageElement"/> class from a base64-encoded PNG, JPEG or BMP image.
    /// </summary>
    public ImageElement(string name, string base64Content, decimal threshold, bool grayscale)
        : this(name, Convert.FromBase64String(base64Content), threshold, grayscale)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageElement"/> class from a PNG, JPEG or BMP image.
    /// </summary>
    public ImageElement(string name, byte[] content, decimal threshold, bool grayscale)
    {
        this.Name = name;
        this.Content = content;
        this.Threshold = threshold;
        this.Grayscale = grayscale;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageElement"/> class from an in-memory image.
    /// </summary>
    public ImageElement(string name, Image image)
        : this(name, image, DefaultThreshold, grayscale: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageElement"/> class from an in-memory image.
    /// </summary>
    public ImageElement(string name, Image image, decimal threshold, bool grayscale)
    {
        ArgumentNullException.ThrowIfNull(image);

        this.Name = name;
        this._image = image;
        this.Threshold = threshold;
        this.Grayscale = grayscale;
    }

    /// <summary>
    /// Creates an image element from a PNG, JPEG or BMP image encoded as base64 text, itself encoded as UTF-8.
    /// This is what generated image libraries use: such text can be stored as static data of an assembly, without allocating a string.
    /// </summary>
    public static ImageElement FromBase64(string name, ReadOnlySpan<byte> utf8Base64Content, decimal threshold, bool grayscale)
    {
        var content = new byte[Base64.GetMaxDecodedFromUtf8Length(utf8Base64Content.Length)];
        if (Base64.DecodeFromUtf8(utf8Base64Content, content, out _, out var written) != OperationStatus.Done)
        {
            throw new FormatException(Messages.ImageElement_Throw_InvalidBase64);
        }

        if (written != content.Length)
        {
            Array.Resize(ref content, written);
        }

        return new ImageElement(name, content, threshold, grayscale);
    }

    /// <summary>
    /// Gets the name of the element.
    /// </summary>
    public string Name
    {
        get => this._name;
        init => this._name = value is { Length: > 0 } ? value : throw new ArgumentException(Messages.Element_Throw_InvalidName, nameof(value));
    }

    /// <summary>
    /// Gets the image as PNG, JPEG or BMP encoded bytes.
    /// </summary>
    public byte[] Content
    {
        get => this._content ??= this._image!.ToPng();
        init
        {
            this._content = value is { Length: > 0 } ? value : throw new ArgumentException(Messages.ImageElement_Throw_EmptyContent, nameof(value));
            this._image = null;
            this._template = null;
            this._templateCreated = false;
        }
    }

    /// <summary>
    /// Gets how similar a region of the screen must be to the image to be considered a match, from 0 to 1.
    /// The default value, 0.95, tolerates small rendering differences.
    /// </summary>
    public decimal Threshold
    {
        get => this._threshold;
        init
        {
            this._threshold = value is >= 0 and <= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), Messages.ImageElement_Throw_ThresholdOutOfRange);
            this._template = null;
            this._templateCreated = false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether colors are ignored when comparing the image with the screen.
    /// </summary>
    public bool Grayscale
    {
        get => this._grayscale;
        init
        {
            this._grayscale = value;
            this._template = null;
            this._templateCreated = false;
        }
    }

    /// <summary>Returns the name of the element.</summary>
    public override string ToString()
    {
        return this.Name;
    }

    /// <summary>
    /// Gets the decoded image. Decoding only happens once, the first time the element is searched.
    /// </summary>
    internal Image GetImage()
    {
        return this._image ??= Image.FromBytes(this._content);
    }

    /// <summary>
    /// Gets the analyzed image used by the search, or null when the image is a solid color.
    /// The analysis only depends on immutable properties, so racing threads at worst compute it twice.
    /// </summary>
    internal ImageTemplate? GetTemplate()
    {
        if (Volatile.Read(ref this._templateCreated))
        {
            return this._template;
        }

        var image = this.GetImage();
        var template = ImageTemplate.Create(image.Pixels, image.Width, image.Height, (double)this.Threshold, this.Grayscale);

        this._template = template;
        Volatile.Write(ref this._templateCreated, true);
        return template;
    }
}
