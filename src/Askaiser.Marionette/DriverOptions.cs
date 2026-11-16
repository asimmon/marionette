using System;

namespace Askaiser.Marionette;

/// <summary>
/// The settings of a <see cref="MarionetteDriver"/>.
/// </summary>
public sealed class DriverOptions
{
    /// <summary>
    /// Gets the model used to read text on the screen. The default model reads languages written in Latin script.
    /// </summary>
    public TextRecognitionModel TextRecognitionModel
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    }

    = TextRecognitionModel.Default;

    /// <summary>
    /// Gets the directory where a screenshot is saved when an element is not found, or found more than once.
    /// Default value: null, no screenshots are saved.
    /// </summary>
    public string? FailureScreenshotPath
    {
        get;
        init => field = value?.Trim() is { Length: > 0 } trimmedValue ? trimmedValue : throw new ArgumentException(nameof(this.FailureScreenshotPath), nameof(value));
    }

    /// <summary>
    /// Gets how long a screenshot is reused by subsequent searches. A screenshot is never reused after the current monitor changed.
    /// Reusing screenshots saves time when several elements are searched one after the other, at the risk of missing very brief changes.
    /// Default value: 100 milliseconds.
    /// </summary>
    public TimeSpan ScreenshotCacheDuration
    {
        get;
        init => field = value >= TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value), Messages.DriverOptions_Throw_NegativeScreenshotCacheDuration);
    }

    = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets how long the driver waits before looking again for an element that was not found.
    /// Default value: 50 milliseconds.
    /// </summary>
    public TimeSpan WaitForThrottlingInterval
    {
        get;
        init => field = value >= TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value), Messages.DriverOptions_Throw_NegativeWaitForThrottlingInterval);
    }

    = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Gets how long methods wait for an element when they are not given a <c>waitFor</c> duration.
    /// Default value: zero, elements must be visible right away.
    /// </summary>
    public TimeSpan DefaultWaitForDuration
    {
        get;
        init => field = value >= TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value), Messages.DriverOptions_Throw_NegativeDefaultWaitForDuration);
    }

    /// <summary>
    /// Gets how long keyboard methods pause after typing when they are not given a <c>sleepAfter</c> duration.
    /// Default value: zero.
    /// </summary>
    public TimeSpan DefaultKeyboardSleepAfterDuration
    {
        get;
        init => field = value >= TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value), Messages.DriverOptions_Throw_NegativeDefaultKeyboardSleepAfterDuration);
    }

    /// <summary>
    /// Gets the initial speed of the mouse cursor. Default value: <see cref="MouseSpeed.Fast"/>.
    /// </summary>
    public MouseSpeed MouseSpeed
    {
        get;
        init => field = Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    = MouseSpeed.Fast;
}
