using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Commands
{
    internal abstract class BaseWaitForCommandHandler
    {
        private static readonly TimeSpan ThrottlingInterval = TimeSpan.FromMilliseconds(50);

        private readonly DriverOptions _options;
        private readonly IMonitorService _monitorService;
        private readonly IElementRecognizer _elementRecognizer;

        protected BaseWaitForCommandHandler(DriverOptions options, IMonitorService monitorService, IElementRecognizer elementRecognizer)
        {
            this._options = options;
            this._monitorService = monitorService;
            this._elementRecognizer = elementRecognizer;
        }

        protected async Task<SearchResult> WaitFor(IElement element, WaitForCommand command)
        {
            if (command.WaitFor < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(command.WaitFor), "WaitFor duration must be greater or equal to zero");
            }

            var monitor = await this._monitorService.GetMonitor(command.MonitorIndex).ConfigureAwait(false);
            var searchRect = AdjustSearchRectangleRelativeToMonitorSize(monitor, command.SearchRectangle);

            RecognizerSearchResult disposableResult = null;
            try
            {
                var isFirstLoop = true;
                for (var sw = Stopwatch.StartNew(); sw.Elapsed < command.WaitFor || isFirstLoop;)
                {
                    using var screenshot = await this.GetScreenshot(monitor, searchRect).ConfigureAwait(false);

                    disposableResult = await this._elementRecognizer.Recognize(screenshot, element).ConfigureAwait(false);
                    if (disposableResult.Success)
                    {
                        var adjustedResult = disposableResult.AdjustToMonitor(monitor).AdjustToSearchRectangle(searchRect);

                        if (command.Behavior == NoSingleResultBehavior.Throw)
                        {
                            adjustedResult.EnsureSingleLocation(command.WaitFor);
                        }

                        return adjustedResult;
                    }

                    await Task.Delay(ThrottlingInterval).ConfigureAwait(false);
                    isFirstLoop = false;
                }

                if (command.Behavior == NoSingleResultBehavior.Throw && this._options.FailureScreenshotPath != null)
                {
                    await this.SaveScreenshot(element, disposableResult.TransformedScreenshot).ConfigureAwait(false);
                }
            }
            finally
            {
                disposableResult?.Dispose();
            }

            if (command.Behavior == NoSingleResultBehavior.Throw)
            {
                throw new ElementNotFoundException(element, command.WaitFor);
            }

            return SearchResult.NotFound(element);
        }

        private static Rectangle AdjustSearchRectangleRelativeToMonitorSize(MonitorDescription monitor, Rectangle searchRect)
        {
            if (searchRect == null)
            {
                return null;
            }

            var offsetLeft = searchRect.Left - monitor.Left;
            var offsetTop = searchRect.Top - monitor.Top;

            return new Rectangle(offsetLeft, offsetTop, offsetLeft + searchRect.Width, offsetTop + searchRect.Height);
        }

        private async Task<Bitmap> GetScreenshot(MonitorDescription monitor, Rectangle searchRect)
        {
            var screenshot = await this._monitorService.GetScreenshot(monitor).ConfigureAwait(false);
            if (searchRect == null)
            {
                return screenshot;
            }

            using (screenshot)
            {
                return screenshot.Crop(searchRect);
            }
        }

        private static readonly Regex NotAlphanumericRegex = new Regex("[^a-z0-9\\-]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WhitespaceRegex = new Regex("\\s+", RegexOptions.Compiled);

        private async Task SaveScreenshot(IElement element, Image screenshot)
        {
            var elementDescriptor = NotAlphanumericRegex.Replace(WhitespaceRegex.Replace(element.ToString()?.Trim() ?? string.Empty, "-"), string.Empty);
            var fileName = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd-HH-mm-ss-ffff}_{1}.png", DateTime.UtcNow, elementDescriptor);

            var screenshotBytes = screenshot.GetBytes(ImageFormat.Png);

#if NETSTANDARD2_0
            using (var fileStream = File.Open(Path.Combine(this._options.FailureScreenshotPath, fileName.ToLowerInvariant()), FileMode.Create))
            {
                await fileStream.WriteAsync(screenshotBytes, 0, screenshotBytes.Length).ConfigureAwait(false);
            }
#else
            await using (var fileStream = File.Open(Path.Combine(this._options.FailureScreenshotPath, fileName.ToLowerInvariant()), FileMode.Create))
            {
                await fileStream.WriteAsync(screenshotBytes).ConfigureAwait(false);
            }
#endif
        }
    }
}
