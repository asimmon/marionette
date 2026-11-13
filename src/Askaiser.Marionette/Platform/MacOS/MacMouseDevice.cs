using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;

namespace Askaiser.Marionette.Platform.MacOS;

/// <summary>
/// Simulates a mouse with Quartz events.
/// </summary>
/// <remarks>
/// Unlike other systems, macOS does not derive double clicks nor drags from the sequence of events: an event must say
/// that it is the second click of a double click, and moving the mouse while a button is down must be reported as a drag.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed class MacMouseDevice : IMouseDevice
{
    private static readonly TimeSpan MultipleClickInterval = TimeSpan.FromMilliseconds(500);

    private const int MultipleClickDistance = 4;
    private const int MaxClickCount = 3;

    private readonly MacDisplayDevice _displays;
    private readonly Lock _gate = new();
    private readonly bool[] _pressed = new bool[3];
    private MouseButton _lastButton;
    private Point _lastPressPosition = Point.Empty;
    private long _lastPressTimestamp;
    private int _clickCount;

    public MacMouseDevice(MacDisplayDevice displays)
    {
        this._displays = displays;
    }

    public Point GetPosition()
    {
        var cgEvent = CoreGraphics.CGEventCreate(0);
        if (cgEvent == 0)
        {
            return Point.Empty;
        }

        try
        {
            return this._displays.ToPixels(CoreGraphics.CGEventGetLocation(cgEvent));
        }
        finally
        {
            CoreGraphics.CFRelease(cgEvent);
        }
    }

    public void MoveTo(int x, int y)
    {
        lock (this._gate)
        {
            var (type, button) = this._pressed[(int)MouseButton.Left] ? (CoreGraphics.EventLeftMouseDragged, CoreGraphics.MouseButtonLeft)
                : this._pressed[(int)MouseButton.Right] ? (CoreGraphics.EventRightMouseDragged, CoreGraphics.MouseButtonRight)
                : this._pressed[(int)MouseButton.Middle] ? (CoreGraphics.EventOtherMouseDragged, CoreGraphics.MouseButtonCenter)
                : (CoreGraphics.EventMouseMoved, CoreGraphics.MouseButtonLeft);

            this.Post(type, button, this._displays.ToPoints(x, y), clickCount: 0);
        }
    }

    public void Press(MouseButton button)
    {
        lock (this._gate)
        {
            var position = this.GetPosition();
            var now = Stopwatch.GetTimestamp();

            var continuesClickSequence = this._clickCount > 0
                && this._clickCount < MaxClickCount
                && button == this._lastButton
                && Stopwatch.GetElapsedTime(this._lastPressTimestamp, now) <= MultipleClickInterval
                && Math.Abs(position.X - this._lastPressPosition.X) <= MultipleClickDistance
                && Math.Abs(position.Y - this._lastPressPosition.Y) <= MultipleClickDistance;

            this._clickCount = continuesClickSequence ? this._clickCount + 1 : 1;
            this._lastButton = button;
            this._lastPressPosition = position;
            this._lastPressTimestamp = now;
            this._pressed[(int)button] = true;

            var (type, nativeButton) = button switch
            {
                MouseButton.Left => (CoreGraphics.EventLeftMouseDown, CoreGraphics.MouseButtonLeft),
                MouseButton.Right => (CoreGraphics.EventRightMouseDown, CoreGraphics.MouseButtonRight),
                _ => (CoreGraphics.EventOtherMouseDown, CoreGraphics.MouseButtonCenter),
            };

            this.Post(type, nativeButton, this._displays.ToPoints(position.X, position.Y), this._clickCount);
        }
    }

    public void Release(MouseButton button)
    {
        lock (this._gate)
        {
            var position = this.GetPosition();
            this._pressed[(int)button] = false;

            var (type, nativeButton) = button switch
            {
                MouseButton.Left => (CoreGraphics.EventLeftMouseUp, CoreGraphics.MouseButtonLeft),
                MouseButton.Right => (CoreGraphics.EventRightMouseUp, CoreGraphics.MouseButtonRight),
                _ => (CoreGraphics.EventOtherMouseUp, CoreGraphics.MouseButtonCenter),
            };

            this.Post(type, nativeButton, this._displays.ToPoints(position.X, position.Y), Math.Max(1, this._clickCount));
        }
    }

    public void Scroll(int notches)
    {
        EventPermission.EnsureGranted();

        var cgEvent = CoreGraphics.CGEventCreateScrollWheelEvent2(0, CoreGraphics.ScrollEventUnitLine, 1, notches, 0, 0);
        if (cgEvent != 0)
        {
            CoreGraphics.CGEventPost(CoreGraphics.HidEventTap, cgEvent);
            CoreGraphics.CFRelease(cgEvent);
        }
    }

    private void Post(uint type, uint button, CoreGraphics.CGPoint position, int clickCount)
    {
        EventPermission.EnsureGranted();

        var cgEvent = CoreGraphics.CGEventCreateMouseEvent(0, type, position, button);
        if (cgEvent == 0)
        {
            return;
        }

        if (clickCount > 0)
        {
            CoreGraphics.CGEventSetIntegerValueField(cgEvent, CoreGraphics.MouseEventClickState, clickCount);
        }

        CoreGraphics.CGEventPost(CoreGraphics.HidEventTap, cgEvent);
        CoreGraphics.CFRelease(cgEvent);
    }
}
