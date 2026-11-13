using System;
using System.Runtime.Versioning;

namespace Askaiser.Marionette.Platform.Linux;

[SupportedOSPlatform("linux")]
internal static class XTest
{
    /// <summary>
    /// Throws when the XTEST extension, which input simulation relies on, cannot be used.
    /// </summary>
    public static void EnsureAvailable(X11Connection connection)
    {
        try
        {
            lock (connection.Gate)
            {
                if (Xlib.XTestQueryExtension(connection.Display, out _, out _, out _, out _) == 0)
                {
                    throw new PlatformNotSupportedException(Messages.Platform_Throw_X11ExtensionMissing.FormatInvariant("XTEST"));
                }
            }
        }
        catch (DllNotFoundException ex)
        {
            throw new PlatformNotSupportedException(Messages.Platform_Throw_LibraryMissing.FormatInvariant("libXtst.so.6", "Install the package that provides it, named libxtst6 on Debian and Ubuntu, and libXtst on Fedora."), ex);
        }
    }
}
