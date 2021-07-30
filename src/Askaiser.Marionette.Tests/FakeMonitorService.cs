using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Tests
{
    public sealed class FakeMonitorService : IMonitorService
    {
        private static readonly MonitorDescription[] Monitors =
        {
            new MonitorDescription(0, 0, 0, 1920, 1080),
        };

        private readonly byte[] _screenshotBytes;

        public FakeMonitorService(Image screenshot)
        {
            this._screenshotBytes = BitmapUtils.ToBytes(screenshot);
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
            return Task.FromResult(BitmapUtils.FromBytes(this._screenshotBytes));
        }
    }
}
