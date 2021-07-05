using System;

namespace Askaiser.UITesting
{
    internal sealed class DisposableTestContext : TestContext
    {
        private MonitorService _monitorService;

        private DisposableTestContext(MonitorService monitorService, IElementRecognizer elementRecognizer, IMouseController mouseController, IKeyboardController keyboardController)
            : base(monitorService, elementRecognizer, mouseController, keyboardController)
        {
            this._monitorService = monitorService;
        }

        public static TestContext CreateInternal()
        {
            var monitorService = new MonitorService(TimeSpan.FromMilliseconds(200));
            var elementRecognizer = new AggregateElementRecognizer(new ImageElementRecognizer(), new TextElementRecognizer());
            var mouseController = new MouseController();
            var keyboardController = new KeyboardController();

            return new DisposableTestContext(monitorService, elementRecognizer, mouseController, keyboardController);
        }

        protected override void Dispose(bool disposing)
        {
            if (this._monitorService != null)
            {
                this._monitorService.Dispose();
                this._monitorService = null;
            }
        }
    }
}