using Askaiser.Marionette.Platform;

namespace Askaiser.Marionette.Tests.Fakes;

internal sealed class FakeKeyboardDevice : IKeyboardDevice
{
    private readonly List<string> _events = [];

    public IReadOnlyList<string> Events => this._events;

    public void Press(VirtualKeyCode key) => this._events.Add($"Press {key}");

    public void Release(VirtualKeyCode key) => this._events.Add($"Release {key}");

    public void TypeText(string text) => this._events.Add($"Type {text}");
}
