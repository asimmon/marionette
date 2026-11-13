using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Askaiser.Marionette.Platform.Windows;

[SupportedOSPlatform("windows")]
internal static unsafe partial class NativeMethods
{
    public const uint InputMouse = 0;
    public const uint InputKeyboard = 1;

    public const uint MouseEventMove = 0x0001;
    public const uint MouseEventLeftDown = 0x0002;
    public const uint MouseEventLeftUp = 0x0004;
    public const uint MouseEventRightDown = 0x0008;
    public const uint MouseEventRightUp = 0x0010;
    public const uint MouseEventMiddleDown = 0x0020;
    public const uint MouseEventMiddleUp = 0x0040;
    public const uint MouseEventWheel = 0x0800;
    public const int WheelDelta = 120;

    public const uint KeyEventExtendedKey = 0x0001;
    public const uint KeyEventKeyUp = 0x0002;
    public const uint KeyEventUnicode = 0x0004;

    public const uint MapVirtualKeyToScanCode = 0;
    public const uint MonitorInfoPrimary = 1;
    public const uint SourceCopy = 0x00CC0020;
    public const uint CaptureLayeredWindows = 0x40000000;
    public const uint DibRgbColors = 0;
    public const int EffectiveDpi = 0;

    /// <summary>DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2: coordinates are physical pixels on every monitor.</summary>
    public static readonly nint PerMonitorAwareV2 = -4;

    private const string User32 = "user32.dll";
    private const string Gdi32 = "gdi32.dll";
    private const string ShCore = "shcore.dll";

    [LibraryImport(User32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial nint SetThreadDpiAwarenessContext(nint context);

    [LibraryImport(User32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial int EnumDisplayMonitors(nint deviceContext, Rect* clip, delegate* unmanaged[Stdcall]<nint, nint, Rect*, nint, int> callback, nint data);

    [LibraryImport(User32, EntryPoint = "GetMonitorInfoW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial int GetMonitorInfo(nint monitor, MonitorInfo* info);

    [LibraryImport(ShCore)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);

    [LibraryImport(User32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial nint GetDC(nint window);

    [LibraryImport(User32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial int ReleaseDC(nint window, nint deviceContext);

    [LibraryImport(Gdi32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial nint CreateCompatibleDC(nint deviceContext);

    [LibraryImport(Gdi32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial int DeleteDC(nint deviceContext);

    [LibraryImport(Gdi32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial nint CreateDIBSection(nint deviceContext, BitmapInfoHeader* info, uint usage, out void* bits, nint section, uint offset);

    [LibraryImport(Gdi32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial nint SelectObject(nint deviceContext, nint gdiObject);

    [LibraryImport(Gdi32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial int DeleteObject(nint gdiObject);

    [LibraryImport(Gdi32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial int BitBlt(nint destination, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint rasterOperation);

    [LibraryImport(Gdi32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial int GdiFlush();

    [LibraryImport(User32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial int GetCursorPos(out NativePoint point);

    [LibraryImport(User32)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial int SetCursorPos(int x, int y);

    [LibraryImport(User32, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial uint SendInput(uint inputCount, Input* inputs, int inputSize);

    [LibraryImport(User32, EntryPoint = "MapVirtualKeyW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial uint MapVirtualKey(uint code, uint mapType);

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public uint Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ColorsUsed;
        public uint ColorsImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MouseInput
    {
        public int X;
        public int Y;
        public int MouseData;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }
}
