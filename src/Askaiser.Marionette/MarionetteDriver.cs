using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Askaiser.Marionette.Input;
using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Recognition;
using Askaiser.Marionette.Screen;

namespace Askaiser.Marionette;

/// <summary>
/// Automates a desktop by looking at the screen: it finds images and text on a monitor, and controls the mouse and the keyboard.
/// It works on Windows, Linux (X11) and macOS, with any application.
/// </summary>
public sealed class MarionetteDriver : IDisposable
{
    private readonly DesktopPlatform _platform;
    private readonly ScreenService _screen;
    private readonly ElementLocator _locator;
    private readonly MouseController _mouse;
    private readonly KeyboardController _keyboard;
    private readonly IDisposable[] _disposables;
    private readonly TimeProvider _timeProvider;

    private int _monitorIndex;
    private MouseSpeed _mouseSpeed;
    private TimeSpan _defaultWaitForDuration;
    private TimeSpan _defaultKeyboardSleepAfterDuration;
    private int _disposed;

    internal MarionetteDriver(
        DriverOptions options,
        DesktopPlatform platform,
        IReadOnlyList<IElementRecognizer> recognizers,
        IFailureScreenshotWriter failureWriter,
        InputTimings timings,
        TimeProvider timeProvider,
        params IDisposable[] disposables)
    {
        this._platform = platform;
        this._timeProvider = timeProvider;
        this._disposables = disposables;

        this._screen = new ScreenService(platform.Display, options.ScreenshotCacheDuration);
        this._locator = new ElementLocator(options, this._screen, recognizers, failureWriter, timeProvider);
        this._mouse = new MouseController(platform.Mouse, timings, timeProvider);
        this._keyboard = new KeyboardController(platform.Keyboard, timings);

        this._monitorIndex = 0;
        this._mouseSpeed = options.MouseSpeed;
        this._defaultWaitForDuration = options.DefaultWaitForDuration;
        this._defaultKeyboardSleepAfterDuration = options.DefaultKeyboardSleepAfterDuration;
    }

    /// <summary>
    /// Creates a driver for the desktop of the current user, with default options.
    /// </summary>
    public static MarionetteDriver Create()
    {
        return Create(new DriverOptions());
    }

    /// <summary>
    /// Creates a driver for the desktop of the current user.
    /// </summary>
    public static MarionetteDriver Create(DriverOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var platform = DesktopPlatform.Create();
        var textRecognizer = new TextElementRecognizer(options.TextRecognitionModel);
        IElementRecognizer[] recognizers = [new ImageElementRecognizer(), textRecognizer];

        return new MarionetteDriver(options, platform, recognizers, new FailureScreenshotWriter(), InputTimings.Default, TimeProvider.System, textRecognizer);
    }

    /// <summary>
    /// Returns the monitors of the desktop. The primary monitor comes first.
    /// </summary>
    public Task<MonitorDescription[]> GetMonitorsAsync()
    {
        return this._screen.GetMonitorsAsync();
    }

    /// <summary>
    /// Returns the monitor elements are searched on, which is the primary monitor unless <see cref="SetCurrentMonitor(int)"/> was called.
    /// </summary>
    public Task<MonitorDescription> GetCurrentMonitorAsync()
    {
        return this._screen.GetMonitorAsync(this._monitorIndex);
    }

    /// <summary>
    /// Captures the current monitor.
    /// </summary>
    public async Task<Image> GetScreenshotAsync()
    {
        var frame = await this._screen.AcquireFrameAsync(this._monitorIndex, CancellationToken.None).ConfigureAwait(false);
        try
        {
            return new Image(frame.Pixels.Span.ToArray(), frame.Pixels.Width, frame.Pixels.Height);
        }
        finally
        {
            frame.Release();
        }
    }

