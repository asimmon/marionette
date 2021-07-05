using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Askaiser.UITesting.Commands;

namespace Askaiser.UITesting
{
    public class TestContext
    {
        private readonly IMonitorService _monitorService;
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

        internal TestContext(IMonitorService monitorService, IElementRecognizer elementRecognizer, IMouseController mouseController, IKeyboardController keyboardController)
        {
            this._monitorService = monitorService;

            this._waitForHandler = new WaitForCommandHandler(monitorService, elementRecognizer);
            this._waitForAnyHandler = new WaitForAnyCommandHandler(monitorService, elementRecognizer);
            this._waitForAllHandler = new WaitForAllCommandHandler(monitorService, elementRecognizer);
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
            this._mouseSpeed = MouseSpeed.Immediate;
        }

        public static TestContext Create()
        {
            var monitorService = new MonitorService(TimeSpan.FromMilliseconds(200));
            var elementRecognizer = new AggregateElementRecognizer(new ImageElementRecognizer(), new TextElementRecognizer());
            var mouseController = new MouseController();
            var keyboardController = new KeyboardController();

            return new TestContext(monitorService, elementRecognizer, mouseController, keyboardController);
        }

        public async Task<MonitorDescription[]> GetMonitors()
        {
            return await this._monitorService.GetMonitors().ConfigureAwait(false);
        }

        public async Task<bool> IsVisible(IElement element, TimeSpan waitFor = default, Rectangle searchRect = default)
        {
            try
            {
                await this.WaitFor(element, waitFor, searchRect).ConfigureAwait(false);
                return true;
            }
            catch (WaitForTimeoutException)
            {
                return false;
            }
        }

        public async Task<SearchResult> WaitFor(IElement element, TimeSpan duration = default, Rectangle searchRect = default)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return await this._waitForHandler.Execute(new WaitForCommand(new[] { element }, duration, searchRect, this._monitorIndex)).ConfigureAwait(false);
        }

        public async Task<SearchResult> WaitForAny(IEnumerable<IElement> elements, TimeSpan duration = default, Rectangle searchRect = default)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var enumeratedElements = new List<IElement>(elements);

