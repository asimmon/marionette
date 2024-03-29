﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Askaiser.Marionette;

[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Keep interop code close to its usage")]
internal static class MouseInterop
{
    // One wheel click is defined as WHEEL_DELTA, which is 120.
    // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-mouse_event
    private const int WheelDelta = 120;

    [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPositionInterop(int x, int y);

    [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPositionInterop(out MousePoint lpMousePoint);

    [DllImport("user32.dll", EntryPoint = "mouse_event")]
    private static extern void MouseEventInterop(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    public static void SetCursorPosition(int x, int y)
    {
        SetCursorPositionInterop(x, y);
    }

    public static Point GetCursorPosition()
    {
        return GetCursorPositionInterop(out var position) ? new Point(position.X, position.Y) : Point.Empty;
    }

    public static void MouseClickEvent(MouseEventFlags value)
    {
        var (x, y) = GetCursorPosition();
        MouseEventInterop((int)value, x, y, 0, 0);
    }

    public static void MouseWheelEventUp() => MouseWheelEvent(true);

    public static void MouseWheelEventDown() => MouseWheelEvent(false);

    private static void MouseWheelEvent(bool isUp)
    {
        var dwFlags = isUp ? WheelDelta : -WheelDelta;
        var position = GetCursorPosition();
        MouseEventInterop((int)MouseEventFlags.Wheel, position.X, position.Y, dwFlags, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct MousePoint
    {
        public readonly int X;
        public readonly int Y;
    }

    [Flags]
    internal enum MouseEventFlags
    {
        LeftDown = 0x00000002,
        LeftUp = 0x00000004,
        RightDown = 0x00000008,
        RightUp = 0x00000010,
        MiddleDown = 0x00000020,
        MiddleUp = 0x00000040,
        Wheel = 0x00000800,
    }
}
