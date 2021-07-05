using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.UITesting
{
    internal class MouseController : IMouseController
    {
        private const int AverageHumanDelayBetweenDownAndUp = 30;
        private const int AverageHumanDoubleClickDuration = 70;
        private const int DelayBeforeFirstClickAction = 100;

        private static readonly Dictionary<MouseSpeed, int> MouseSpeedSteps = new()
        {
            [MouseSpeed.Immediate] = 0,
            [MouseSpeed.Fast] = 80,
            [MouseSpeed.Slow] = 160,
        };

        public async Task Move(int x, int y, MouseSpeed speed)
        {
            var steps = MouseSpeedSteps[speed];

            if (steps > 0)
            {
                var startPos = MouseInterop.GetCursorPosition();
                var slope = new PointF(x - startPos.X, y - startPos.Y);

                slope.X /= steps;
                slope.Y /= steps;

                PointF iterPos = startPos;
                for (var i = 0; i < steps; i++)
                {
                    iterPos = new PointF(iterPos.X + slope.X, iterPos.Y + slope.Y);
                    MouseInterop.SetCursorPosition(System.Drawing.Point.Round(iterPos));
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

        public async Task Drag(int x, int y, MouseSpeed speed)
        {
            await this.Move(x, y, speed).ConfigureAwait(false);
            await Task.Delay(DelayBeforeFirstClickAction).ConfigureAwait(false);
            MouseInterop.MouseClickEvent(MouseInterop.MouseEventFlags.LeftDown);
        }

        public async Task Drop(int x, int y, MouseSpeed speed)
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
}