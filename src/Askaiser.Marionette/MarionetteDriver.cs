using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Askaiser.Marionette.Commands;

[assembly: InternalsVisibleTo("Askaiser.Marionette.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Askaiser.Marionette;

public sealed class MarionetteDriver : IDisposable
{
    private readonly IMonitorService _monitorService;
    private readonly IMouseController _mouseController;
    private readonly IDisposable[] _disposables;
    private readonly WaitForCommandHandler _waitForHandler;
    private readonly WaitForAnyCommandHandler _waitForAnyHandler;
    private readonly WaitForAllCommandHandler _waitForAllHandler;
    private readonly MoveToLocationCommandHandler _moveToLocationHandler;
    private readonly SingleClickLocationCommandHandler _singleClickLocationHandler;
    private readonly DoubleClickLocationCommandHandler _doubleClickLocationHandler;
    private readonly TripleClickLocationCommandHandler _tripleClickLocationHandler;
    private readonly RightClickLocationCommandHandler _rightClickLocationHandler;
    private readonly DragLocationCommandHandler _dragLocationHandler;
    private readonly DropLocationCommandHandler _dropLocationHandler;
    private readonly MouseWheelCommandHandler _mouseWheelHandler;
    private readonly TypeTextCommandHandler _typeTextHandler;
    private readonly KeyPressCommandHandler _keyPressHandler;
    private readonly KeyDownCommandHandler _keyDownHandler;
    private readonly KeyUpCommandHandler _keyUpHandler;

    private int _monitorIndex;
    private MouseSpeed _mouseSpeed;
    private TimeSpan _defaultWaitForDuration;
    private TimeSpan _defaultKeyboardSleepAfterDuration;

    internal MarionetteDriver(
        DriverOptions options,
        IFileWriter fileWriter,
        IMonitorService monitorService,
        IElementRecognizer elementRecognizer,
        IMouseController mouseController,
        IKeyboardController keyboardController,
        params IDisposable[] disposables)
    {
        this._monitorService = monitorService;
        this._mouseController = mouseController;
        this._disposables = disposables;

        this._waitForHandler = new WaitForCommandHandler(options, fileWriter, monitorService, elementRecognizer);
        this._waitForAnyHandler = new WaitForAnyCommandHandler(options, fileWriter, monitorService, elementRecognizer);
        this._waitForAllHandler = new WaitForAllCommandHandler(options, fileWriter, monitorService, elementRecognizer);
        this._moveToLocationHandler = new MoveToLocationCommandHandler(mouseController);
        this._singleClickLocationHandler = new SingleClickLocationCommandHandler(mouseController);
        this._doubleClickLocationHandler = new DoubleClickLocationCommandHandler(mouseController);
        this._tripleClickLocationHandler = new TripleClickLocationCommandHandler(mouseController);
        this._rightClickLocationHandler = new RightClickLocationCommandHandler(mouseController);
        this._dragLocationHandler = new DragLocationCommandHandler(mouseController);
        this._dropLocationHandler = new DropLocationCommandHandler(mouseController);
        this._mouseWheelHandler = new MouseWheelCommandHandler(mouseController);
        this._typeTextHandler = new TypeTextCommandHandler(keyboardController);
        this._keyPressHandler = new KeyPressCommandHandler(keyboardController);
        this._keyDownHandler = new KeyDownCommandHandler(keyboardController);
        this._keyUpHandler = new KeyUpCommandHandler(keyboardController);

        this._monitorIndex = 0;
        this._mouseSpeed = options.MouseSpeed;
        this._defaultWaitForDuration = options.DefaultWaitForDuration;
        this._defaultKeyboardSleepAfterDuration = options.DefaultKeyboardSleepAfterDuration;
    }

    public static MarionetteDriver Create()
    {
        return Create(new DriverOptions());
    }

    public static MarionetteDriver Create(DriverOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var monitorService = new MonitorService(options.ScreenshotCacheDuration);
        var imageRecognizer = new ImageElementRecognizer();
        var textRecognizer = new TextElementRecognizer(options);
        var elementRecognizer = new AggregateElementRecognizer(imageRecognizer, textRecognizer);
        var mouseController = new MouseController();
        var keyboardController = new KeyboardController();
        var fileWriter = new RealFileWriter();

        return new MarionetteDriver(options, fileWriter, monitorService, elementRecognizer, mouseController, keyboardController, textRecognizer);
    }

    public async Task<MonitorDescription[]> GetMonitors()
    {
        return await this._monitorService.GetMonitors().ConfigureAwait(false);
    }

    public async Task<MonitorDescription> GetCurrentMonitor()
    {
        var monitors = await this.GetMonitors().ConfigureAwait(false);
        return monitors.FirstOrDefault(x => x.Index == this._monitorIndex);
    }

    public async Task<Bitmap> GetScreenshot()
    {
        var monitor = await this.GetCurrentMonitor().ConfigureAwait(false);
        return await this._monitorService.GetScreenshot(monitor).ConfigureAwait(false);
    }

    public Point GetMousePosition()
    {
        return this._mouseController.GetCurrentPosition();
    }

    internal async Task<SearchResult> WaitFor(IElement element, TimeSpan? waitFor, Rectangle searchRect, NoSingleResultBehavior noSingleResultBehavior)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        var effectiveWaitFor = waitFor.GetValueOrDefault(this._defaultWaitForDuration);
        return await this._waitForHandler.Execute(new WaitForCommand(new[] { element }, effectiveWaitFor, searchRect, this._monitorIndex, noSingleResultBehavior)).ConfigureAwait(false);
    }

    public async Task<SearchResult> WaitFor(IElement element, TimeSpan? waitFor = default, Rectangle searchRect = default)
    {
        return await this.WaitFor(element, waitFor, searchRect, NoSingleResultBehavior.Throw).ConfigureAwait(false);
    }

    internal async Task<SearchResult> WaitForAny(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle searchRect, NoSingleResultBehavior noSingleResultBehavior)
    {
        if (elements == null)
        {
            throw new ArgumentNullException(nameof(elements));
        }

        var enumeratedElements = new List<IElement>(elements);
        if (enumeratedElements.Count == 0)
        {
            throw new ArgumentException(Messages.MarionetteDriver_Throw_ElementsEmpty, nameof(elements));
        }

        var effectiveWaitFor = waitFor.GetValueOrDefault(this._defaultWaitForDuration);
        return await this._waitForAnyHandler.Execute(new WaitForCommand(enumeratedElements, effectiveWaitFor, searchRect, this._monitorIndex, noSingleResultBehavior)).ConfigureAwait(false);
    }

    public async Task<SearchResult> WaitForAny(IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle searchRect = default)
    {
        return await this.WaitForAny(elements, waitFor, searchRect, NoSingleResultBehavior.Throw).ConfigureAwait(false);
    }

    internal async Task<SearchResultCollection> WaitForAll(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle searchRect, NoSingleResultBehavior noSingleResultBehavior)
    {
        if (elements == null)
        {
            throw new ArgumentNullException(nameof(elements));
        }

        var enumeratedElements = new List<IElement>(elements);
        if (enumeratedElements.Count == 0)
        {
            throw new ArgumentException(Messages.MarionetteDriver_Throw_ElementsEmpty, nameof(elements));
        }

        var effectiveWaitFor = waitFor.GetValueOrDefault(this._defaultWaitForDuration);
        return await this._waitForAllHandler.Execute(new WaitForCommand(enumeratedElements, effectiveWaitFor, searchRect, this._monitorIndex, noSingleResultBehavior)).ConfigureAwait(false);
    }

    public async Task<SearchResultCollection> WaitForAll(IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle searchRect = default)
    {
        return await this.WaitForAll(elements, waitFor, searchRect, NoSingleResultBehavior.Throw).ConfigureAwait(false);
    }

    public async Task MoveTo(int x, int y)
    {
        await this._moveToLocationHandler.Execute(new MouseLocationCommand(x, y, this._mouseSpeed)).ConfigureAwait(false);
    }

    public async Task SingleClick(int x, int y)
    {
        await this._singleClickLocationHandler.Execute(new MouseLocationCommand(x, y, this._mouseSpeed)).ConfigureAwait(false);
    }

    public async Task DoubleClick(int x, int y)
    {
        await this._doubleClickLocationHandler.Execute(new MouseLocationCommand(x, y, this._mouseSpeed)).ConfigureAwait(false);
    }

    public async Task TripleClick(int x, int y)
    {
        await this._tripleClickLocationHandler.Execute(new MouseLocationCommand(x, y, this._mouseSpeed)).ConfigureAwait(false);
    }

    public async Task RightClick(int x, int y)
    {
        await this._rightClickLocationHandler.Execute(new MouseLocationCommand(x, y, this._mouseSpeed)).ConfigureAwait(false);
    }

    public async Task DragFrom(int x, int y)
    {
        await this._dragLocationHandler.Execute(new MouseLocationCommand(x, y, this._mouseSpeed)).ConfigureAwait(false);
    }

    public async Task DropTo(int x, int y)
    {
        await this._dropLocationHandler.Execute(new MouseLocationCommand(x, y, this._mouseSpeed)).ConfigureAwait(false);
    }

    public async Task ScrollUp(int scrollTicks = 1)
    {
        if (scrollTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scrollTicks), Messages.MarionetteDriver_Throw_ScrollTicksNotGreaterThanZero);
        }

        for (var i = 0; i < scrollTicks; i++)
        {
            await this._mouseWheelHandler.Execute(new MouseWheelCommand(IsUp: true)).ConfigureAwait(false);
        }
    }

    public async Task ScrollDown(int scrollTicks = 1)
    {
        if (scrollTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scrollTicks), Messages.MarionetteDriver_Throw_ScrollTicksNotGreaterThanZero);
        }

        for (var i = 0; i < scrollTicks; i++)
        {
            await this._mouseWheelHandler.Execute(new MouseWheelCommand(IsUp: false)).ConfigureAwait(false);
        }
    }

    public Task ScrollUpUntilVisible(IElement element, TimeSpan waitFor, int scrollTicks = 1, Rectangle searchRect = default)
    {
        return this.ScrollUntilVisible(element, waitFor, isUp: true, scrollTicks, searchRect);
    }

    public Task ScrollDownUntilVisible(IElement element, TimeSpan waitFor, int scrollTicks = 1, Rectangle searchRect = default)
    {
        return this.ScrollUntilVisible(element, waitFor, isUp: false, scrollTicks, searchRect);
    }

    private async Task ScrollUntilVisible(IElement element, TimeSpan waitFor, bool isUp, int scrollTicks, Rectangle searchRect)
    {
        if (waitFor <= TimeSpan.Zero)
        {
            throw new ArgumentException(Messages.Throw_NegativeWaitFor, nameof(waitFor));
        }

        for (var sw = Stopwatch.StartNew(); sw.Elapsed < waitFor;)
        {
            if (await this.IsVisible(element, TimeSpan.Zero, searchRect).ConfigureAwait(false))
            {
                return;
            }

            var scrollTask = isUp ? this.ScrollUp(scrollTicks) : this.ScrollDown(scrollTicks);
            await scrollTask.ConfigureAwait(false);
        }

        throw new ElementNotFoundException(element, waitFor);
    }

    public async Task TypeText(string text, TimeSpan? sleepAfter = default)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (text.Length > 0)
        {
            await this._typeTextHandler.Execute(new KeyboardTextCommand(text)).ConfigureAwait(false);

            var effectiveSleepAfter = sleepAfter.GetValueOrDefault(this._defaultKeyboardSleepAfterDuration);
            if (effectiveSleepAfter > TimeSpan.Zero)
            {
                await this.Sleep(effectiveSleepAfter).ConfigureAwait(false);
            }
        }
    }

    public async Task KeyPress(VirtualKeyCode[] keyCodes, TimeSpan? sleepAfter = default)
    {
        EnsureNotNullOrEmpty(keyCodes);
        await this._keyPressHandler.Execute(new KeyboardKeysCommand(keyCodes)).ConfigureAwait(false);

        var effectiveSleepAfter = sleepAfter.GetValueOrDefault(this._defaultKeyboardSleepAfterDuration);
        if (effectiveSleepAfter > TimeSpan.Zero)
        {
            await this.Sleep(effectiveSleepAfter).ConfigureAwait(false);
        }
    }

    public async Task KeyDown(VirtualKeyCode[] keyCodes, TimeSpan? sleepAfter = default)
    {
        EnsureNotNullOrEmpty(keyCodes);
        await this._keyDownHandler.Execute(new KeyboardKeysCommand(keyCodes)).ConfigureAwait(false);

        var effectiveSleepAfter = sleepAfter.GetValueOrDefault(this._defaultKeyboardSleepAfterDuration);
        if (effectiveSleepAfter > TimeSpan.Zero)
        {
            await this.Sleep(effectiveSleepAfter).ConfigureAwait(false);
        }
    }

    public async Task KeyUp(VirtualKeyCode[] keyCodes, TimeSpan? sleepAfter = default)
    {
        EnsureNotNullOrEmpty(keyCodes);
        await this._keyUpHandler.Execute(new KeyboardKeysCommand(keyCodes)).ConfigureAwait(false);

        var effectiveSleepAfter = sleepAfter.GetValueOrDefault(this._defaultKeyboardSleepAfterDuration);
        if (effectiveSleepAfter > TimeSpan.Zero)
        {
            await this.Sleep(effectiveSleepAfter).ConfigureAwait(false);
        }
    }

    private static void EnsureNotNullOrEmpty(params VirtualKeyCode[] keyCodes)
    {
        if (keyCodes == null)
        {
            throw new ArgumentNullException(nameof(keyCodes));
        }

        if (keyCodes.Length == 0)
        {
            throw new ArgumentException(Messages.MarionetteDriver_Throw_EmptyKeyCodes, nameof(keyCodes));
        }
    }

    public MarionetteDriver SetMouseSpeed(MouseSpeed speed)
    {
        this._mouseSpeed = speed;
        return this;
    }

    public MarionetteDriver SetCurrentMonitor(int monitorIndex)
    {
        if (monitorIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monitorIndex));
        }

        this._monitorIndex = monitorIndex;
        return this;
    }

    public MarionetteDriver SetCurrentMonitor(MonitorDescription monitor)
    {
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

    [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "It looks more coherent to only use instance methods here.")]
    public Task Sleep(int millisecondsDelay) => Task.Delay(millisecondsDelay);

    [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "It looks more coherent to only use instance methods here.")]
    public Task Sleep(TimeSpan delay) => Task.Delay(delay);

    public void Dispose()
    {
        foreach (var disposable in this._disposables)
        {
            disposable.Dispose();
        }
    }
}
