using System;

namespace Askaiser.Marionette;

[Serializable]
[AttributeUsage(AttributeTargets.Class)]
public sealed class ImageLibraryAttribute : Attribute
{
    public ImageLibraryAttribute(string imageLibraryDirectoryPath)
    {
        this.ImageLibraryDirectoryPath = imageLibraryDirectoryPath;
    }

    public string ImageLibraryDirectoryPath { get; }
}
