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
/// <param name="IsPrimary">
/// Whether this is the primary monitor of the desktop, as designated by the operating system: the monitor that carries the
/// taskbar on Windows, the menu bar on macOS, and the output marked primary with <c>xrandr</c> on Linux. Exactly one monitor
/// of a desktop is primary, and it is always the one at index zero.
/// </param>
/// <remarks>
/// The primary monitor is not deduced from its position. Its top-left corner is the origin of the virtual desktop on Windows
/// and macOS, but not on Linux, where the origin is the top-left corner of the rectangle that encloses every monitor and where
/// the primary monitor is a choice of the user that is independent of how monitors are arranged.
/// </remarks>
public record MonitorDescription(int Index, int Left, int Top, int Right, int Bottom, bool IsPrimary)
    : Rectangle(Left, Top, Right, Bottom)
{
    /// <summary>Returns the index, the bounds and the size of the monitor, formatted as <c>Monitor #index (left,top,right,bottom) widthxheight</c>.</summary>
    public override string ToString()
    {
        return Messages.MonitorDescription_ToString.FormatInvariant(this.Index, base.ToString(), this.Width, this.Height);
    }
}
