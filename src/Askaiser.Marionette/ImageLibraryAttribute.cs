using System;

namespace Askaiser.Marionette
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ImageLibraryAttribute : Attribute
    {
        public string ImageLibraryDirectoryPath { get; }

        public ImageLibraryAttribute(string imageLibraryDirectoryPath)
        {
            this.ImageLibraryDirectoryPath = imageLibraryDirectoryPath;
        }
    }
}
