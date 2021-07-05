using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.UITesting
{
    internal sealed class MonitorService : IMonitorService, IDisposable
    {
        private readonly SemaphoreSlim _monitorsMutex = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _screenshotMutex = new SemaphoreSlim(1);

        private readonly TimeSpan _cacheDuration;
        private MonitorDescription[] _monitors;
        private Bitmap _cachedBitmap;
        private DateTime? _cacheDate;

        public MonitorService(TimeSpan cacheDuration)
        {
            this._cacheDuration = cacheDuration > TimeSpan.Zero ? cacheDuration : throw new ArgumentOutOfRangeException(nameof(cacheDuration), "Cache duration must be greater than zero.");
            this._monitors = null;
            this._cachedBitmap = null;
            this._cacheDate = null;
        }

        public async Task<MonitorDescription[]> GetMonitors()
        {
            if (this._monitors != null)
                return this._monitors;

            using (await SemaphoreWaiter.EnterAsync(this._monitorsMutex).ConfigureAwait(false))
            {
                if (this._monitors != null)
                    return this._monitors;

                this._monitors = await GraphicsScreenshot.GetMonitors().ConfigureAwait(false);
            }

            return this._monitors;
        }

        public async Task<MonitorDescription> GetMonitor(int index)
        {
            var monitors = await this.GetMonitors().ConfigureAwait(false);

            var monitor = monitors.FirstOrDefault(x => x.Index == index);
            if (monitor == null)
                throw new InvalidOperationException($"Requested monitor index {index} not found.");

            return monitor;
        }

        public async Task<Bitmap> GetScreenshot(MonitorDescription monitor)
        {
            if (this.TryGetNonExpiredCachedBitmapClone(out var cachedBitmap))
                return cachedBitmap;

            using (await SemaphoreWaiter.EnterAsync(this._screenshotMutex).ConfigureAwait(false))
            {
                if (this.TryGetNonExpiredCachedBitmapClone(out cachedBitmap))
                    return cachedBitmap;

                this._cachedBitmap = await GraphicsScreenshot.Take(monitor).ConfigureAwait(false);
                this._cacheDate = DateTime.UtcNow;

                return new Bitmap(this._cachedBitmap);
            }
        }

        private bool TryGetNonExpiredCachedBitmapClone(out Bitmap bitmap)
        {
            bitmap = default;

            if (!this._cacheDate.HasValue)
                return false;

            var cacheAge = DateTime.UtcNow - this._cacheDate.Value;
            if (cacheAge > this._cacheDuration)
                return false;

            bitmap = new Bitmap(this._cachedBitmap);
            return true;
        }

        public void Dispose()
        {
            if (this._cachedBitmap != null)
            {
                this._cachedBitmap.Dispose();
                this._cachedBitmap = null;
            }
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