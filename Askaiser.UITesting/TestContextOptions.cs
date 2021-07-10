using System;

namespace Askaiser.UITesting
{
    public sealed class TestContextOptions
    {
        private readonly string _tesseractDataPath = "./tessdata";
        private readonly string _tesseractLanguage = "eng";

        public string TesseractDataPath
        {
            get => this._tesseractDataPath;
            init => this._tesseractDataPath = value?.Trim() is { Length: > 0 } trimmedValue ? trimmedValue : throw new ArgumentException(nameof(this.TesseractDataPath));
        }

        public string TesseractLanguage
        {
            get => this._tesseractLanguage;
            init => this._tesseractLanguage = value?.Trim() is { Length: > 0 } trimmedValue ? trimmedValue : throw new ArgumentException(nameof(this.TesseractLanguage));
        }
    }
}
