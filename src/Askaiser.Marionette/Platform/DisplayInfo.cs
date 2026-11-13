namespace Askaiser.Marionette.Platform;

/// <summary>
/// A monitor as seen by the operating system.
/// </summary>
/// <param name="Bounds">The bounds of the monitor in pixels, in the coordinates of the virtual desktop that spans all monitors.</param>
/// <param name="IsPrimary">Whether this is the main monitor of the desktop.</param>
/// <param name="ScaleFactor">The scale applied by the operating system to user interfaces on this monitor: 1 at standard pixel density, 2 on most high density screens.</param>
/// <param name="Handle">The identifier of the monitor for the operating system.</param>
internal sealed record DisplayInfo(Rectangle Bounds, bool IsPrimary, float ScaleFactor, nint Handle);
