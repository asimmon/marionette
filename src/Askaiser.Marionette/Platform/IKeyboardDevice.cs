namespace Askaiser.Marionette.Platform;

/// <summary>
/// Simulates a keyboard.
/// </summary>
internal interface IKeyboardDevice
{
    void Press(VirtualKeyCode key);

    void Release(VirtualKeyCode key);

    /// <summary>
    /// Types characters as they are, whatever the layout of the keyboard is. The text does not contain line breaks nor tabulations.
    /// </summary>
    void TypeText(string text);
}
