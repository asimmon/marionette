using System.Globalization;
using System.Linq;

namespace Askaiser.Marionette.SourceGenerator;

/// <summary>
/// An image file and what its name says about how to search for it.
/// </summary>
internal sealed class ImageEntry
{
    public ImageEntry(ImageFile file, LibraryNode library, string name, int groupIndex, decimal threshold, bool grayscale)
    {
        this.File = file;
        this.Library = library;
        this.Name = name;
        this.GroupIndex = groupIndex;
        this.Threshold = threshold;
        this.Grayscale = grayscale;
    }

    public ImageFile File { get; }

    public LibraryNode Library { get; }

    /// <summary>Gets the name of the property that exposes this image.</summary>
    public string Name { get; }

    /// <summary>Gets the position of this image among the images that share its property.</summary>
    public int GroupIndex { get; }

    public decimal Threshold { get; }

    public bool Grayscale { get; }

    /// <summary>Gets the content of the file. Set once the file was read.</summary>
    public byte[] Content { get; set; } = [];

    /// <summary>Gets the name of the element, which identifies it in error messages: <c>Pages.Login.Title.0</c>.</summary>
    public string ElementName => string.Join(".", this.Library.Path.Concat([this.Name, this.GroupIndex.ToString(CultureInfo.InvariantCulture)]));
}
