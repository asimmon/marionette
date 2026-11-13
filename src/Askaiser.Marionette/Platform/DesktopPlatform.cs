using System;
using System.Runtime.InteropServices;
using Askaiser.Marionette.Platform.Linux;
using Askaiser.Marionette.Platform.MacOS;
using Askaiser.Marionette.Platform.Windows;

namespace Askaiser.Marionette.Platform;

/// <summary>
/// The devices of the desktop the current process runs in: its monitors, its mouse and its keyboard.
/// </summary>
internal sealed class DesktopPlatform : IDisposable
{
    private readonly IDisposable? _session;

    public DesktopPlatform(IDisplayDevice display, IMouseDevice mouse, IKeyboardDevice keyboard, IDisposable? session = null)
    {
        this.Display = display;
        this.Mouse = mouse;
        this.Keyboard = keyboard;
        this._session = session;
    }

    public IDisplayDevice Display { get; }

    public IMouseDevice Mouse { get; }

    public IKeyboardDevice Keyboard { get; }

    /// <summary>
    /// Creates the devices of the current operating system.
    /// </summary>
    public static DesktopPlatform Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new DesktopPlatform(new WindowsDisplayDevice(), new WindowsMouseDevice(), new WindowsKeyboardDevice());
        }

        if (OperatingSystem.IsLinux())
        {
            var connection = X11Connection.Open();
            return new DesktopPlatform(new X11DisplayDevice(connection), new X11MouseDevice(connection), new X11KeyboardDevice(connection), connection);
        }

        if (OperatingSystem.IsMacOS())
        {
            var displays = new MacDisplayDevice();
            return new DesktopPlatform(displays, new MacMouseDevice(displays), new MacKeyboardDevice());
        }

        throw new PlatformNotSupportedException(Messages.Platform_Throw_Unsupported.FormatInvariant(RuntimeInformation.OSDescription));
    }

    public void Dispose()
    {
        this.Display.Dispose();
        this._session?.Dispose();
    }
}
