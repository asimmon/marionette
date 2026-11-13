using System;
using System.Runtime.Versioning;
using System.Threading;

namespace Askaiser.Marionette.Platform.MacOS;

[SupportedOSPlatform("macos")]
internal static class EventPermission
{
    private static int _granted;
    private static int _requested;

    /// <summary>
    /// Throws when macOS does not let this process simulate input. Without this check, events would be silently ignored.
    /// </summary>
    public static void EnsureGranted()
    {
        if (Volatile.Read(ref _granted) != 0)
        {
            return;
        }

        try
        {
            if (CoreGraphics.CGPreflightPostEventAccess() == 0)
            {
                // Asking for access makes macOS show its permission dialog the first time.
                if (Interlocked.Exchange(ref _requested, 1) == 0)
                {
                    _ = CoreGraphics.CGRequestPostEventAccess();
                }

                if (CoreGraphics.CGPreflightPostEventAccess() == 0)
                {
                    throw new InvalidOperationException(Messages.Platform_MacOS_AccessibilityHint);
                }
            }
        }
        catch (EntryPointNotFoundException)
        {
            // These permissions do not exist before macOS 10.15.
        }

        Volatile.Write(ref _granted, 1);
    }
}
