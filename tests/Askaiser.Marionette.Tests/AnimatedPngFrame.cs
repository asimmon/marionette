namespace Askaiser.Marionette.Tests;

/// <summary>
/// What a program that plays an animated PNG file displays: the whole picture after each frame, and for how long.
/// </summary>
internal sealed record AnimatedPngFrame(Image Picture, TimeSpan Delay);
