using System;
using System.Runtime.Versioning;

namespace Askaiser.Marionette.Platform.Linux;

/// <summary>
/// Simulates a mouse with the XTEST extension, whose events are indistinguishable from those of a real device for applications.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class X11MouseDevice : IMouseDevice
{
    private const uint WheelUpButton = 4;
    private const uint WheelDownButton = 5;

    private readonly X11Connection _connection;

    public X11MouseDevice(X11Connection connection)
    {
        this._connection = connection;
        XTest.EnsureAvailable(connection);
    }

    public Point GetPosition()
    {
        lock (this._connection.Gate)
        {
            _ = Xlib.XQueryPointer(this._connection.Display, this._connection.RootWindow, out _, out _, out var x, out var y, out _, out _, out _);
            return new Point(x, y);
        }
    }

    public void MoveTo(int x, int y)
    {
        lock (this._connection.Gate)
        {
            _ = Xlib.XTestFakeMotionEvent(this._connection.Display, this._connection.Screen, x, y, 0);
            _ = Xlib.XFlush(this._connection.Display);
        }
    }

    public void Press(MouseButton button)
    {
        this.SendButton(ToX11Button(button), isPress: true);
    }

    public void Release(MouseButton button)
    {
        this.SendButton(ToX11Button(button), isPress: false);
    }

    public void Scroll(int notches)
    {
        // X11 has no wheel events: a notch is a click of the fourth or fifth button.
        var button = notches > 0 ? WheelUpButton : WheelDownButton;

        lock (this._connection.Gate)
        {
            for (var i = 0; i < Math.Abs(notches); i++)
            {
                _ = Xlib.XTestFakeButtonEvent(this._connection.Display, button, 1, 0);
                _ = Xlib.XTestFakeButtonEvent(this._connection.Display, button, 0, 0);
            }

            _ = Xlib.XFlush(this._connection.Display);
        }
    }

    private static uint ToX11Button(MouseButton button) => button switch
    {
        MouseButton.Left => 1,
        MouseButton.Middle => 2,
        MouseButton.Right => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(button)),
    };

    private void SendButton(uint button, bool isPress)
    {
        lock (this._connection.Gate)
        {
            _ = Xlib.XTestFakeButtonEvent(this._connection.Display, button, isPress ? 1 : 0, 0);
            _ = Xlib.XFlush(this._connection.Display);
        }
    }
}
