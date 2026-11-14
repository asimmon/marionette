using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Input;
using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Recognition;
using Askaiser.Marionette.Screen;

namespace Askaiser.Marionette;

/// <summary>
/// Automates a desktop by looking at the screen: it finds images and text on a monitor, and controls the mouse and the keyboard.
/// It works on Windows, Linux (X11) and macOS, with any application.
/// </summary>
/// <remarks>
/// <para>
/// Coordinates are in pixels of the virtual desktop that spans all monitors, whose origin is the top-left corner of the primary monitor.
/// </para>
/// <para>
/// Every asynchronous method accepts a <see cref="CancellationToken"/> and throws an <see cref="OperationCanceledException"/> when it is canceled.
/// Cancellation stops waiting, searching and moving right away. It never leaves the desktop in a state the method was not asked for:
/// a canceled click releases the mouse button it pressed, and a canceled shortcut releases its keys.
/// </para>
/// </remarks>
public sealed class MarionetteDriver : IDisposable
{
    private readonly DesktopPlatform _platform;
    private readonly ScreenService _screen;
    private readonly ScreenRecorder? _recorder;
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
        IFailureFileWriter failureWriter,
        InputTimings timings,
        ScreenRecorderSettings recorderSettings,
        TimeProvider timeProvider,
        params IDisposable[] disposables)
    {
        if (options.FailureScreenshotPath != null && options.FailureVideoPath != null)
        {
            throw new ArgumentException(Messages.DriverOptions_Throw_FailureScreenshotAndVideo, nameof(options));
        }

        this._platform = platform;
        this._timeProvider = timeProvider;
        this._disposables = disposables;

        this._screen = new ScreenService(platform.Display, options.ScreenshotCacheDuration);
        this._recorder = options.FailureVideoPath != null ? new ScreenRecorder(this._screen, options.FailureVideoDuration, recorderSettings, timeProvider) : null;
        this._locator = new ElementLocator(options, this._screen, recognizers, failureWriter, this._recorder, timeProvider);
        this._mouse = new MouseController(platform.Mouse, timings, timeProvider);
        this._keyboard = new KeyboardController(platform.Keyboard, timings);

        this._monitorIndex = 0;
        this._mouseSpeed = options.MouseSpeed;
        this._defaultWaitForDuration = options.DefaultWaitForDuration;
        this._defaultKeyboardSleepAfterDuration = options.DefaultKeyboardSleepAfterDuration;

        this._recorder?.Start();
    }

    /// <summary>
    /// Creates a driver for the desktop of the current user, with default options.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">The operating system is not Windows, Linux with X11, or macOS.</exception>
    public static MarionetteDriver Create()
    {
        return Create(new DriverOptions());
    }

    /// <summary>
    /// Creates a driver for the desktop of the current user.
    /// </summary>
    /// <param name="options">The settings of the driver.</param>
    /// <exception cref="ArgumentException">The options ask for both screenshots and videos of failures.</exception>
    /// <exception cref="PlatformNotSupportedException">The operating system is not Windows, Linux with X11, or macOS.</exception>
    public static MarionetteDriver Create(DriverOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.FailureScreenshotPath != null && options.FailureVideoPath != null)
        {
            throw new ArgumentException(Messages.DriverOptions_Throw_FailureScreenshotAndVideo, nameof(options));
        }

        var platform = DesktopPlatform.Create();
        var textRecognizer = new TextElementRecognizer(options.TextRecognitionModel);
        IElementRecognizer[] recognizers = [new ImageElementRecognizer(), textRecognizer];

        return new MarionetteDriver(options, platform, recognizers, new FailureFileWriter(), InputTimings.Default, ScreenRecorderSettings.Default, TimeProvider.System, textRecognizer);
    }

    /// <summary>
    /// Returns the monitors of the desktop. The primary monitor comes first.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task<MonitorDescription[]> GetMonitorsAsync(CancellationToken cancellationToken = default)
    {
        return this._screen.GetMonitorsAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the monitor elements are searched on, which is the primary monitor unless <see cref="SetCurrentMonitor(int)"/> was called.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">The current monitor does not exist.</exception>
    public Task<MonitorDescription> GetCurrentMonitorAsync(CancellationToken cancellationToken = default)
    {
        return this._screen.GetMonitorAsync(this._monitorIndex, cancellationToken);
    }

    /// <summary>
    /// Captures the current monitor.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<Image> GetScreenshotAsync(CancellationToken cancellationToken = default)
    {
        var frame = await this._screen.AcquireFrameAsync(this._monitorIndex, cancellationToken).ConfigureAwait(false);
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
    /// <param name="destinationStream">The stream to write to. It is left open.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task SaveScreenshotAsync(Stream destinationStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destinationStream);

        var png = await this.CapturePngAsync(cancellationToken).ConfigureAwait(false);
        await destinationStream.WriteAsync(png, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Captures the current monitor and saves it as a PNG file.
    /// </summary>
    /// <param name="destinationPath">The path of the file, which is overwritten when it exists.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task SaveScreenshotAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destinationPath);

        var png = await this.CapturePngAsync(cancellationToken).ConfigureAwait(false);
        await File.WriteAllBytesAsync(destinationPath, png, cancellationToken).ConfigureAwait(false);
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
    /// <param name="cancellationToken">A token to stop waiting.</param>
    /// <exception cref="ElementNotFoundException">The element is not visible.</exception>
    /// <exception cref="MultipleElementFoundException">The element is visible at several locations.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public Task<SearchResult> WaitForAsync(IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        return this.WaitForAsync(element, waitFor, searchRect, NoSingleResultBehavior.Throw, cancellationToken);
    }

    /// <summary>
    /// Waits for any of the elements to be visible on the current monitor and returns where the first visible one is, in the order they were given.
    /// </summary>
    /// <param name="elements">The images or texts to look for.</param>
    /// <param name="waitFor">How long to wait for an element. When null, the default duration of the driver is used.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor. Searching a small area is much faster.</param>
    /// <param name="cancellationToken">A token to stop waiting.</param>
    /// <exception cref="ElementNotFoundException">None of the elements is visible.</exception>
    /// <exception cref="MultipleElementFoundException">The first visible element is visible at several locations.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public Task<SearchResult> WaitForAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        return this.WaitForAnyAsync(elements, waitFor, searchRect, NoSingleResultBehavior.Throw, cancellationToken);
    }

    /// <summary>
    /// Waits for all the elements to be visible on the current monitor and returns where they are.
    /// An element that was seen once is considered found, even if it disappears while waiting for the others.
    /// </summary>
    /// <param name="elements">The images or texts to look for.</param>
    /// <param name="waitFor">How long to wait for the elements. When null, the default duration of the driver is used.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor. Searching a small area is much faster.</param>
    /// <param name="cancellationToken">A token to stop waiting.</param>
    /// <exception cref="ElementNotFoundException">An element is not visible.</exception>
    /// <exception cref="MultipleElementFoundException">An element is visible at several locations.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public Task<SearchResultCollection> WaitForAllAsync(IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        return this.WaitForAllAsync(elements, waitFor, searchRect, NoSingleResultBehavior.Throw, cancellationToken);
    }

    /// <summary>
    /// Moves the mouse cursor to a position, at the current speed of the mouse.
    /// </summary>
    /// <param name="x">The horizontal position, in pixels of the virtual desktop.</param>
    /// <param name="y">The vertical position, in pixels of the virtual desktop.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return this.ActAsync(this._mouse.MoveAsync(x, y, this._mouseSpeed, cancellationToken));
    }

    /// <summary>
    /// Moves the mouse cursor to a position and clicks the left button.
    /// </summary>
    /// <param name="x">The horizontal position, in pixels of the virtual desktop.</param>
    /// <param name="y">The vertical position, in pixels of the virtual desktop.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task SingleClickAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return this.ActAsync(this._mouse.SingleClickAsync(x, y, this._mouseSpeed, cancellationToken));
    }

    /// <summary>
    /// Moves the mouse cursor to a position and clicks the left button twice.
    /// </summary>
    /// <param name="x">The horizontal position, in pixels of the virtual desktop.</param>
    /// <param name="y">The vertical position, in pixels of the virtual desktop.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task DoubleClickAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return this.ActAsync(this._mouse.DoubleClickAsync(x, y, this._mouseSpeed, cancellationToken));
    }

    /// <summary>
    /// Moves the mouse cursor to a position and clicks the left button three times.
    /// </summary>
    /// <param name="x">The horizontal position, in pixels of the virtual desktop.</param>
    /// <param name="y">The vertical position, in pixels of the virtual desktop.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task TripleClickAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return this.ActAsync(this._mouse.TripleClickAsync(x, y, this._mouseSpeed, cancellationToken));
    }

    /// <summary>
    /// Moves the mouse cursor to a position and clicks the right button.
    /// </summary>
    /// <param name="x">The horizontal position, in pixels of the virtual desktop.</param>
    /// <param name="y">The vertical position, in pixels of the virtual desktop.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task RightClickAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return this.ActAsync(this._mouse.RightClickAsync(x, y, this._mouseSpeed, cancellationToken));
    }

    /// <summary>
    /// Moves the cursor to a position and presses the left mouse button, without releasing it. Use <see cref="DropToAsync(int, int, CancellationToken)"/> to release it.
    /// </summary>
    /// <param name="x">The horizontal position, in pixels of the virtual desktop.</param>
    /// <param name="y">The vertical position, in pixels of the virtual desktop.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task DragFromAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return this.ActAsync(this._mouse.DragFromAsync(x, y, this._mouseSpeed, cancellationToken));
    }

    /// <summary>
    /// Moves the cursor to a position and releases the left mouse button.
    /// </summary>
    /// <param name="x">The horizontal position, in pixels of the virtual desktop.</param>
    /// <param name="y">The vertical position, in pixels of the virtual desktop.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task DropToAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return this.ActAsync(this._mouse.DropToAsync(x, y, this._mouseSpeed, cancellationToken));
    }

    /// <summary>
    /// Turns the mouse wheel up, at the position of the cursor.
    /// </summary>
    /// <param name="scrollTicks">The number of notches of the wheel.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scrollTicks"/> is not greater than zero.</exception>
    public Task ScrollUpAsync(int scrollTicks = 1, CancellationToken cancellationToken = default)
    {
        return this.ScrollAsync(up: true, scrollTicks, cancellationToken);
    }

    /// <summary>
    /// Turns the mouse wheel down, at the position of the cursor.
    /// </summary>
    /// <param name="scrollTicks">The number of notches of the wheel.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scrollTicks"/> is not greater than zero.</exception>
    public Task ScrollDownAsync(int scrollTicks = 1, CancellationToken cancellationToken = default)
    {
        return this.ScrollAsync(up: false, scrollTicks, cancellationToken);
    }

    /// <summary>
    /// Scrolls up until an element is visible.
    /// </summary>
    /// <param name="element">The image or text to look for.</param>
    /// <param name="waitFor">How long to keep scrolling.</param>
    /// <param name="scrollTicks">The number of notches of the wheel between two searches.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ElementNotFoundException">The element is still not visible after <paramref name="waitFor"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public Task ScrollUpUntilVisibleAsync(IElement element, TimeSpan waitFor, int scrollTicks = 1, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        return this.ScrollUntilVisibleAsync(element, waitFor, up: true, scrollTicks, searchRect, cancellationToken);
    }

    /// <summary>
    /// Scrolls down until an element is visible.
    /// </summary>
    /// <param name="element">The image or text to look for.</param>
    /// <param name="waitFor">How long to keep scrolling.</param>
    /// <param name="scrollTicks">The number of notches of the wheel between two searches.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ElementNotFoundException">The element is still not visible after <paramref name="waitFor"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public Task ScrollDownUntilVisibleAsync(IElement element, TimeSpan waitFor, int scrollTicks = 1, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        return this.ScrollUntilVisibleAsync(element, waitFor, up: false, scrollTicks, searchRect, cancellationToken);
    }

    /// <summary>
    /// Types text as it is, whatever the layout of the keyboard is. Line breaks press the Enter key and tabulations press the Tab key.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <param name="sleepAfter">How long to pause once the text is typed. When null, the default duration of the driver is used.</param>
    /// <param name="cancellationToken">A token to cancel the operation. Text that was already typed stays typed.</param>
    public async Task TypeTextAsync(string text, TimeSpan? sleepAfter = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length > 0)
        {
            await this.ActAsync(this._keyboard.TypeTextAsync(text, cancellationToken)).ConfigureAwait(false);
            await this.SleepAfterKeyboardAsync(sleepAfter, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Presses and releases keys. Modifier keys (Shift, Control, Alt, Windows or Command) are held down while the other keys are pressed,
    /// which makes <c>[Control, C]</c> a shortcut.
    /// </summary>
    /// <param name="keyCodes">The keys to press.</param>
    /// <param name="sleepAfter">How long to pause once the keys are released. When null, the default duration of the driver is used.</param>
    /// <param name="cancellationToken">A token to cancel the operation. Keys that are down are released.</param>
    public async Task KeyPressAsync(VirtualKeyCode[] keyCodes, TimeSpan? sleepAfter = default, CancellationToken cancellationToken = default)
    {
        EnsureNotNullOrEmpty(keyCodes);
        await this.ActAsync(this._keyboard.KeyPressAsync(keyCodes, cancellationToken)).ConfigureAwait(false);
        await this.SleepAfterKeyboardAsync(sleepAfter, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Presses keys without releasing them. Use <see cref="KeyUpAsync(VirtualKeyCode[], TimeSpan?, CancellationToken)"/> to release them.
    /// </summary>
    /// <param name="keyCodes">The keys to press.</param>
    /// <param name="sleepAfter">How long to pause once the keys are down. When null, the default duration of the driver is used.</param>
    /// <param name="cancellationToken">A token to cancel the operation. Keys that are already down stay down.</param>
    public async Task KeyDownAsync(VirtualKeyCode[] keyCodes, TimeSpan? sleepAfter = default, CancellationToken cancellationToken = default)
    {
        EnsureNotNullOrEmpty(keyCodes);
        await this.ActAsync(this._keyboard.KeyDownAsync(keyCodes, cancellationToken)).ConfigureAwait(false);
        await this.SleepAfterKeyboardAsync(sleepAfter, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases keys.
    /// </summary>
    /// <param name="keyCodes">The keys to release.</param>
    /// <param name="sleepAfter">How long to pause once the keys are released. When null, the default duration of the driver is used.</param>
    /// <param name="cancellationToken">A token to cancel the operation. Once the first key is released, the others are too.</param>
    public async Task KeyUpAsync(VirtualKeyCode[] keyCodes, TimeSpan? sleepAfter = default, CancellationToken cancellationToken = default)
    {
        EnsureNotNullOrEmpty(keyCodes);
        await this.ActAsync(this._keyboard.KeyUpAsync(keyCodes, cancellationToken)).ConfigureAwait(false);
        await this.SleepAfterKeyboardAsync(sleepAfter, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Changes how fast the mouse cursor travels to its destination.
    /// </summary>
    /// <returns>This driver, to chain calls.</returns>
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
    /// <param name="monitorIndex">The index of the monitor, as returned by <see cref="GetMonitorsAsync"/>.</param>
    /// <returns>This driver, to chain calls.</returns>
    public MarionetteDriver SetCurrentMonitor(int monitorIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monitorIndex);

        this._monitorIndex = monitorIndex;
        this._recorder?.MonitorIndex = monitorIndex;
        return this;
    }

    /// <summary>
    /// Changes the monitor elements are searched on.
    /// </summary>
    /// <param name="monitor">A monitor returned by <see cref="GetMonitorsAsync"/>.</param>
    /// <returns>This driver, to chain calls.</returns>
    public MarionetteDriver SetCurrentMonitor(MonitorDescription monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        return this.SetCurrentMonitor(monitor.Index);
    }

    /// <summary>
    /// Changes how long methods wait for an element when they are not given a <c>waitFor</c> duration.
    /// </summary>
    /// <returns>This driver, to chain calls.</returns>
    public MarionetteDriver SetDefaultWaitForDuration(TimeSpan defaultWaitFor)
    {
        if (defaultWaitFor < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultWaitFor), Messages.DriverOptions_Throw_NegativeDefaultWaitForDuration);
        }

        this._defaultWaitForDuration = defaultWaitFor;
        return this;
    }

    /// <summary>
    /// Changes how long keyboard methods pause after typing when they are not given a <c>sleepAfter</c> duration.
    /// </summary>
    /// <returns>This driver, to chain calls.</returns>
    public MarionetteDriver SetDefaultKeyboardSleepAfterDuration(TimeSpan defaultKeyboardSleepAfter)
    {
        if (defaultKeyboardSleepAfter < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultKeyboardSleepAfter), Messages.DriverOptions_Throw_NegativeDefaultKeyboardSleepAfterDuration);
        }

        this._defaultKeyboardSleepAfterDuration = defaultKeyboardSleepAfter;
        return this;
    }

    /// <summary>
    /// Waits for a number of milliseconds.
    /// </summary>
    /// <param name="millisecondsDelay">How long to wait, in milliseconds.</param>
    /// <param name="cancellationToken">A token to stop waiting.</param>
    public Task SleepAsync(int millisecondsDelay, CancellationToken cancellationToken = default)
    {
        return Task.Delay(TimeSpan.FromMilliseconds(millisecondsDelay), this._timeProvider, cancellationToken);
    }

    /// <summary>
    /// Waits for a duration.
    /// </summary>
    /// <param name="delay">How long to wait.</param>
    /// <param name="cancellationToken">A token to stop waiting.</param>
    public Task SleepAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return Task.Delay(delay, this._timeProvider, cancellationToken);
    }

    /// <summary>
    /// Releases the resources of the driver: it stops recording the screen and unloads the text recognition model.
    /// Operations in progress should be completed or canceled first.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref this._disposed, 1) != 0)
        {
            return;
        }

        this._recorder?.Dispose();
        this._screen.Dispose();

        foreach (var disposable in this._disposables)
        {
            disposable.Dispose();
        }

        this._platform.Dispose();
    }

    internal Task<SearchResult> WaitForAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect, NoSingleResultBehavior behavior, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(element);
        return this._locator.WaitForAsync(element, waitFor ?? this._defaultWaitForDuration, searchRect, this._monitorIndex, behavior, cancellationToken);
    }

    internal Task<SearchResult> WaitForAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect, NoSingleResultBehavior behavior, CancellationToken cancellationToken)
    {
        return this._locator.WaitForAnyAsync(ToNonEmptyList(elements), waitFor ?? this._defaultWaitForDuration, searchRect, this._monitorIndex, behavior, cancellationToken);
    }

    internal Task<SearchResultCollection> WaitForAllAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect, NoSingleResultBehavior behavior, CancellationToken cancellationToken)
    {
        return this._locator.WaitForAllAsync(ToNonEmptyList(elements), waitFor ?? this._defaultWaitForDuration, searchRect, this._monitorIndex, behavior, cancellationToken);
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
    /// Captures the current monitor and encodes it as PNG, straight from the capture.
    /// </summary>
    private async Task<byte[]> CapturePngAsync(CancellationToken cancellationToken)
    {
        var frame = await this._screen.AcquireFrameAsync(this._monitorIndex, cancellationToken).ConfigureAwait(false);
        try
        {
            return ImageCodec.EncodePng(frame.Pixels.Span, frame.Pixels.Width, frame.Pixels.Height);
        }
        finally
        {
            frame.Release();
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

    private async Task ScrollAsync(bool up, int scrollTicks, CancellationToken cancellationToken)
    {
        if (scrollTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scrollTicks), Messages.MarionetteDriver_Throw_ScrollTicksNotGreaterThanZero);
        }

        for (var i = 0; i < scrollTicks; i++)
        {
            await this.ActAsync(this._mouse.ScrollAsync(up, cancellationToken)).ConfigureAwait(false);
        }
    }

    private async Task ScrollUntilVisibleAsync(IElement element, TimeSpan waitFor, bool up, int scrollTicks, Rectangle? searchRect, CancellationToken cancellationToken)
    {
        if (waitFor <= TimeSpan.Zero)
        {
            throw new ArgumentException(Messages.Throw_NegativeWaitFor, nameof(waitFor));
        }

        var startTimestamp = this._timeProvider.GetTimestamp();
        while (this._timeProvider.GetElapsedTime(startTimestamp) < waitFor)
        {
            var result = await this.WaitForAsync(element, TimeSpan.Zero, searchRect, NoSingleResultBehavior.Ignore, cancellationToken).ConfigureAwait(false);
            if (result.Success)
            {
                return;
            }

            await this.ScrollAsync(up, scrollTicks, cancellationToken).ConfigureAwait(false);
        }

        throw new ElementNotFoundException(element, waitFor);
    }

    private Task SleepAfterKeyboardAsync(TimeSpan? sleepAfter, CancellationToken cancellationToken)
    {
        var duration = sleepAfter ?? this._defaultKeyboardSleepAfterDuration;
        return duration > TimeSpan.Zero ? this.SleepAsync(duration, cancellationToken) : Task.CompletedTask;
    }
}
