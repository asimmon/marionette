using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Tests
{
    public sealed class FakeMonitorService : IMonitorService
    {
        public const int Screen0 = 0;
        public const int Screen1 = 1;
        public const int Screen2 = 2;
        public const int Screen3 = 3;
        public const int Screen4 = 4;

        public static readonly MonitorDescription[] Monitors =
        {
            new MonitorDescription(Screen0, 0, 0, 1920, 1080),
            new MonitorDescription(Screen1, 1920, 0, 3840, 1080),
            new MonitorDescription(Screen2, -1920, 0, 0, 1080),
            new MonitorDescription(Screen3, 0, -1080, 1920, 0),
            new MonitorDescription(Screen4, 0, 1080, 1920, 2160),
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
