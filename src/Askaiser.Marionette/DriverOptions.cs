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
    /// The screenshot is a PNG file that shows the part of the monitor that was searched.
    /// Default value: null, no screenshots are saved.
    /// </summary>
    /// <remarks>
    /// Failures are saved either as screenshots or as videos: this property cannot be set along with <see cref="FailureVideoPath"/>.
    /// </remarks>
    public string? FailureScreenshotPath
    {
        get;
        init => field = ValidateFailurePath(value);
    }

    /// <summary>
    /// Gets the directory where a video of the screen is saved when an element is not found, or found more than once.
    /// The video shows what the current monitor displayed during the last moments before the failure, see <see cref="FailureVideoDuration"/>.
    /// When the current monitor changed during these moments, the video shows one monitor after the other.
    /// Default value: null, the screen is not recorded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The video is an animated PNG file (APNG). It is lossless, so text stays readable, and it is barely larger than a screenshot when
    /// little changes on the screen. Open it with a web browser to play it. Programs that do not play animated PNG files display
    /// the last image of the video instead: the screen at the time of the failure. Monitors with more pixels than 2560x1600 are
    /// recorded at half their size, or less.
    /// </para>
    /// <para>
    /// The screen is recorded for as long as the driver exists, about ten times per second. This costs little while the screen does not change.
    /// </para>
    /// <para>
    /// Failures are saved either as screenshots or as videos: this property cannot be set along with <see cref="FailureScreenshotPath"/>.
    /// </para>
    /// </remarks>
    public string? FailureVideoPath
    {
        get;
        init => field = ValidateFailurePath(value);
    }

    /// <summary>
    /// Gets how much time a failure video covers: a video shows this much time before the failure, or less when the driver
    /// was created more recently. This property has no effect unless <see cref="FailureVideoPath"/> is set.
    /// Default value: 5 seconds.
    /// </summary>
    public TimeSpan FailureVideoDuration
    {
        get;
        init => field = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value), Messages.DriverOptions_Throw_FailureVideoDurationNotPositive);
    }

    = TimeSpan.FromSeconds(5);

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

    private static string? ValidateFailurePath(string? value)
    {
        if (value == null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > 0 ? trimmed : throw new ArgumentException(Messages.DriverOptions_Throw_EmptyFailurePath, nameof(value));
    }
}
