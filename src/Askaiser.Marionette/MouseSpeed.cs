namespace Askaiser.Marionette;

/// <summary>
/// How fast the mouse cursor travels to the position of a mouse action.
/// </summary>
public enum MouseSpeed
{
    /// <summary>The cursor jumps to its destination, without going through the positions in between.</summary>
    Immediate = 0,

    /// <summary>The cursor travels to its destination progressively, in about 0.2 second. This is the default speed.</summary>
    Fast = 1,

    /// <summary>The cursor travels to its destination progressively, in about 0.7 second, which is easier to follow for someone watching the screen.</summary>
    Slow = 2,
}
