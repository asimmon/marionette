using System;

namespace Askaiser.Marionette;

[Serializable]
[AttributeUsage(AttributeTargets.Class)]
public sealed class ImageLibraryAttribute : Attribute
{
    public ImageLibraryAttribute(string imageLibraryDirectoryPath, bool singleton = false)
    {
        this.ImageLibraryDirectoryPath = imageLibraryDirectoryPath;
        this.Singleton = true;
    }

    public string ImageLibraryDirectoryPath { get; }

    public bool Singleton { get; }
}
