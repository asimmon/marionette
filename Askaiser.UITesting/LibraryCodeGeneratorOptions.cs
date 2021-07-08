using System;

namespace Askaiser.UITesting
{
    public sealed class LibraryCodeGeneratorOptions
    {
        private readonly long _maxImageFileSize = 2 * 1024 * 1024;

        public long MaxImageFileSize
        {
            get => this._maxImageFileSize;
            init => this._maxImageFileSize = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(this.MaxImageFileSize));
        }

        public string NamespaceName { get; init; }

        public string ImageDirectoryPath { get; init; }

        public void Validate()
        {
            if (this.MaxImageFileSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(this.MaxImageFileSize));

            if (string.IsNullOrWhiteSpace(this.NamespaceName))
                throw new ArgumentException(nameof(this.NamespaceName));

            if (string.IsNullOrWhiteSpace(this.ImageDirectoryPath))
                throw new ArgumentException(nameof(this.NamespaceName));
        }
    }
}