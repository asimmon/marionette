using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Askaiser.Marionette
{
    internal static class GraphicsScreenshot
    {
        [DllImport("user32.dll", EntryPoint = "EnumDisplayMonitors")]
        private static extern bool EnumDisplayMonitors(
            [In] IntPtr dcHandle,
            [In] IntPtr clip,
            MonitorEnumProcedure callback,
            IntPtr callbackObject);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int MonitorEnumProcedure(
            IntPtr monitorHandle,
            IntPtr dcHandle,
            ref RectangleL rect,
            IntPtr callbackObject);

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct RectangleL
        {
            [MarshalAs(UnmanagedType.U4)]
            public readonly int Left;

            [MarshalAs(UnmanagedType.U4)]
            public readonly int Top;

            [MarshalAs(UnmanagedType.U4)]
            public readonly int Right;

            [MarshalAs(UnmanagedType.U4)]
            public readonly int Bottom;
        }

        public static Task<MonitorDescription[]> GetMonitors()
        {
            return Task.Run(() => GetMonitorsInternal().ToArray());
        }

        private static IEnumerable<MonitorDescription> GetMonitorsInternal()
        {
            var monitors = new List<MonitorDescription>();

            var onMonitorCallback = new MonitorEnumProcedure((IntPtr handle, IntPtr dcHandle, ref RectangleL rect, IntPtr callbackObject) =>
            {
                const int temporaryMonitorIndex = 0;
                monitors.Add(new MonitorDescription(temporaryMonitorIndex, rect.Left, rect.Top, rect.Right, rect.Bottom));
                return 1;
            });

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, onMonitorCallback, IntPtr.Zero);

            if (monitors.Count == 0)
            {
                throw new InvalidOperationException("No monitors were found.");
            }

            return monitors.OrderBy(x => x.Left).ThenBy(x => x.Top).Select((monitor, index) => monitor with { Index = index });
        }

        public static Task<Bitmap> Take(Rectangle monitor)
        {
            return Task.Run(() => TakeInternal(monitor));
        }

        private static Bitmap TakeInternal(Rectangle monitor)
        {
            var bitmap = new Bitmap(monitor.Width, monitor.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(monitor.Left, monitor.Top, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }
    }
}
