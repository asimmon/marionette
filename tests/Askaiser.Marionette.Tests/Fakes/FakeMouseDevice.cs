using Askaiser.Marionette.Platform;

namespace Askaiser.Marionette.Tests.Fakes;

internal sealed class FakeMouseDevice : IMouseDevice
{
    private readonly List<string> _events = [];
    private Point _position = Point.Empty;

    public IReadOnlyList<string> Events => this._events;

    public Point GetPosition() => this._position;

    public void MoveTo(int x, int y)
    {
        this._position = new Point(x, y);
        this._events.Add($"Move {x},{y}");
    }

    public void Press(MouseButton button) => this._events.Add($"Press {button}");

    public void Release(MouseButton button) => this._events.Add($"Release {button}");

    public void Scroll(int notches) => this._events.Add($"Scroll {notches}");
}
