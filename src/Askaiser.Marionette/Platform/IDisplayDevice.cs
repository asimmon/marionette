using System;
using System.Collections.Generic;
using Askaiser.Marionette.Imaging;

namespace Askaiser.Marionette.Platform;

/// <summary>
/// Gives access to the monitors of the desktop and to what they display.
/// </summary>
internal interface IDisplayDevice : IDisposable
{
    /// <summary>
    /// Returns the monitors that are currently part of the desktop.
    /// </summary>
    IReadOnlyList<DisplayInfo> GetDisplays();

    /// <summary>
    /// Captures what a monitor currently displays. The returned buffer is opaque and belongs to the caller.
    /// </summary>
    PixelBuffer Capture(DisplayInfo display);
}
