namespace Askaiser.Marionette.Platform.MacOS;

/// <summary>
/// The bounds of a display in the points macOS uses to position windows and the cursor.
/// <see cref="Right"/> and <see cref="Bottom"/> are exclusive, as in <see cref="Rectangle"/>.
/// </summary>
/// <param name="Left">The X coordinate of the left edge of the display.</param>
/// <param name="Top">The Y coordinate of the top edge of the display.</param>
/// <param name="Width">The width of the display in points.</param>
/// <param name="Height">The height of the display in points.</param>
internal readonly record struct PointsRectangle(double Left, double Top, double Width, double Height)
{
    /// <summary>Gets the X coordinate of the right edge of the display, which is just outside it.</summary>
    public double Right => this.Left + this.Width;

    /// <summary>Gets the Y coordinate of the bottom edge of the display, which is just outside it.</summary>
    public double Bottom => this.Top + this.Height;

    /// <summary>
    /// Returns whether a position in points is inside the display.
    /// </summary>
    public bool Contains(double x, double y) => x >= this.Left && x < this.Right && y >= this.Top && y < this.Bottom;
}
