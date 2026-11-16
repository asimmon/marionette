namespace Askaiser.Marionette.Platform;

/// <summary>
/// A monitor as seen by the operating system.
/// </summary>
/// <param name="Bounds">The bounds of the monitor in pixels, in the coordinates of the virtual desktop that spans all monitors.</param>
/// <param name="IsPrimary">
/// Whether the operating system designates this monitor as the main one of the desktop. It is read from the operating system
/// rather than deduced from <paramref name="Bounds"/>: the primary monitor sits at the origin of the desktop on Windows and
/// macOS, but X11 lets the user mark any output as primary, whatever its position, and lets them mark none at all.
/// </param>
/// <param name="ScaleFactor">The scale applied by the operating system to user interfaces on this monitor: 1 at standard pixel density, 2 on most high density screens.</param>
/// <param name="Handle">The identifier of the monitor for the operating system.</param>
internal sealed record DisplayInfo(Rectangle Bounds, bool IsPrimary, float ScaleFactor, nint Handle);
