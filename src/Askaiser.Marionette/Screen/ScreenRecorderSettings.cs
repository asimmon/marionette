using System;

namespace Askaiser.Marionette.Screen;

/// <summary>
/// How a <see cref="ScreenRecorder"/> trades the quality of its videos for processor time and memory.
/// </summary>
internal sealed record ScreenRecorderSettings
{
    public static ScreenRecorderSettings Default { get; } = new ScreenRecorderSettings();

    /// <summary>Gets the time between two captures of the screen. The default is ten images per second, enough to follow what a test does.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets the share of a processor core the recorder may use. A capture that takes long, because the screen is large or
    /// changes a lot, delays the next one accordingly: the video loses images rather than slowing down what is being recorded.
    /// </summary>
    public double MaxProcessorShare { get; init; } = 0.25;

    /// <summary>
    /// Gets the number of pixels above which a monitor is recorded at a fraction of its size. The default keeps monitors up to
    /// 2560x1600 as they are, and halves 4K and 5K monitors, on which user interfaces are scaled up anyway.
    /// </summary>
    public int MaxPixels { get; init; } = 2560 * 1600;

    /// <summary>
    /// Gets the amount of memory compressed images may use. The oldest images are dropped beyond that, which only happens
    /// when the whole screen keeps changing, like when it plays a movie.
    /// </summary>
    public long MaxBufferedBytes { get; init; } = 64 * 1024 * 1024;

    /// <summary>Gets how long the last image of a video stays on screen before the video starts again.</summary>
    public TimeSpan FinalImageHold { get; init; } = TimeSpan.FromSeconds(1.5);
}
