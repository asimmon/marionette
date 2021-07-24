using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Askaiser.Marionette
{
    internal static class DisplayScreen
    {
        public static Task<MonitorDescription[]> GetMonitors()
        {
            return Task.Run(() => GetMonitorsInternal().ToArray());
        }

        private static IEnumerable<MonitorDescription> GetMonitorsInternal()
        {
            var monitors = new List<MonitorDescription>();
            var monitorIndex = 0;

            var onMonitorCallback = new MonitorEnumProcedure((IntPtr monitorHandle, IntPtr _, ref RectangleL _, IntPtr _) =>
            {
                var monitorInfo = MonitorInfo.Initialize();
                var deviceMode = new DeviceMode(DeviceModeFields.None);

                if (GetMonitorInfo(monitorHandle, ref monitorInfo) && EnumDisplaySettings(monitorInfo.DisplayName, DisplaySettingsMode.CurrentSettings, ref deviceMode))
                {
                    var width = (int)deviceMode.PixelsWidth;
                    var height = (int)deviceMode.PixelsHeight;

                    monitors.Add(new MonitorDescription(
                        Index: monitorIndex++,
                        Left: monitorInfo.Bounds.Left,
                        Top: monitorInfo.Bounds.Top,
                        Right: monitorInfo.Bounds.Left + width,
                        Bottom: monitorInfo.Bounds.Top + height));
                }

                return 1;
            });

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, onMonitorCallback, IntPtr.Zero);

            if (monitors.Count == 0)
            {
                throw new InvalidOperationException("No monitors were found.");
            }

            return monitors;
        }

        #region Interop

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(
            [In] IntPtr dcHandle,
            [In] IntPtr clip,
            MonitorEnumProcedure callback,
            IntPtr callbackObject);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(
            IntPtr monitorHandle,
            ref MonitorInfo monitorInfo);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern bool EnumDisplaySettings(
            string deviceName,
            DisplaySettingsMode mode,
            ref DeviceMode devMode);

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

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            internal uint Size;

            public readonly RectangleL Bounds;

            public readonly RectangleL WorkingArea;

            public readonly MonitorInfoFlags Flags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public readonly string DisplayName;

            public static MonitorInfo Initialize()
            {
                return new MonitorInfo
                {
                    Size = (uint)Marshal.SizeOf(typeof(MonitorInfo)),
                };
            }
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd183565(v=vs.85).aspx
        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
        private readonly struct DeviceMode
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            [FieldOffset(0)]
            public readonly string DeviceName;

            [MarshalAs(UnmanagedType.U2)]
            [FieldOffset(32)]
            public readonly ushort SpecificationVersion;

            [MarshalAs(UnmanagedType.U2)]
            [FieldOffset(34)]
            public readonly ushort DriverVersion;

            [MarshalAs(UnmanagedType.U2)]
            [FieldOffset(36)]
            public readonly ushort Size;

            [MarshalAs(UnmanagedType.U2)]
            [FieldOffset(38)]
            public readonly ushort DriverExtra;

            [MarshalAs(UnmanagedType.U4)]
            [FieldOffset(40)]
            public readonly DeviceModeFields Fields;

            [FieldOffset(44)]
            public readonly PointL Position;

            [MarshalAs(UnmanagedType.U4)]
            [FieldOffset(52)]
            public readonly DisplayOrientation DisplayOrientation;

            [MarshalAs(UnmanagedType.U4)]
            [FieldOffset(56)]
            public readonly DisplayFixedOutput DisplayFixedOutput;

            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(60)]
            public readonly short Color;

            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(62)]
            public readonly short Duplex;

            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(64)]
            public readonly short YResolution;

            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(66)]
            public readonly short TrueTypeOption;

            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(68)]
            public readonly short Collate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            [FieldOffset(72)]
            private readonly string FormName;

            [MarshalAs(UnmanagedType.U2)]
            [FieldOffset(102)]
            public readonly ushort LogicalInchPixels;

            [MarshalAs(UnmanagedType.U4)]
            [FieldOffset(104)]
            public readonly uint BitsPerPixel;

            [MarshalAs(UnmanagedType.U4)]
            [FieldOffset(108)]
            public readonly uint PixelsWidth;

            [MarshalAs(UnmanagedType.U4)]
            [FieldOffset(112)]
            public readonly uint PixelsHeight;

            [MarshalAs(UnmanagedType.U4)]
            [FieldOffset(116)]
            public readonly DisplayFlags DisplayFlags;

            [MarshalAs(UnmanagedType.U4)]
            [FieldOffset(120)]
            public readonly uint DisplayFrequency;

            public DeviceMode(DeviceModeFields fields)
                : this()
            {
                this.SpecificationVersion = 0x0320;
                this.Size = (ushort)Marshal.SizeOf(this.GetType());
                this.Fields = fields;
            }
        }

        // https://msdn.microsoft.com/en-us/library/vs/alm/dd162807(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct PointL
        {
            [MarshalAs(UnmanagedType.I4)]
            public readonly int X;

            [MarshalAs(UnmanagedType.I4)]
            public readonly int Y;
        }

        [Flags]
        private enum MonitorInfoFlags : uint
        {
            None = 0,
            Primary = 1,
        }

        private enum DisplaySettingsMode
        {
            CurrentSettings = -1,
            RegistrySettings = -2,
        }

        [Flags]
        private enum DeviceModeFields : uint
        {
            None = 0,
            Position = 0x20,
            DisplayOrientation = 0x80,
            Color = 0x800,
            Duplex = 0x1000,
            YResolution = 0x2000,
            TtOption = 0x4000,
            Collate = 0x8000,
            FormName = 0x10000,
            LogPixels = 0x20000,
            BitsPerPixel = 0x40000,
            PelsWidth = 0x80000,
            PelsHeight = 0x100000,
            DisplayFlags = 0x200000,
            DisplayFrequency = 0x400000,
            DisplayFixedOutput = 0x20000000,

            AllDisplay = Position | DisplayOrientation | YResolution | BitsPerPixel | PelsWidth | PelsHeight | DisplayFlags | DisplayFrequency | DisplayFixedOutput,
        }

        [Flags]
        private enum DisplayFlags : uint
        {
            None = 0,
            Grayscale = 1,
            Interlaced = 2,
        }

        private enum DisplayOrientation : uint
        {
            Identity = 0,
            Rotate90Degree = 1,
            Rotate180Degree = 2,
            Rotate270Degree = 3,
        }

        private enum DisplayFixedOutput : uint
        {
            Default = 0,
            Stretch = 1,
            Center = 2,
        }

        #endregion
    }
}
