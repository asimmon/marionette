using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Tests
{
    public abstract class BaseWaitForCommandHandlerTests
    {
        internal class FakeMonitorService : IMonitorService, IDisposable
        {
            private static readonly MonitorDescription Single1080pMonitor = new MonitorDescription(0, 0, 0, 1920, 1080);

            private readonly Bitmap _screenshot;

            public FakeMonitorService(Bitmap screenshot)
            {
                this._screenshot = screenshot;
            }

            public Task<MonitorDescription[]> GetMonitors()
            {
                return Task.FromResult(new[] { Single1080pMonitor });
            }

            public Task<MonitorDescription> GetMonitor(int index)
            {
                return Task.FromResult(Single1080pMonitor);
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

        internal class FakeElement : IElement
        {
            public FakeElement(string name)
            {
                this.Name = name;
            }

            public string Name { get; }

            public override string ToString()
            {
                return this.Name;
            }
        }
    }
}
