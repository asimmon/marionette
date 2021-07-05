using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Askaiser.UITesting
{
    internal static class ElementAwaiter
    {
        private static readonly TimeSpan ThrottlingInterval = TimeSpan.FromMilliseconds(50);

        public static async Task<SearchResult> WaitFor(IMonitorService monitorService, IElementRecognizer elementRecognizer, IElement element, TimeSpan duration, int monitorIndex)
        {
            if (duration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater or equal to zero");

            var monitor = await monitorService.GetMonitor(monitorIndex);

            var isFirstLoop = true;
            for (var sw = Stopwatch.StartNew(); sw.Elapsed < duration || isFirstLoop;)
            {
                using var screenshot = await monitorService.GetScreenshot(monitor).ConfigureAwait(false);

                var result = await elementRecognizer.Recognize(screenshot, element).ConfigureAwait(false);
                if (result.Success)
                    return result.AdjustToMonitor(monitor);

                await Task.Delay(ThrottlingInterval).ConfigureAwait(false);
                isFirstLoop = false;
            }

            throw new WaitForTimeoutException(element, duration);
        }
    }
}