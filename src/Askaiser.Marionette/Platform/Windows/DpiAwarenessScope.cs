using System;
using System.Runtime.Versioning;

namespace Askaiser.Marionette.Platform.Windows;

/// <summary>
/// Makes the current thread see the desktop in physical pixels for the duration of a native call. Without it, Windows
/// reports scaled coordinates and stretched screenshots to processes that did not declare themselves aware of high pixel densities.
/// </summary>
[SupportedOSPlatform("windows")]
internal readonly ref struct DpiAwarenessScope
{
    private readonly nint _previous;

    public DpiAwarenessScope()
    {
        try
        {
            this._previous = NativeMethods.SetThreadDpiAwarenessContext(NativeMethods.PerMonitorAwareV2);
        }
        catch (EntryPointNotFoundException)
        {
            // Windows versions older than Windows 10 1607 do not have per-thread awareness.
            this._previous = 0;
        }
    }

    public void Dispose()
    {
        if (this._previous != 0)
        {
            _ = NativeMethods.SetThreadDpiAwarenessContext(this._previous);
        }
    }
}
