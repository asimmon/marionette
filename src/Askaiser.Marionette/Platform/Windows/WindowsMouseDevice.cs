using System;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace Askaiser.Marionette.Platform.Windows;

[SupportedOSPlatform("windows")]
internal sealed unsafe class WindowsMouseDevice : IMouseDevice
{
    public Point GetPosition()
    {
        using var scope = new DpiAwarenessScope();
        return NativeMethods.GetCursorPos(out var point) != 0 ? new Point(point.X, point.Y) : Point.Empty;
    }

    public void MoveTo(int x, int y)
    {
        using var scope = new DpiAwarenessScope();
        _ = NativeMethods.SetCursorPos(x, y);

        // Moving the cursor does not go through the input queue. A relative move of zero pixels does, which lets
        // applications that listen to raw input notice the new position.
        Send(NativeMethods.MouseEventMove, 0);
    }

    public void Press(MouseButton button)
    {
        Send(button switch
        {
            MouseButton.Left => NativeMethods.MouseEventLeftDown,
            MouseButton.Right => NativeMethods.MouseEventRightDown,
            MouseButton.Middle => NativeMethods.MouseEventMiddleDown,
            _ => throw new ArgumentOutOfRangeException(nameof(button)),
        }, 0);
    }

    public void Release(MouseButton button)
    {
        Send(button switch
        {
            MouseButton.Left => NativeMethods.MouseEventLeftUp,
            MouseButton.Right => NativeMethods.MouseEventRightUp,
            MouseButton.Middle => NativeMethods.MouseEventMiddleUp,
            _ => throw new ArgumentOutOfRangeException(nameof(button)),
        }, 0);
    }

    public void Scroll(int notches)
    {
        Send(NativeMethods.MouseEventWheel, notches * NativeMethods.WheelDelta);
    }

    private static void Send(uint flags, int data)
    {
        var input = new NativeMethods.Input { Type = NativeMethods.InputMouse };
        input.Data.Mouse.Flags = flags;
        input.Data.Mouse.MouseData = data;

        if (NativeMethods.SendInput(1, &input, sizeof(NativeMethods.Input)) != 1)
        {
            // Input is rejected when the active window belongs to a process with a higher privilege level.
            throw new InvalidOperationException(new Win32Exception().Message);
        }
    }
}
