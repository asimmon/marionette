using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Askaiser.Marionette.Imaging;

namespace Askaiser.Marionette.Platform.Windows;

/// <summary>
/// Enumerates monitors and captures them with GDI. The screen is copied to a device-independent bitmap whose memory
/// is directly readable, in the same 32-bit BGRA layout as <see cref="PixelBuffer"/>.
/// </summary>
[SupportedOSPlatform("windows")]
#pragma warning disable CA2216 // Disposable types should declare finalizer: the device lives as long as its driver, which disposes it
internal sealed unsafe class WindowsDisplayDevice : IDisplayDevice
#pragma warning restore CA2216
{
    private const float StandardDpi = 96f;

    private readonly Lock _gate = new();
    private nint _memoryContext;
    private nint _bitmap;
    private nint _previousBitmap;
    private void* _bits;
    private int _width;
    private int _height;

    public IReadOnlyList<DisplayInfo> GetDisplays()
    {
        using var scope = new DpiAwarenessScope();

        var displays = new List<DisplayInfo>();
        var handle = GCHandle.Alloc(displays);
        try
        {
            _ = NativeMethods.EnumDisplayMonitors(0, null, &OnMonitor, GCHandle.ToIntPtr(handle));
        }
        finally
        {
            handle.Free();
        }

        return displays;
    }

    public PixelBuffer Capture(DisplayInfo display)
    {
        using var scope = new DpiAwarenessScope();
        var bounds = display.Bounds;

        lock (this._gate)
        {
            this.EnsureBitmap(bounds.Width, bounds.Height);

            var screenContext = NativeMethods.GetDC(0);
            if (screenContext == 0)
            {
                throw new InvalidOperationException(Messages.Platform_Throw_CaptureFailed.FormatInvariant(new Win32Exception().Message));
            }

            try
            {
                if (NativeMethods.BitBlt(this._memoryContext, 0, 0, bounds.Width, bounds.Height, screenContext, bounds.Left, bounds.Top, NativeMethods.SourceCopy | NativeMethods.CaptureLayeredWindows) == 0)
                {
                    throw new InvalidOperationException(Messages.Platform_Throw_CaptureFailed.FormatInvariant(new Win32Exception().Message));
                }

                _ = NativeMethods.GdiFlush();
            }
            finally
            {
                _ = NativeMethods.ReleaseDC(0, screenContext);
            }

            var buffer = PixelBuffer.Rent(bounds.Width, bounds.Height);
            new ReadOnlySpan<byte>(this._bits, buffer.ByteLength).CopyTo(buffer.Span);
            PixelOperations.MakeOpaque(buffer.Span);
            return buffer;
        }
    }

    public void Dispose()
    {
        lock (this._gate)
        {
            this.ReleaseBitmap();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int OnMonitor(nint monitor, nint deviceContext, NativeMethods.Rect* bounds, nint data)
    {
        var displays = (List<DisplayInfo>)GCHandle.FromIntPtr(data).Target!;

        var info = new NativeMethods.MonitorInfo { Size = (uint)sizeof(NativeMethods.MonitorInfo) };
        if (NativeMethods.GetMonitorInfo(monitor, &info) != 0)
        {
            var scale = 1f;
            try
            {
                if (NativeMethods.GetDpiForMonitor(monitor, NativeMethods.EffectiveDpi, out var dpiX, out _) == 0 && dpiX > 0)
                {
                    scale = dpiX / StandardDpi;
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // Per-monitor DPI does not exist before Windows 8.1.
            }

            var rectangle = new Rectangle(info.Monitor.Left, info.Monitor.Top, info.Monitor.Right, info.Monitor.Bottom);
            displays.Add(new DisplayInfo(rectangle, (info.Flags & NativeMethods.MonitorInfoPrimary) != 0, scale, monitor));
        }

        return 1;
    }

    private void EnsureBitmap(int width, int height)
    {
        if (this._bitmap != 0 && this._width == width && this._height == height)
        {
            return;
        }

        this.ReleaseBitmap();

        var info = new NativeMethods.BitmapInfoHeader
        {
            Size = (uint)sizeof(NativeMethods.BitmapInfoHeader),
            Width = width,
            Height = -height, // A negative height means rows are stored from top to bottom.
            Planes = 1,
            BitCount = 32,
        };

        this._memoryContext = NativeMethods.CreateCompatibleDC(0);
        this._bitmap = NativeMethods.CreateDIBSection(this._memoryContext, &info, NativeMethods.DibRgbColors, out this._bits, 0, 0);

        if (this._memoryContext == 0 || this._bitmap == 0)
        {
            var message = new Win32Exception().Message;
            this.ReleaseBitmap();
            throw new InvalidOperationException(Messages.Platform_Throw_CaptureFailed.FormatInvariant(message));
        }

        this._previousBitmap = NativeMethods.SelectObject(this._memoryContext, this._bitmap);
        this._width = width;
        this._height = height;
    }

    private void ReleaseBitmap()
    {
        if (this._memoryContext != 0)
        {
            if (this._previousBitmap != 0)
            {
                _ = NativeMethods.SelectObject(this._memoryContext, this._previousBitmap);
            }

            _ = NativeMethods.DeleteDC(this._memoryContext);
        }

        if (this._bitmap != 0)
        {
            _ = NativeMethods.DeleteObject(this._bitmap);
        }

        this._memoryContext = 0;
        this._bitmap = 0;
        this._previousBitmap = 0;
        this._bits = null;
    }
}
