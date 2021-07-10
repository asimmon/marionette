using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
{
    internal abstract class BaseWaitForCommandHandler
    {
        private static readonly TimeSpan ThrottlingInterval = TimeSpan.FromMilliseconds(50);

        private readonly IMonitorService _monitorService;
        private readonly IElementRecognizer _elementRecognizer;

        protected BaseWaitForCommandHandler(IMonitorService monitorService, IElementRecognizer elementRecognizer)
        {
            this._monitorService = monitorService;
            this._elementRecognizer = elementRecognizer;
        }

        protected async Task<SearchResult> WaitFor(IElement element, TimeSpan waitFor, Rectangle searchRect, int monitorIndex)
        {
            if (waitFor < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(waitFor), "WaitFor duration must be greater or equal to zero");

            var monitor = await this._monitorService.GetMonitor(monitorIndex).ConfigureAwait(false);

            var isFirstLoop = true;
            for (var sw = Stopwatch.StartNew(); sw.Elapsed < waitFor || isFirstLoop;)
            {
                using var screenshot = await this.GetScreenshot(monitor, searchRect).ConfigureAwait(false);

                var result = await this._elementRecognizer.Recognize(screenshot, element).ConfigureAwait(false);
                if (result.Success)
                    return result.AdjustToMonitor(monitor).AdjustToSearchRectangle(searchRect);

                await Task.Delay(ThrottlingInterval).ConfigureAwait(false);
                isFirstLoop = false;
            }

            throw new WaitForTimeoutException(element, waitFor);
        }

        private async Task<Bitmap> GetScreenshot(MonitorDescription monitor, Rectangle searchRect)
        {
            var screenshot = await this._monitorService.GetScreenshot(monitor).ConfigureAwait(false);
            if (searchRect == null)
                return screenshot;

            using (screenshot)
                return screenshot.Crop(searchRect);
        }
    }
}