            if (enumeratedElements.Count == 0) throw new ArgumentException("Elements cannot be empty", nameof(elements));
            return await this._waitForAnyHandler.Execute(new WaitForCommand(enumeratedElements, duration, searchRect, this._monitorIndex)).ConfigureAwait(false);
        }

        public async Task<SearchResultCollection> WaitForAll(IEnumerable<IElement> elements, TimeSpan duration = default, Rectangle searchRect = default)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var enumeratedElements = new List<IElement>(elements);

            if (enumeratedElements.Count == 0) throw new ArgumentException("Elements cannot be empty", nameof(elements));
            return await this._waitForAllHandler.Execute(new WaitForCommand(enumeratedElements, duration, searchRect, this._monitorIndex)).ConfigureAwait(false);
        }

        public async Task MoveTo(IElement element, TimeSpan waitFor = default, Rectangle searchRect = default)
        {
            var result = await this.WaitFor(element, waitFor, searchRect).ConfigureAwait(false);
            var (x, y) = result.Area.Center;
            await this.MoveTo(x, y).ConfigureAwait(false);
        }

        public async Task MoveTo(int x, int y)
        {
            await this._moveToLocationHandler.Execute(new MouseLocationCommand(x, y, this._mouseSpeed)).ConfigureAwait(false);
        }

        public async Task SingleClick(IElement element, TimeSpan waitFor = default, Rectangle searchRect = default)
        {
            await this.ElementMouseAction(element, waitFor, searchRect, this.SingleClick).ConfigureAwait(false);
        }

        public async Task DoubleClick(IElement element, TimeSpan waitFor = default, Rectangle searchRect = default)
        {
            await this.ElementMouseAction(element, waitFor, searchRect, this.DoubleClick).ConfigureAwait(false);
        }

        public async Task TripleClick(IElement element, TimeSpan waitFor = default, Rectangle searchRect = default)
        {
            await this.ElementMouseAction(element, waitFor, searchRect, this.TripleClick).ConfigureAwait(false);
        }

        public async Task RightClick(IElement element, TimeSpan waitFor = default, Rectangle searchRect = default)
        {
            await this.ElementMouseAction(element, waitFor, searchRect, this.RightClick).ConfigureAwait(false);
        }

        public async Task Drag(IElement element, TimeSpan waitFor = default, Rectangle searchRect = default)
        {
            await this.ElementMouseAction(element, waitFor, searchRect, this.Drag).ConfigureAwait(false);
        }

        public async Task Drop(IElement element, TimeSpan waitFor = default, Rectangle searchRect = default)
        {
            await this.ElementMouseAction(element, waitFor, searchRect, this.Drop).ConfigureAwait(false);
        }

        public async Task SingleClick(SearchResult searchResult) => await SearchResultMouseAction(searchResult, this.SingleClick).ConfigureAwait(false);
        public async Task DoubleClick(SearchResult searchResult) => await SearchResultMouseAction(searchResult, this.DoubleClick).ConfigureAwait(false);
        public async Task TripleClick(SearchResult searchResult) => await SearchResultMouseAction(searchResult, this.TripleClick).ConfigureAwait(false);
        public async Task RightClick(SearchResult searchResult) => await SearchResultMouseAction(searchResult, this.RightClick).ConfigureAwait(false);
        public async Task Drag(SearchResult searchResult) => await SearchResultMouseAction(searchResult, this.Drag).ConfigureAwait(false);
        public async Task Drop(SearchResult searchResult) => await SearchResultMouseAction(searchResult, this.Drop).ConfigureAwait(false);
        public async Task MoveTo(SearchResult searchResult) => await SearchResultMouseAction(searchResult, this.MoveTo).ConfigureAwait(false);

        private async Task ElementMouseAction(IElement element, TimeSpan waitFor, Rectangle searchRect, Func<int, int, Task> clickFunc)
        {
            var result = await this.WaitFor(element, waitFor, searchRect).ConfigureAwait(false);
            await SearchResultMouseAction(result, clickFunc).ConfigureAwait(false);
        }

        private static async Task SearchResultMouseAction(SearchResult sr, Func<int, int, Task> clickFunc)
        {
            var (x, y) = sr.Area.Center;
            await clickFunc(x, y).ConfigureAwait(false);
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

        public async Task Drag(int x, int y)
        {
            await this._dragLocationHandler.Execute(new MouseLocationCommand(x, y, this._mouseSpeed)).ConfigureAwait(false);
        }

        public async Task Drop(int x, int y)
        {
            await this._dropLocationHandler.Execute(new MouseLocationCommand(x, y, this._mouseSpeed)).ConfigureAwait(false);
        }

        public async Task ScrollUp(int scrollTicks = 1)
        {
            if (scrollTicks <= 0) throw new ArgumentOutOfRangeException(nameof(scrollTicks), "Scroll ticks must be a positive integer.");
            for (var i = 0; i < scrollTicks; i++)
                await this._mouseWheelHandler.Execute(new MouseWheelCommand(IsUp: true)).ConfigureAwait(false);
        }

        public async Task ScrollDown(int scrollTicks = 1)
        {
            if (scrollTicks <= 0) throw new ArgumentOutOfRangeException(nameof(scrollTicks), "Scroll ticks must be a positive integer.");
            for (var i = 0; i < scrollTicks; i++)
                await this._mouseWheelHandler.Execute(new MouseWheelCommand(IsUp: false)).ConfigureAwait(false);
        }

        public Task ScrollUpUntilVisible(IElement element, TimeSpan totalDuration, int scrollTicks = 1, Rectangle searchRect = default)
        {
            return this.ScrollUntilVisible(element, totalDuration, isUp: true, scrollTicks, searchRect);
        }

        public Task ScrollDownUntilVisible(IElement element, TimeSpan totalDuration, int scrollTicks = 1, Rectangle searchRect = default)
        {
            return this.ScrollUntilVisible(element, totalDuration, isUp: false, scrollTicks, searchRect);
        }

        private async Task ScrollUntilVisible(IElement element, TimeSpan totalDuration, bool isUp, int scrollTicks, Rectangle searchRect)
        {
            if (totalDuration <= TimeSpan.Zero)
                throw new ArgumentException("Total duration must be greater than zero.", nameof(totalDuration));

            for (var sw = Stopwatch.StartNew(); sw.Elapsed < totalDuration;)
            {
                if (await this.IsVisible(element, TimeSpan.Zero, searchRect).ConfigureAwait(false))
                    return;

                var scrollTask = isUp ? this.ScrollUp(scrollTicks) : this.ScrollDown(scrollTicks);
                await scrollTask.ConfigureAwait(false);
            }

            throw new WaitForTimeoutException(element, totalDuration);
        }

        public async Task TypeText(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length > 0)
                await this._typeTextHandler.Execute(new KeyboardTextCommand(text)).ConfigureAwait(false);
        }

        public async Task KeyPress(params VirtualKeyCode[] keyCodes)
        {
            EnsureNotNullOrEmpty(keyCodes);
            await this._keyPressHandler.Execute(new KeyboardKeysCommand(keyCodes)).ConfigureAwait(false);
        }

        public async Task KeyDown(params VirtualKeyCode[] keyCodes)
        {
            EnsureNotNullOrEmpty(keyCodes);
            await this._keyDownHandler.Execute(new KeyboardKeysCommand(keyCodes)).ConfigureAwait(false);
        }

        public async Task KeyUp(params VirtualKeyCode[] keyCodes)
        {
            EnsureNotNullOrEmpty(keyCodes);
            await this._keyUpHandler.Execute(new KeyboardKeysCommand(keyCodes)).ConfigureAwait(false);
        }

        private static void EnsureNotNullOrEmpty(params VirtualKeyCode[] keyCodes)
        {
            if (keyCodes == null) throw new ArgumentNullException(nameof(keyCodes));
            if (keyCodes.Length == 0) throw new ArgumentException("Key codes cannot be empty", nameof(keyCodes));
        }

        public TestContext SetMouseSpeed(MouseSpeed speed)
        {
            this._mouseSpeed = speed;
            return this;
        }

        public TestContext SetMonitor(int monitorIndex)
        {
            this._monitorIndex = monitorIndex;
            return this;
        }

        public Task Sleep(int millisecondsDelay) => Task.Delay(millisecondsDelay);
        public Task Sleep(TimeSpan delay) => Task.Delay(delay);
    }
}