    /// <summary>
    /// Captures the current monitor and writes it to a stream as a PNG image.
    /// </summary>
    public async Task SaveScreenshotAsync(Stream destinationStream)
    {
        ArgumentNullException.ThrowIfNull(destinationStream);

        var screenshot = await this.GetScreenshotAsync().ConfigureAwait(false);
        var png = screenshot.ToPng();
        await destinationStream.WriteAsync(png).ConfigureAwait(false);
    }

    /// <summary>
    /// Captures the current monitor and saves it as a PNG file.
    /// </summary>
    public async Task SaveScreenshotAsync(string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(destinationPath);

        var screenshot = await this.GetScreenshotAsync().ConfigureAwait(false);
        await File.WriteAllBytesAsync(destinationPath, screenshot.ToPng()).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the position of the mouse cursor.
    /// </summary>
#pragma warning disable VSTHRD200 // The name is kept for compatibility with previous versions
    public Point GetMousePositionAsync()
#pragma warning restore VSTHRD200
    {
        return this._mouse.GetPosition();
    }

    /// <summary>
    /// Waits for an element to be visible on the current monitor and returns where it is.
    /// </summary>
    /// <param name="element">The image or text to look for.</param>
    /// <param name="waitFor">How long to wait for the element. When null, the default duration of the driver is used.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor. Searching a small area is much faster.</param>
    /// <exception cref="ElementNotFoundException">The element is not visible.</exception>
    /// <exception cref="MultipleElementFoundException">The element is visible at several locations.</exception>
    public Task<SearchResult> WaitForAsync(IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        return this.WaitForAsync(element, waitFor, searchRect, NoSingleResultBehavior.Throw);
    }

    /// <summary>
    /// Waits for any of the elements to be visible on the current monitor and returns where the first visible one is.
    /// </summary>
    public Task<SearchResult> WaitForAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        return this.WaitForAnyAsync(elements, waitFor, searchRect, NoSingleResultBehavior.Throw);
    }

    /// <summary>
    /// Waits for all the elements to be visible on the current monitor and returns where they are.
    /// </summary>
    public Task<SearchResultCollection> WaitForAllAsync(IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        return this.WaitForAllAsync(elements, waitFor, searchRect, NoSingleResultBehavior.Throw);
    }

    public Task MoveToAsync(int x, int y)
    {
        return this.ActAsync(this._mouse.MoveAsync(x, y, this._mouseSpeed));
    }

    public Task SingleClickAsync(int x, int y)
    {
        return this.ActAsync(this._mouse.SingleClickAsync(x, y, this._mouseSpeed));
    }

    public Task DoubleClickAsync(int x, int y)
    {
        return this.ActAsync(this._mouse.DoubleClickAsync(x, y, this._mouseSpeed));
    }

    public Task TripleClickAsync(int x, int y)
    {
        return this.ActAsync(this._mouse.TripleClickAsync(x, y, this._mouseSpeed));
    }

    public Task RightClickAsync(int x, int y)
    {
        return this.ActAsync(this._mouse.RightClickAsync(x, y, this._mouseSpeed));
    }

    /// <summary>
    /// Moves the cursor to a position and presses the left mouse button, without releasing it. Use <see cref="DropToAsync(int, int)"/> to release it.
    /// </summary>
    public Task DragFromAsync(int x, int y)
    {
        return this.ActAsync(this._mouse.DragFromAsync(x, y, this._mouseSpeed));
    }

    /// <summary>
    /// Moves the cursor to a position and releases the left mouse button.
    /// </summary>
    public Task DropToAsync(int x, int y)
    {
        return this.ActAsync(this._mouse.DropToAsync(x, y, this._mouseSpeed));
    }

    public Task ScrollUpAsync(int scrollTicks = 1)
    {
        return this.ScrollAsync(up: true, scrollTicks);
    }

    public Task ScrollDownAsync(int scrollTicks = 1)
    {
        return this.ScrollAsync(up: false, scrollTicks);
    }

