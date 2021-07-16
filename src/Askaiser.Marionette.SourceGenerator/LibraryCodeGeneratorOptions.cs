using System;

namespace Askaiser.Marionette.SourceGenerator
{
    public sealed class LibraryCodeGeneratorOptions
    {
        public LibraryCodeGeneratorOptions()
        {
            this.MaxImageFileSize = 2 * 1024 * 1024;
        }

        public long MaxImageFileSize { get; set; }

        public string NamespaceName { get; set; }

        public string ClassName { get; set; }

        public string ImageDirectoryPath { get; set; }

        public void Validate()
        {
            if (this.MaxImageFileSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(this.MaxImageFileSize));

            if (string.IsNullOrWhiteSpace(this.ClassName))
                throw new ArgumentException(nameof(this.ClassName));

            if (string.IsNullOrWhiteSpace(this.ImageDirectoryPath))
                throw new ArgumentException(nameof(this.NamespaceName));
        }
    }
}
