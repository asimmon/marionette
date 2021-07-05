using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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

        protected async Task<SearchResult> WaitFor(IElement element, TimeSpan duration, Rectangle searchRect, int monitorIndex)
        {
            if (duration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater or equal to zero");

            var monitor = await this._monitorService.GetMonitor(monitorIndex);

            var isFirstLoop = true;
            for (var sw = Stopwatch.StartNew(); sw.Elapsed < duration || isFirstLoop;)
            {
                using var screenshot = await this.GetScreenshot(monitor, searchRect).ConfigureAwait(false);

                var result = await this._elementRecognizer.Recognize(screenshot, element).ConfigureAwait(false);
                if (result.Success)
                    return result.AdjustToMonitor(monitor);

                await Task.Delay(ThrottlingInterval).ConfigureAwait(false);
                isFirstLoop = false;
            }

            throw new WaitForTimeoutException(element, duration);
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