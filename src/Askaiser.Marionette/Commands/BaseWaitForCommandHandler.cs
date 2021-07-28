using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Commands
{
    internal abstract class BaseWaitForCommandHandler
    {
        private static readonly TimeSpan ThrottlingInterval = TimeSpan.FromMilliseconds(50);

        private readonly DriverOptions _options;
        private readonly IFileWriter _fileWriter;
        private readonly IMonitorService _monitorService;
        private readonly IElementRecognizer _elementRecognizer;

        protected BaseWaitForCommandHandler(DriverOptions options, IFileWriter fileWriter, IMonitorService monitorService, IElementRecognizer elementRecognizer)
        {
            this._options = options;
            this._fileWriter = fileWriter;
            this._monitorService = monitorService;
            this._elementRecognizer = elementRecognizer;
        }

        protected async Task<RecognizerSearchResult> WaitFor(IElement element, WaitForCommand command, CancellationToken token)
        {
            if (command.WaitFor < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(command.WaitFor), $"{nameof(command.WaitFor)} duration must be greater or equal to zero");
            }

            var monitor = await this._monitorService.GetMonitor(command.MonitorIndex).ConfigureAwait(false);
            var searchRect = AdjustSearchRectangleRelativeToMonitorSize(monitor, command.SearchRectangle);
            var watch = Stopwatch.StartNew();

            RecognizerSearchResult recognizerResult = null;
            do
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                using var screenshot = await this.GetScreenshot(monitor, searchRect).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                {
                    break;
                }

                recognizerResult?.Dispose();
                recognizerResult = await this._elementRecognizer.Recognize(screenshot, element, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (recognizerResult.Success)
                {
                    var adjustedResult = recognizerResult.AdjustToMonitor(monitor).AdjustToSearchRectangle(searchRect);
                    return new RecognizerSearchResult(recognizerResult.TransformedScreenshot, adjustedResult);
                }

                try
                {
                    await Task.Delay(ThrottlingInterval, token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            while (watch.Elapsed < command.WaitFor);

            return RecognizerSearchResult.NotFound(recognizerResult?.TransformedScreenshot, element);
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

        protected async Task<SearchResult> TrimRecognizerResultAndThrowIfRequired(WaitForCommand command, RecognizerSearchResult disposableResult)
        {
            try
            {
                if (command.Behavior == NoSingleResultBehavior.Throw)
                {
                    disposableResult.EnsureSingleLocation(command.WaitFor);
                }

                return new SearchResult(disposableResult);
            }
            catch (MarionetteException)
            {
                if (disposableResult != null)
                {
                    if (command.Behavior == NoSingleResultBehavior.Throw && disposableResult.TransformedScreenshot != null)
                    {
                        await this.SaveScreenshot(disposableResult.Element, disposableResult.TransformedScreenshot).ConfigureAwait(false);
                    }

                    disposableResult.Dispose();
                }

                throw;
            }
        }

        private static readonly Regex NotAlphanumericRegex = new Regex("[^a-z0-9\\-]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WhitespaceRegex = new Regex("\\s+", RegexOptions.Compiled);

        private async Task SaveScreenshot(IElement element, Image screenshot)
        {
            if (this._options.FailureScreenshotPath == null)
            {
                return;
            }

            var fileName = MakeFailureScreenshotFileName(element);

            var screenshotBytes = screenshot.GetBytes(ImageFormat.Png);
            await this._fileWriter.SaveScreenshot(Path.Combine(this._options.FailureScreenshotPath, fileName.ToLowerInvariant()), screenshotBytes).ConfigureAwait(false);
        }

        private static string MakeFailureScreenshotFileName(IElement element)
        {
            var elementDescriptor = NotAlphanumericRegex.Replace(WhitespaceRegex.Replace(element.ToString()?.Trim() ?? string.Empty, "-"), string.Empty);
            return string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd-HH-mm-ss-ffff}_{1}.png", DateTime.UtcNow, elementDescriptor.ToLowerInvariant());
        }
    }
}
