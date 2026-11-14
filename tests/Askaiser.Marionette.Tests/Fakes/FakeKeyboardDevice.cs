using Askaiser.Marionette.Platform;

namespace Askaiser.Marionette.Tests.Fakes;

internal sealed class FakeKeyboardDevice : IKeyboardDevice
{
    private readonly List<string> _events = [];

    public IReadOnlyList<string> Events => this._events;

    /// <summary>Gets or sets what happens right after an event, which lets a test act in the middle of a sequence of keys.</summary>
    public Action<string>? OnEvent { get; set; }

    public void Press(VirtualKeyCode key) => this.Add($"Press {key}");

    public void Release(VirtualKeyCode key) => this.Add($"Release {key}");

    public void TypeText(string text) => this.Add($"Type {text}");

    private void Add(string keyboardEvent)
    {
        this._events.Add(keyboardEvent);
        this.OnEvent?.Invoke(keyboardEvent);
    }
}
