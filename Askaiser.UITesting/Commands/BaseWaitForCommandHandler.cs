using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
{
    internal abstract class BaseWaitForCommandHandler
    {
        private static readonly TimeSpan ThrottlingInterval = TimeSpan.FromMilliseconds(50);

        private readonly TestContextOptions _options;
        private readonly IMonitorService _monitorService;
        private readonly IElementRecognizer _elementRecognizer;

        protected BaseWaitForCommandHandler(TestContextOptions options, IMonitorService monitorService, IElementRecognizer elementRecognizer)
        {
            this._options = options;
            this._monitorService = monitorService;
            this._elementRecognizer = elementRecognizer;
        }

        protected async Task<SearchResult> WaitFor(IElement element, WaitForCommand command)
        {
            if (command.WaitFor < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(command.WaitFor), "WaitFor duration must be greater or equal to zero");

            var monitor = await this._monitorService.GetMonitor(command.MonitorIndex).ConfigureAwait(false);

            Bitmap screenshot = null;
            try
            {
                var isFirstLoop = true;
                for (var sw = Stopwatch.StartNew(); sw.Elapsed < command.WaitFor || isFirstLoop;)
                {
                    screenshot?.Dispose();
                    screenshot = await this.GetScreenshot(monitor, command.SearchRectangle).ConfigureAwait(false);

                    var result = await this._elementRecognizer.Recognize(screenshot, element).ConfigureAwait(false);
                    if (result.Success)
                        return result.AdjustToMonitor(monitor).AdjustToSearchRectangle(command.SearchRectangle);

                    await Task.Delay(ThrottlingInterval).ConfigureAwait(false);
                    isFirstLoop = false;
                }

                if (command.TimeoutHandling == TimeoutHandling.Throw && this._options.FailureScreenshotPath != null)
                {
                    await this.SaveScreenshot(element, screenshot).ConfigureAwait(false);
                }
            }
            finally
            {
                screenshot?.Dispose();
            }

            if (command.TimeoutHandling == TimeoutHandling.Throw)
                throw new ElementTimeoutException(element, command.WaitFor);

            return SearchResult.NotFound(element);
        }

        private static readonly Regex NotAlphanumericRegex = new Regex("[^a-z0-9\\-]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WhitespaceRegex = new Regex("\\s+", RegexOptions.Compiled);

        private async Task SaveScreenshot(IElement element, Image screenshot)
        {
            var elementDescriptor = NotAlphanumericRegex.Replace(WhitespaceRegex.Replace(element.ToString()?.Trim() ?? "", "-"), "");
            var fileName = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd-HH-mm-ss-ffff}_{1}.png", DateTime.UtcNow, elementDescriptor);

            var screenshotBytes = screenshot.GetBytes(ImageFormat.Png);
            await File.WriteAllBytesAsync(Path.Combine(this._options.FailureScreenshotPath, fileName), screenshotBytes).ConfigureAwait(false);
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
