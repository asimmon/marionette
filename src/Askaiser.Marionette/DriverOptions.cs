using System;

namespace Askaiser.Marionette
{
    public sealed class DriverOptions
    {
        private readonly string _tesseractDataPath;
        private readonly string _tesseractLanguage;
        private readonly string _failureScreenshotPath;

        public DriverOptions()
        {
            this._tesseractDataPath = "./tessdata";
            this._tesseractLanguage = "eng";
            this._failureScreenshotPath = null;
            this.MouseSpeed = MouseSpeed.Fast;
        }

        /// <summary>
        /// The directory path of Tesseract OCR data (https://github.com/tesseract-ocr/tessdata).
        /// Allows you to use your own tessdata. Default value: ./tessdata
        /// </summary>
        public string TesseractDataPath
        {
            get => this._tesseractDataPath;
            init => this._tesseractDataPath = value?.Trim() is { Length: > 0 } trimmedValue ? trimmedValue : throw new ArgumentException(nameof(this.TesseractDataPath));
        }

        /// <summary>
        /// Overrides the language used by Tesseract OCR. Default value: eng
        /// </summary>
        public string TesseractLanguage
        {
            get => this._tesseractLanguage;
            init => this._tesseractLanguage = value?.Trim() is { Length: > 0 } trimmedValue ? trimmedValue : throw new ArgumentException(nameof(this.TesseractLanguage));
        }

        /// <summary>
        /// The directory path where screenshots can be saved when an element recognition fails.
        /// Default value: null, no screenshots are saved.
        /// </summary>
        public string FailureScreenshotPath
        {
            get => this._failureScreenshotPath;
            init => this._failureScreenshotPath = value?.Trim() is { Length: > 0 } trimmedValue ? trimmedValue : throw new ArgumentException(nameof(this.FailureScreenshotPath));
        }

        /// <summary>
        /// Gets or sets the initial mouse speed of the driver
        /// </summary>
        public MouseSpeed MouseSpeed { get; init; }
    }
}