    /// <summary>
    /// Scrolls up until an element is visible.
    /// </summary>
    /// <exception cref="ElementNotFoundException">The element is still not visible after <paramref name="waitFor"/>.</exception>
    public Task ScrollUpUntilVisibleAsync(IElement element, TimeSpan waitFor, int scrollTicks = 1, Rectangle? searchRect = default)
    {
        return this.ScrollUntilVisibleAsync(element, waitFor, up: true, scrollTicks, searchRect);
    }

    /// <summary>
    /// Scrolls down until an element is visible.
    /// </summary>
    /// <exception cref="ElementNotFoundException">The element is still not visible after <paramref name="waitFor"/>.</exception>
    public Task ScrollDownUntilVisibleAsync(IElement element, TimeSpan waitFor, int scrollTicks = 1, Rectangle? searchRect = default)
    {
        return this.ScrollUntilVisibleAsync(element, waitFor, up: false, scrollTicks, searchRect);
    }

    /// <summary>
    /// Types text as it is, whatever the layout of the keyboard is. Line breaks press the Enter key and tabulations press the Tab key.
    /// </summary>
    public async Task TypeTextAsync(string text, TimeSpan? sleepAfter = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length > 0)
        {
            await this.ActAsync(this._keyboard.TypeTextAsync(text)).ConfigureAwait(false);
            await this.SleepAfterKeyboardAsync(sleepAfter).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Presses and releases keys. Modifier keys (Shift, Control, Alt, Windows or Command) are held down while the other keys are pressed,
    /// which makes <c>[Control, C]</c> a shortcut.
    /// </summary>
    public async Task KeyPressAsync(VirtualKeyCode[] keyCodes, TimeSpan? sleepAfter = default)
    {
        EnsureNotNullOrEmpty(keyCodes);
        await this.ActAsync(this._keyboard.KeyPressAsync(keyCodes)).ConfigureAwait(false);
        await this.SleepAfterKeyboardAsync(sleepAfter).ConfigureAwait(false);
    }

    /// <summary>
    /// Presses keys without releasing them.
    /// </summary>
    public async Task KeyDownAsync(VirtualKeyCode[] keyCodes, TimeSpan? sleepAfter = default)
    {
        EnsureNotNullOrEmpty(keyCodes);
        await this.ActAsync(this._keyboard.KeyDownAsync(keyCodes)).ConfigureAwait(false);
        await this.SleepAfterKeyboardAsync(sleepAfter).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases keys.
    /// </summary>
    public async Task KeyUpAsync(VirtualKeyCode[] keyCodes, TimeSpan? sleepAfter = default)
    {
        EnsureNotNullOrEmpty(keyCodes);
        await this.ActAsync(this._keyboard.KeyUpAsync(keyCodes)).ConfigureAwait(false);
        await this.SleepAfterKeyboardAsync(sleepAfter).ConfigureAwait(false);
    }

    public MarionetteDriver SetMouseSpeed(MouseSpeed speed)
    {
        if (!Enum.IsDefined(speed))
        {
            throw new ArgumentOutOfRangeException(nameof(speed));
        }

        this._mouseSpeed = speed;
        return this;
    }

    /// <summary>
    /// Changes the monitor elements are searched on. Index zero is the primary monitor.
    /// </summary>
    public MarionetteDriver SetCurrentMonitor(int monitorIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monitorIndex);

        this._monitorIndex = monitorIndex;
        return this;
    }

    /// <summary>
    /// Changes the monitor elements are searched on.
    /// </summary>
    public MarionetteDriver SetCurrentMonitor(MonitorDescription monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        return this.SetCurrentMonitor(monitor.Index);
    }

    public MarionetteDriver SetDefaultWaitForDuration(TimeSpan defaultWaitFor)
    {
        if (defaultWaitFor < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultWaitFor), Messages.DriverOptions_Throw_NegativeDefaultWaitForDuration);
        }

