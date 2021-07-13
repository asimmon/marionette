using System;
using System.Collections.Generic;

namespace Askaiser.UITesting.Commands
{
    internal record WaitForCommand(IReadOnlyCollection<IElement> Elements, TimeSpan WaitFor, Rectangle SearchRectangle, int MonitorIndex, NotFoundBehavior Behavior);

    internal record MouseLocationCommand(int X, int Y, MouseSpeed Speed);

    internal record MouseWheelCommand(bool IsUp);

    internal record KeyboardTextCommand(string Text);

    internal record KeyboardKeysCommand(params VirtualKeyCode[] KeyCodes);
}
