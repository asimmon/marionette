using System;

namespace Askaiser.Marionette;

/// <summary>
/// Turns a partial class into an image library: a source generator adds a property to the class for every PNG, JPEG or BMP file
/// of a directory, and embeds the images in the assembly. The class cannot be static, generic or nested in another class.
/// </summary>
/// <example>
/// <code>
/// [ImageLibrary("images")]
/// public partial class MyLibrary
/// {
/// }
///
/// await driver.SingleClickAsync(MyLibrary.Instance.SaveButton); // images/save-button.png
/// </code>
/// </example>
[Serializable]
[AttributeUsage(AttributeTargets.Class)]
public sealed class ImageLibraryAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageLibraryAttribute"/> class.
    /// </summary>
    /// <param name="imageLibraryDirectoryPath">
    /// The path of the directory that contains the images. A relative path is resolved from the directory of the file that declares the class.
    /// </param>
    public ImageLibraryAttribute(string imageLibraryDirectoryPath)
    {
        this.ImageLibraryDirectoryPath = imageLibraryDirectoryPath;
    }

    /// <summary>
    /// Gets the path of the directory that contains the images. A relative path is resolved from the directory of the file that declares the class.
    /// </summary>
    public string ImageLibraryDirectoryPath { get; }
}
