using System;
using System.IO;

namespace Askaiser.Marionette;

public sealed class DriverOptions
{
    private static readonly Lazy<string> LazyCurrentAssemblyDirectoryPath = new Lazy<string>(GetCurrentAssemblyDirectoryPath);

    private readonly string _tesseractDataPath;
    private readonly string _tesseractLanguage;
    private readonly string? _failureScreenshotPath;
    private readonly TimeSpan _screenshotCacheDuration;
    private readonly TimeSpan _waitForThrottlingInterval;
    private readonly TimeSpan _defaultWaitForDuration;
    private readonly TimeSpan _defaultKeyboardSleepAfterDuration;

    public DriverOptions()
    {
        this._tesseractDataPath = Path.Combine(LazyCurrentAssemblyDirectoryPath.Value, "tessdata");
        this._tesseractLanguage = "eng";
        this._failureScreenshotPath = null;
        this._screenshotCacheDuration = TimeSpan.FromMilliseconds(100);
        this._waitForThrottlingInterval = TimeSpan.FromMilliseconds(50);
        this._defaultWaitForDuration = TimeSpan.Zero;
        this._defaultKeyboardSleepAfterDuration = TimeSpan.Zero;
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
    public string? FailureScreenshotPath
    {
        get => this._failureScreenshotPath;
        init => this._failureScreenshotPath = value?.Trim() is { Length: > 0 } trimmedValue ? trimmedValue : throw new ArgumentException(nameof(this.FailureScreenshotPath));
    }

    /// <summary>
    /// Gets or sets how long previous screenshot bytes will be reused in subsequent searches. The cache will be evicted if the current monitor is changed.
    /// It can be useful when searching for multiple elements at the same time, but it can also make the driver miss some frames.
    /// Default value: 100 milliseconds.
    /// </summary>
    public TimeSpan ScreenshotCacheDuration
    {
        get => this._screenshotCacheDuration;
        init => this._screenshotCacheDuration = value >= TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(this.ScreenshotCacheDuration), Messages.DriverOptions_Throw_NegativeScreenshotCacheDuration);
    }

    /// <summary>
    /// When an element is not found and the driver is allowed to try again to find it, it will wait for this specific duration before retrying.
    /// Default value: 50 milliseconds.
    /// </summary>
    public TimeSpan WaitForThrottlingInterval
    {
        get => this._waitForThrottlingInterval;
        init => this._waitForThrottlingInterval = value >= TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(this.WaitForThrottlingInterval), Messages.DriverOptions_Throw_NegativeWaitForThrottlingInterval);
    }

    /// <summary>
    /// Gets or sets the waitFor default value for any element wait-based method that receives a null waitFor TimeSpan?. Default is TimeSpan.Zero.
    /// </summary>
    public TimeSpan DefaultWaitForDuration
    {
        get => this._defaultWaitForDuration;
        init => this._defaultWaitForDuration = value >= TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(this.DefaultWaitForDuration), Messages.DriverOptions_Throw_NegativeDefaultWaitForDuration);
    }

    /// <summary>
    /// Gets or sets the sleepAfter default value for any keyboard-based method that receives a null sleepAfter TimeSpan?. Default is TimeSpan.Zero.
    /// </summary>
    public TimeSpan DefaultKeyboardSleepAfterDuration
    {
        get => this._defaultKeyboardSleepAfterDuration;
        init => this._defaultKeyboardSleepAfterDuration = value >= TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(this.DefaultKeyboardSleepAfterDuration), Messages.DriverOptions_Throw_NegativeDefaultKeyboardSleepAfterDuration);
    }

    /// <summary>
    /// Gets or sets the initial mouse speed of the driver
    /// </summary>
    public MouseSpeed MouseSpeed { get; init; }

    private static string GetCurrentAssemblyDirectoryPath()
    {
        return AppContext.BaseDirectory is { Length: > 0 } baseDir ? baseDir : ".";
    }
}