        this._defaultWaitForDuration = defaultWaitFor;
        return this;
    }

    public MarionetteDriver SetDefaultKeyboardSleepAfterDuration(TimeSpan defaultKeyboardSleepAfter)
    {
        if (defaultKeyboardSleepAfter < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultKeyboardSleepAfter), Messages.DriverOptions_Throw_NegativeDefaultKeyboardSleepAfterDuration);
        }

        this._defaultKeyboardSleepAfterDuration = defaultKeyboardSleepAfter;
        return this;
    }

    public Task SleepAsync(int millisecondsDelay)
    {
        return Task.Delay(TimeSpan.FromMilliseconds(millisecondsDelay), this._timeProvider);
    }

    public Task SleepAsync(TimeSpan delay)
    {
        return Task.Delay(delay, this._timeProvider);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this._disposed, 1) != 0)
        {
            return;
        }

        this._screen.Dispose();

        foreach (var disposable in this._disposables)
        {
            disposable.Dispose();
        }

        this._platform.Dispose();
    }

    internal Task<SearchResult> WaitForAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect, NoSingleResultBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(element);
        return this._locator.WaitForAsync(element, waitFor ?? this._defaultWaitForDuration, searchRect, this._monitorIndex, behavior);
    }

    internal Task<SearchResult> WaitForAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect, NoSingleResultBehavior behavior)
    {
        return this._locator.WaitForAnyAsync(ToNonEmptyList(elements), waitFor ?? this._defaultWaitForDuration, searchRect, this._monitorIndex, behavior);
    }

    internal Task<SearchResultCollection> WaitForAllAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect, NoSingleResultBehavior behavior)
    {
        return this._locator.WaitForAllAsync(ToNonEmptyList(elements), waitFor ?? this._defaultWaitForDuration, searchRect, this._monitorIndex, behavior);
    }

    private static List<IElement> ToNonEmptyList(IEnumerable<IElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);

        var list = new List<IElement>(elements);
        return list.Count > 0 ? list : throw new ArgumentException(Messages.MarionetteDriver_Throw_ElementsEmpty, nameof(elements));
    }

    private static void EnsureNotNullOrEmpty(VirtualKeyCode[] keyCodes)
    {
        ArgumentNullException.ThrowIfNull(keyCodes);

        if (keyCodes.Length == 0)
        {
            throw new ArgumentException(Messages.MarionetteDriver_Throw_EmptyKeyCodes, nameof(keyCodes));
        }
    }

    /// <summary>
    /// Runs a mouse or keyboard action. The screen most likely changes as a result, so the last screenshot is not reused afterwards.
    /// </summary>
    private async Task ActAsync(Task action)
    {
        try
        {
            await action.ConfigureAwait(false);
        }
        finally
        {
            this._screen.Invalidate();
        }
    }

    private async Task ScrollAsync(bool up, int scrollTicks)
    {
        if (scrollTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scrollTicks), Messages.MarionetteDriver_Throw_ScrollTicksNotGreaterThanZero);
        }

        for (var i = 0; i < scrollTicks; i++)
        {
            await this.ActAsync(this._mouse.ScrollAsync(up)).ConfigureAwait(false);
        }
    }

    private async Task ScrollUntilVisibleAsync(IElement element, TimeSpan waitFor, bool up, int scrollTicks, Rectangle? searchRect)
    {
        if (waitFor <= TimeSpan.Zero)
        {
            throw new ArgumentException(Messages.Throw_NegativeWaitFor, nameof(waitFor));
        }

        var startTimestamp = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(startTimestamp) < waitFor)
        {
            var result = await this.WaitForAsync(element, TimeSpan.Zero, searchRect, NoSingleResultBehavior.Ignore).ConfigureAwait(false);
            if (result.Success)
            {
                return;
            }

            await this.ScrollAsync(up, scrollTicks).ConfigureAwait(false);
        }

        throw new ElementNotFoundException(element, waitFor);
    }

    private Task SleepAfterKeyboardAsync(TimeSpan? sleepAfter)
    {
        var duration = sleepAfter ?? this._defaultKeyboardSleepAfterDuration;
        return duration > TimeSpan.Zero ? this.SleepAsync(duration) : Task.CompletedTask;
    }
}
