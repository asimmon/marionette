using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using SharpDX.DXGI;

namespace Askaiser.UITesting
{
    internal static class GraphicsScreenshot
    {
        public static Task<MonitorDescription[]> GetMonitors()
        {
            return Task.Run(() => GetMonitorsInternal().ToArray());
        }

        private static IEnumerable<MonitorDescription> GetMonitorsInternal()
        {
            var monitorCount = 0;

            using var factory = new Factory1();

            var adapterCount = factory.GetAdapterCount();
            for (var adapterIndex = 0; adapterIndex < adapterCount; adapterIndex++)
            {
                using var adapter = factory.GetAdapter(adapterIndex);

                var outputCount = adapter.GetOutputCount();
                if (outputCount == 0)
                    continue;

                using var device = new SharpDX.Direct3D11.Device(adapter);

                for (var outputIndex = 0; outputIndex < outputCount; outputIndex++)
                {
                    using var output = adapter.GetOutput(outputIndex);
                    var bounds = output.Description.DesktopBounds;
                    yield return new MonitorDescription(monitorCount++, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
                }
            }
        }

        public static Task<Bitmap> Take(Rectangle monitor)
        {
            return Task.Run(() => TakeInternal(monitor));
        }

        private static Bitmap TakeInternal(Rectangle monitor)
        {
            var bitmap = new Bitmap(monitor.Width, monitor.Heigh);
            using (var graphics = Graphics.FromImage(bitmap))
                graphics.CopyFromScreen(monitor.Left, monitor.Top, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);

            return bitmap;
        }
    }
}