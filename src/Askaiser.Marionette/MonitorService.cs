using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette
{
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",  Justification = "We do not use SemaphoreSlim's AvailableWaitHandle.")]
    internal sealed class MonitorService : IMonitorService
    {
        private readonly SemaphoreSlim _monitorsMutex = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _screenshotMutex = new SemaphoreSlim(1);

        private readonly TimeSpan _cacheDuration;
        private MonitorDescription[] _monitors;
        private byte[] _cachedBitmapBytes;
        private DateTime? _cacheDate;
        private int _cacheMonitorIndex;

        public MonitorService(TimeSpan cacheDuration)
        {
            this._cacheDuration = cacheDuration;
            this._monitors = null;
            this._cachedBitmapBytes = null;
            this._cacheDate = null;
            this._cacheMonitorIndex = 0;
        }

        public async Task<MonitorDescription[]> GetMonitors()
        {
            if (this._monitors != null)
            {
                return this._monitors;
            }

            using (await SemaphoreWaiter.EnterAsync(this._monitorsMutex).ConfigureAwait(false))
            {
                if (this._monitors != null)
                {
                    return this._monitors;
                }

                this._monitors = await DisplayScreen.GetMonitors().ConfigureAwait(false);
            }

            return this._monitors;
        }

        public async Task<MonitorDescription> GetMonitor(int index)
        {
            var monitors = await this.GetMonitors().ConfigureAwait(false);

            var monitor = monitors.FirstOrDefault(x => x.Index == index);
            if (monitor == null)
            {
                throw new InvalidOperationException(Messages.MonitorService_Throw_MonitorNotFound.FormatInvariant(index));
            }

            return monitor;
        }

        public async Task<Bitmap> GetScreenshot(MonitorDescription monitor)
        {
            if (this.TryGetNonExpiredCachedBitmapClone(monitor.Index, out var cachedScreenshot))
            {
                return cachedScreenshot;
            }

            using (await SemaphoreWaiter.EnterAsync(this._screenshotMutex).ConfigureAwait(false))
            {
                if (this.TryGetNonExpiredCachedBitmapClone(monitor.Index, out cachedScreenshot))
                {
                    return cachedScreenshot;
                }

                var screenshot = await ScreenshotService.Take(monitor).ConfigureAwait(false);

                var screenshotStream = new MemoryStream();

#if NETSTANDARD2_0
                using (screenshotStream)
#else
                await using (screenshotStream.ConfigureAwait(false))
#endif
                {
                    screenshot.Save(screenshotStream, ImageFormat.Bmp);
                    this._cachedBitmapBytes = screenshotStream.ToArray();
                    this._cacheDate = DateTime.UtcNow;
                    this._cacheMonitorIndex = monitor.Index;
                    return screenshot;
                }
            }
        }

        private bool TryGetNonExpiredCachedBitmapClone(int monitorIndex, out Bitmap bitmap)
        {
            bitmap = default;

            var isCacheDisabled = this._cacheDuration == TimeSpan.Zero;
            if (isCacheDisabled)
            {
                return false;
            }

            var isCacheEmpty = !this._cacheDate.HasValue;
            if (isCacheEmpty)
            {
                return false;
            }

            var isAnotherMonitorRequested = this._cacheMonitorIndex != monitorIndex;
            if (isAnotherMonitorRequested)
            {
                return false;
            }

            var cacheAge = DateTime.UtcNow - this._cacheDate.Value;
            if (cacheAge > this._cacheDuration)
            {
                return false;
            }

            using var bitmapStream = new MemoryStream(this._cachedBitmapBytes);
            bitmap = new Bitmap(bitmapStream);
            return true;
        }

        private sealed class SemaphoreWaiter : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            private SemaphoreWaiter(SemaphoreSlim semaphore)
            {
                this._semaphore = semaphore;
            }

            public static async ValueTask<SemaphoreWaiter> EnterAsync(SemaphoreSlim semaphore)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                return new SemaphoreWaiter(semaphore);
            }

            public void Dispose()
            {
                this._semaphore.Release();
            }
        }
    }
}
