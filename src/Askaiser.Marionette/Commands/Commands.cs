#pragma warning disable SA1649

using System;
using System.Collections.Generic;

namespace Askaiser.Marionette.Commands;

internal record WaitForCommand(IReadOnlyCollection<IElement> Elements, TimeSpan WaitFor, Rectangle? SearchRectangle, int MonitorIndex, NoSingleResultBehavior Behavior);

internal record MouseLocationCommand(int X, int Y, MouseSpeed Speed);

internal record MouseWheelCommand(bool IsUp);

internal record KeyboardTextCommand(string Text);

internal record KeyboardKeysCommand(params VirtualKeyCode[] KeyCodes);
