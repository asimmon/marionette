using Askaiser.Marionette.Platform;

namespace Askaiser.Marionette.Tests.Fakes;

internal sealed class FakeMouseDevice : IMouseDevice
{
    private readonly List<string> _events = [];
    private Point _position = Point.Empty;

    public IReadOnlyList<string> Events => this._events;

    /// <summary>Gets or sets what happens right after an event, which lets a test act in the middle of a gesture.</summary>
    public Action<string>? OnEvent { get; set; }

    public Point GetPosition() => this._position;

    public void MoveTo(int x, int y)
    {
        this._position = new Point(x, y);
        this.Add($"Move {x},{y}");
    }

    public void Press(MouseButton button) => this.Add($"Press {button}");

    public void Release(MouseButton button) => this.Add($"Release {button}");

    public void Scroll(int notches) => this.Add($"Scroll {notches}");

    private void Add(string mouseEvent)
    {
        this._events.Add(mouseEvent);
        this.OnEvent?.Invoke(mouseEvent);
    }
}
