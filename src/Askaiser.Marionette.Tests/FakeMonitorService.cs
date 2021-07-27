using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Tests
{
    public sealed class FakeMonitorService : IMonitorService, IDisposable
    {
        private static readonly MonitorDescription[] Monitors =
        {
            new MonitorDescription(0, 0, 0, 1920, 1080),
        };

        private readonly Bitmap _screenshot;

        public FakeMonitorService(Bitmap screenshot)
        {
            this._screenshot = screenshot;
        }

        public Task<MonitorDescription[]> GetMonitors()
        {
            return Task.FromResult(Monitors);
        }

        public Task<MonitorDescription> GetMonitor(int index)
        {
            return Task.FromResult(Monitors[index]);
        }

        public Task<Bitmap> GetScreenshot(MonitorDescription monitor)
        {
            return Task.FromResult(new Bitmap(this._screenshot));
        }

        public void Dispose()
        {
            this._screenshot.Dispose();
        }
    }
}
