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

        public MonitorService(TimeSpan cacheDuration)
        {
            this._cacheDuration = cacheDuration > TimeSpan.Zero ? cacheDuration : throw new ArgumentOutOfRangeException(nameof(cacheDuration), "Cache duration must be greater than zero.");
            this._monitors = null;
            this._cachedBitmapBytes = null;
            this._cacheDate = null;
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

                this._monitors = await GraphicsScreenshot.GetMonitors().ConfigureAwait(false);
            }

            return this._monitors;
        }

        public async Task<MonitorDescription> GetMonitor(int index)
        {
            var monitors = await this.GetMonitors().ConfigureAwait(false);

            var monitor = monitors.FirstOrDefault(x => x.Index == index);
            if (monitor == null)
            {
                throw new InvalidOperationException($"Requested monitor index {index} not found.");
            }

            return monitor;
        }

        public async Task<Bitmap> GetScreenshot(MonitorDescription monitor)
        {
            if (this.TryGetNonExpiredCachedBitmapClone(out var cachedScreenshot))
            {
                return cachedScreenshot;
            }

            using (await SemaphoreWaiter.EnterAsync(this._screenshotMutex).ConfigureAwait(false))
            {
                if (this.TryGetNonExpiredCachedBitmapClone(out cachedScreenshot))
                {
                    return cachedScreenshot;
                }

                var screenshot = await GraphicsScreenshot.Take(monitor).ConfigureAwait(false);

                var screenshotStream = new MemoryStream();
                await using (screenshotStream.ConfigureAwait(false))
                {
                    screenshot.Save(screenshotStream, ImageFormat.Png);
                    this._cachedBitmapBytes = screenshotStream.ToArray();
                    this._cacheDate = DateTime.UtcNow;
                    return screenshot;
                }
            }
        }

        private bool TryGetNonExpiredCachedBitmapClone(out Bitmap bitmap)
        {
            bitmap = default;

            if (!this._cacheDate.HasValue)
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
