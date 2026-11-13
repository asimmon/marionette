namespace Askaiser.Marionette.Platform;

/// <summary>
/// Simulates a mouse. Coordinates are in pixels, in the coordinates of the virtual desktop that spans all monitors.
/// </summary>
internal interface IMouseDevice
{
    Point GetPosition();

    void MoveTo(int x, int y);

    void Press(MouseButton button);

    void Release(MouseButton button);

    /// <summary>
    /// Rotates the wheel by a number of notches: positive values scroll up, negative values scroll down.
    /// </summary>
    void Scroll(int notches);
}
