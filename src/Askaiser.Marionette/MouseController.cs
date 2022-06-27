using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

internal class MouseController : IMouseController
{
    private const int AverageHumanDelayBetweenDownAndUp = 30;
    private const int AverageHumanDoubleClickDuration = 70;
    private const int DelayBeforeFirstClickAction = 100;

    private static readonly Dictionary<MouseSpeed, int> MouseSpeedSteps = new Dictionary<MouseSpeed, int>
    {
        [MouseSpeed.Immediate] = 0,
        [MouseSpeed.Fast] = 60,
        [MouseSpeed.Slow] = 160,
    };

    public Point GetCurrentPosition()
    {
        return MouseInterop.GetCursorPosition();
    }

    public async Task Move(int x, int y, MouseSpeed speed)
    {
        float steps = MouseSpeedSteps[speed];

        if (steps > 0)
        {
            var startPos = MouseInterop.GetCursorPosition();

            var slopeX = (x - startPos.X) / steps;
            var slopeY = (y - startPos.Y) / steps;

            float iterPosX = startPos.X;
            float iterPosY = startPos.Y;

            for (var i = 0; i < steps; i++)
            {
                iterPosX += slopeX;
                iterPosY += slopeY;

                MouseInterop.SetCursorPosition(unchecked((int)Math.Round(iterPosX)), unchecked((int)Math.Round(iterPosY)));
                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        MouseInterop.SetCursorPosition(x, y);
    }

    public async Task SingleClick(int x, int y, MouseSpeed speed)
    {
        await this.Move(x, y, speed).ConfigureAwait(false);
        await Task.Delay(DelayBeforeFirstClickAction).ConfigureAwait(false);
        await SingleClickWithoutMove().ConfigureAwait(false);
    }

    public async Task DoubleClick(int x, int y, MouseSpeed speed)
    {
        await this.Move(x, y, speed).ConfigureAwait(false);
        await Task.Delay(DelayBeforeFirstClickAction).ConfigureAwait(false);
        await SingleClickWithoutMove().ConfigureAwait(false);
        await Task.Delay(AverageHumanDoubleClickDuration).ConfigureAwait(false);
        await SingleClickWithoutMove().ConfigureAwait(false);
    }

    public async Task TripleClick(int x, int y, MouseSpeed speed)
    {
        await this.Move(x, y, speed).ConfigureAwait(false);
        await Task.Delay(DelayBeforeFirstClickAction).ConfigureAwait(false);
        await SingleClickWithoutMove().ConfigureAwait(false);
        await Task.Delay(AverageHumanDoubleClickDuration).ConfigureAwait(false);
        await SingleClickWithoutMove().ConfigureAwait(false);
        await Task.Delay(AverageHumanDoubleClickDuration).ConfigureAwait(false);
        await SingleClickWithoutMove().ConfigureAwait(false);
    }

    private static async Task SingleClickWithoutMove()
    {
        MouseInterop.MouseClickEvent(MouseInterop.MouseEventFlags.LeftDown);
        await Task.Delay(AverageHumanDelayBetweenDownAndUp).ConfigureAwait(false);
        MouseInterop.MouseClickEvent(MouseInterop.MouseEventFlags.LeftUp);
    }

    public async Task RightClick(int x, int y, MouseSpeed speed)
    {
        await this.Move(x, y, speed).ConfigureAwait(false);
        await Task.Delay(DelayBeforeFirstClickAction).ConfigureAwait(false);
        MouseInterop.MouseClickEvent(MouseInterop.MouseEventFlags.RightDown);
        await Task.Delay(AverageHumanDelayBetweenDownAndUp).ConfigureAwait(false);
        MouseInterop.MouseClickEvent(MouseInterop.MouseEventFlags.RightUp);
    }

    public async Task DragFrom(int x, int y, MouseSpeed speed)
    {
        await this.Move(x, y, speed).ConfigureAwait(false);
        await Task.Delay(DelayBeforeFirstClickAction).ConfigureAwait(false);
        MouseInterop.MouseClickEvent(MouseInterop.MouseEventFlags.LeftDown);
    }

    public async Task DropTo(int x, int y, MouseSpeed speed)
    {
        await this.Move(x, y, speed).ConfigureAwait(false);
        await Task.Delay(DelayBeforeFirstClickAction).ConfigureAwait(false);
        MouseInterop.MouseClickEvent(MouseInterop.MouseEventFlags.LeftUp);
    }

    public async Task WheelUp()
    {
        MouseInterop.MouseWheelEventUp();
        await Task.Delay(DelayBeforeFirstClickAction).ConfigureAwait(false);
    }

    public async Task WheelDown()
    {
        MouseInterop.MouseWheelEventDown();
        await Task.Delay(DelayBeforeFirstClickAction).ConfigureAwait(false);
    }
}
