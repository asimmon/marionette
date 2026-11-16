namespace Askaiser.Marionette.Platform.MacOS;

/// <summary>
/// A display as Quartz describes it, before it is placed in the pixel coordinates of Marionette by <see cref="MacDisplayLayout"/>.
/// </summary>
/// <param name="Id">The identifier Quartz gives to the display.</param>
/// <param name="IsMain">Whether this is the main display, whose top-left corner is the origin of the desktop in both coordinate systems.</param>
/// <param name="Points">The bounds of the display in points, as reported by <c>CGDisplayBounds</c>.</param>
/// <param name="Scale">The number of pixels the display shows per point: 1 at standard pixel density, 2 on most Mac screens.</param>
internal readonly record struct MacDisplay(uint Id, bool IsMain, PointsRectangle Points, double Scale);
