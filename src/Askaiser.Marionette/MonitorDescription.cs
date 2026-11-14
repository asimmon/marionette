namespace Askaiser.Marionette;

/// <summary>
/// A monitor of the desktop and its bounds in pixels, in the coordinates of the virtual desktop that spans all monitors.
/// As a <see cref="Rectangle"/>, it can be given to <c>searchRect</c> parameters, or narrowed first with methods such as <see cref="Rectangle.FromTopLeft(int, int)"/>.
/// </summary>
/// <param name="Index">The position of the monitor in the list of monitors of the desktop. Index zero is the primary monitor.</param>
/// <param name="Left">The X coordinate of the left edge of the monitor.</param>
/// <param name="Top">The Y coordinate of the top edge of the monitor.</param>
/// <param name="Right">The X coordinate of the right edge of the monitor, which is exclusive.</param>
/// <param name="Bottom">The Y coordinate of the bottom edge of the monitor, which is exclusive.</param>
public record MonitorDescription(int Index, int Left, int Top, int Right, int Bottom)
    : Rectangle(Left, Top, Right, Bottom)
{
    /// <summary>
    /// Gets a value indicating whether this is the primary monitor, which is the one whose top-left corner is the origin of the desktop.
    /// </summary>
    public bool IsPrimary
    {
        get => this.Left == 0 && this.Top == 0;
    }

    /// <summary>Returns the index, the bounds and the size of the monitor, formatted as <c>Monitor #index (left,top,right,bottom) widthxheight</c>.</summary>
    public override string ToString()
    {
        return Messages.MonitorDescription_ToString.FormatInvariant(this.Index, base.ToString(), this.Width, this.Height);
    }
}
