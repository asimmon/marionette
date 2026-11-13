using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Askaiser.Marionette.Platform.Linux;

/// <summary>
/// A connection to the X server, shared by the display, mouse and keyboard devices.
/// Xlib calls on a connection must not run concurrently, so every use of <see cref="Display"/> happens inside <see cref="Gate"/>.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed unsafe class X11Connection : IDisposable
{
    private static int _lastErrorCode;
    private static int _errorHandlerInstalled;

    private nint _display;

    private X11Connection(nint display)
    {
        this._display = display;
        this.Screen = Xlib.XDefaultScreen(display);
        this.RootWindow = Xlib.XRootWindow(display, this.Screen);
    }

    public Lock Gate { get; } = new();

    public nint Display
    {
        get
        {
            ObjectDisposedException.ThrowIf(this._display == 0, this);
            return this._display;
        }
    }

    public int Screen { get; }

    public nuint RootWindow { get; }

    public static X11Connection Open()
    {
        var displayName = Environment.GetEnvironmentVariable("DISPLAY");
        nint display;

        try
        {
            _ = Xlib.XInitThreads();

            // By default Xlib terminates the process when the server reports an error. Record errors instead.
            if (Interlocked.Exchange(ref _errorHandlerInstalled, 1) == 0)
            {
                _ = Xlib.XSetErrorHandler(&OnError);
            }

            display = Xlib.XOpenDisplay(null);
        }
        catch (DllNotFoundException ex)
        {
            throw new PlatformNotSupportedException(Messages.Platform_Throw_LibraryMissing.FormatInvariant("libX11.so.6", Messages.Platform_Throw_X11DisplayUnavailable.FormatInvariant(displayName ?? string.Empty)), ex);
        }

        if (display == 0)
        {
            throw new PlatformNotSupportedException(Messages.Platform_Throw_X11DisplayUnavailable.FormatInvariant(displayName ?? string.Empty));
        }

        return new X11Connection(display);
    }

    /// <summary>
    /// Forgets the last error reported by the server. Must be called inside <see cref="Gate"/>.
    /// </summary>
    public void ClearError()
    {
        _ = Xlib.XSync(this.Display, 0);
        Volatile.Write(ref _lastErrorCode, 0);
    }

    /// <summary>
    /// Waits for the server to process pending requests and returns the code of the error it reported, or zero. Must be called inside <see cref="Gate"/>.
    /// </summary>
    public int GetError()
    {
        _ = Xlib.XSync(this.Display, 0);
        return Interlocked.Exchange(ref _lastErrorCode, 0);
    }

    public void Dispose()
    {
        lock (this.Gate)
        {
            var display = Interlocked.Exchange(ref this._display, 0);
            if (display != 0)
            {
                _ = Xlib.XCloseDisplay(display);
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OnError(nint display, Xlib.XErrorEvent* error)
    {
        Volatile.Write(ref _lastErrorCode, error->ErrorCode);
        return 0;
    }
}
