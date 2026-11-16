using System;

namespace Askaiser.Marionette.Platform.MacOS;

/// <summary>
/// A monitor in both coordinate systems of macOS: the bounds in points that macOS positions windows and the cursor
/// with, and the bounds in pixels that Marionette exposes. <see cref="MacDisplayLayout"/> describes how both relate.
/// </summary>
/// <param name="Id">The identifier Quartz gives to the display.</param>
/// <param name="IsMain">Whether this is the main display, whose top-left corner is the origin of the desktop in both coordinate systems.</param>
/// <param name="Points">The bounds of the display in points, as reported by <c>CGDisplayBounds</c>.</param>
/// <param name="Pixels">
/// The bounds of the display in the pixel coordinates of the virtual desktop. Its size is the size of a capture of
/// that display, which is its size in points multiplied by its own <paramref name="Scale"/>, while its origin comes
/// from the layout of the whole desktop.
/// </param>
/// <param name="Scale">The number of pixels the display shows per point: 1 at standard pixel density, 2 on most Mac screens.</param>
internal readonly record struct DisplayGeometry(uint Id, bool IsMain, PointsRectangle Points, Rectangle Pixels, double Scale)
{
    /// <summary>
    /// Converts a position in the pixels of the virtual desktop to the points macOS expects, as if it were on this monitor.
    /// </summary>
    public (double X, double Y) ToPoints(int x, int y)
    {
        return (
            this.Points.Left + ((x - this.Pixels.Left) / this.Scale),
            this.Points.Top + ((y - this.Pixels.Top) / this.Scale));
    }

    /// <summary>
    /// Converts a position in the points of macOS to the pixels of the virtual desktop, as if it were on this monitor.
    /// </summary>
    public Point ToPixels(double x, double y)
    {
        return new Point(
            this.Pixels.Left + (int)Math.Round((x - this.Points.Left) * this.Scale),
            this.Pixels.Top + (int)Math.Round((y - this.Points.Top) * this.Scale));
    }
}
