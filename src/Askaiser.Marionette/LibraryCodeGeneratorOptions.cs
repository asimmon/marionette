using System;

namespace Askaiser.Marionette
{
    public sealed class LibraryCodeGeneratorOptions
    {
        public long MaxImageFileSize { get; init; }

        public string NamespaceName { get; init; }

        public string ClassName { get; init; }

        public string ImageDirectoryPath { get; init; }

        public void Validate()
        {
            if (this.MaxImageFileSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(this.MaxImageFileSize));

            if (string.IsNullOrWhiteSpace(this.NamespaceName))
                throw new ArgumentException(nameof(this.NamespaceName));

            if (string.IsNullOrWhiteSpace(this.ClassName))
                throw new ArgumentException(nameof(this.ClassName));

            if (string.IsNullOrWhiteSpace(this.ImageDirectoryPath))
                throw new ArgumentException(nameof(this.NamespaceName));
        }
    }
}
