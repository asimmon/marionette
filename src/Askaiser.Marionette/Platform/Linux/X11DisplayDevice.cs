using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Askaiser.Marionette.Imaging;

namespace Askaiser.Marionette.Platform.Linux;

/// <summary>
/// Enumerates monitors with the RandR extension and captures them with the MIT-SHM extension, which lets the X server
/// write pixels directly in memory shared with this process. When an extension is missing, it falls back to core X11 requests.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed unsafe class X11DisplayDevice : IDisplayDevice
{
    private const float StandardDpi = 96f;

    private readonly X11Connection _connection;
    private bool _sharedMemoryAvailable;
    private Xlib.XImage* _sharedImage;
    private Xlib.XShmSegmentInfo* _sharedSegment;

    public X11DisplayDevice(X11Connection connection)
    {
        this._connection = connection;

        try
        {
            lock (connection.Gate)
            {
                this._sharedMemoryAvailable = Xlib.XShmQueryExtension(connection.Display) != 0;
            }
        }
        catch (DllNotFoundException)
        {
            this._sharedMemoryAvailable = false;
        }
    }

    public IReadOnlyList<DisplayInfo> GetDisplays()
    {
        lock (this._connection.Gate)
        {
            var display = this._connection.Display;
            var scale = this.GetScaleFactor();
            var displays = new List<DisplayInfo>();

            try
            {
                var monitors = Xlib.XRRGetMonitors(display, this._connection.RootWindow, getActive: 1, out var count);
                if (monitors != null)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var monitor = monitors[i];
                        if (monitor.Width > 0 && monitor.Height > 0)
                        {
                            displays.Add(new DisplayInfo(new Rectangle(monitor.X, monitor.Y, monitor.X + monitor.Width, monitor.Y + monitor.Height), monitor.Primary != 0, scale, (nint)monitor.Name));
                        }
                    }

                    Xlib.XRRFreeMonitors(monitors);
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // RandR is not installed or too old: the whole screen is reported as a single monitor below.
            }

            if (displays.Count == 0)
            {
                var width = Xlib.XDisplayWidth(display, this._connection.Screen);
                var height = Xlib.XDisplayHeight(display, this._connection.Screen);
                displays.Add(new DisplayInfo(new Rectangle(0, 0, width, height), IsPrimary: true, scale, Handle: 0));
            }

            return displays;
        }
    }

    public PixelBuffer Capture(DisplayInfo display)
    {
        var bounds = display.Bounds;
        var buffer = PixelBuffer.Rent(bounds.Width, bounds.Height);

        try
        {
            lock (this._connection.Gate)
            {
                if (!this.TryCaptureWithSharedMemory(bounds, buffer))
                {
                    this.CaptureWithCoreProtocol(bounds, buffer);
                }
            }

            PixelOperations.MakeOpaque(buffer.Span);
            return buffer;
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        lock (this._connection.Gate)
        {
            this.ReleaseSharedImage();
        }
    }

    private float GetScaleFactor()
    {
        // Desktop environments publish their scale as a font resolution in the X resources.
        var value = Xlib.XGetDefault(this._connection.Display, "Xft", "dpi");
        if (value != null && float.TryParse(Marshal.PtrToStringUTF8((nint)value), NumberStyles.Float, CultureInfo.InvariantCulture, out var dpi) && dpi >= StandardDpi / 2 && dpi <= StandardDpi * 8)
        {
            return dpi / StandardDpi;
        }

        return 1f;
    }

    private bool TryCaptureWithSharedMemory(Rectangle bounds, PixelBuffer destination)
    {
        if (!this._sharedMemoryAvailable)
        {
            return false;
        }

        if (this._sharedImage == null || this._sharedImage->Width != bounds.Width || this._sharedImage->Height != bounds.Height)
        {
            this.ReleaseSharedImage();
            if (!this.TryCreateSharedImage(bounds.Width, bounds.Height))
            {
                // The server most likely runs on another machine: do not try again.
                this._sharedMemoryAvailable = false;
                return false;
            }
        }

        this._connection.ClearError();
        var succeeded = Xlib.XShmGetImage(this._connection.Display, this._connection.RootWindow, this._sharedImage, bounds.Left, bounds.Top, Xlib.AllPlanes) != 0;
        if (!succeeded || this._connection.GetError() != 0)
        {
            return false;
        }

        CopyImage(this._sharedImage, destination);
        return true;
    }

    private bool TryCreateSharedImage(int width, int height)
    {
        var display = this._connection.Display;
        var segment = (Xlib.XShmSegmentInfo*)NativeMemory.AllocZeroed((nuint)sizeof(Xlib.XShmSegmentInfo));
        Xlib.XImage* image = null;
        var attached = false;

        try
        {
            var visual = Xlib.XDefaultVisual(display, this._connection.Screen);
            var depth = (uint)Xlib.XDefaultDepth(display, this._connection.Screen);

            image = Xlib.XShmCreateImage(display, visual, depth, Xlib.ZPixmap, null, segment, (uint)width, (uint)height);
            if (image == null)
            {
                return false;
            }

            segment->Id = Xlib.shmget(Xlib.IpcPrivate, (nuint)image->BytesPerLine * (nuint)image->Height, Xlib.IpcCreate | 0x180);
            if (segment->Id < 0)
            {
                return false;
            }

            segment->Address = (byte*)Xlib.shmat(segment->Id, null, 0);

            // Mark the segment for removal right away: it is then released when the last process detaches from it, even after a crash.
            _ = Xlib.shmctl(segment->Id, Xlib.IpcRemoveId, null);

            if ((nint)segment->Address == -1)
            {
                segment->Address = null;
                return false;
            }

            image->Data = segment->Address;
            segment->ReadOnly = 0;

            this._connection.ClearError();
            attached = Xlib.XShmAttach(display, segment) != 0 && this._connection.GetError() == 0;
            if (!attached)
            {
                return false;
            }

            this._sharedImage = image;
            this._sharedSegment = segment;
            return true;
        }
        finally
        {
            if (!attached)
            {
                if (segment->Address != null)
                {
                    _ = Xlib.shmdt(segment->Address);
                }

                if (image != null)
                {
                    image->Data = null;
                    _ = image->DestroyImage(image);
                }

                NativeMemory.Free(segment);
            }
        }
    }

    private void ReleaseSharedImage()
    {
        if (this._sharedImage == null)
        {
            return;
        }

        try
        {
            _ = Xlib.XShmDetach(this._connection.Display, this._sharedSegment);
            _ = Xlib.XSync(this._connection.Display, 0);
        }
        catch (ObjectDisposedException)
        {
            // The connection is already closed, which detached the segment on the server side.
        }

        _ = Xlib.shmdt(this._sharedSegment->Address);

        // The pixels belong to the shared segment, not to the image.
        this._sharedImage->Data = null;
        _ = this._sharedImage->DestroyImage(this._sharedImage);
        NativeMemory.Free(this._sharedSegment);

        this._sharedImage = null;
        this._sharedSegment = null;
    }

    private void CaptureWithCoreProtocol(Rectangle bounds, PixelBuffer destination)
    {
        this._connection.ClearError();
        var image = Xlib.XGetImage(this._connection.Display, this._connection.RootWindow, bounds.Left, bounds.Top, (uint)bounds.Width, (uint)bounds.Height, Xlib.AllPlanes, Xlib.ZPixmap);
        var error = this._connection.GetError();

        if (image == null)
        {
            throw new InvalidOperationException(Messages.Platform_Throw_CaptureFailed.FormatInvariant("X11 error " + error.ToString(CultureInfo.InvariantCulture)));
        }

        try
        {
            CopyImage(image, destination);
        }
        finally
        {
            _ = image->DestroyImage(image);
        }
    }

    /// <summary>
    /// Copies an X image to a BGRA buffer. Practically every X server uses 32 bits per pixel in blue, green, red order,
    /// which is copied as is. Other layouts are converted pixel by pixel.
    /// </summary>
    private static void CopyImage(Xlib.XImage* image, PixelBuffer destination)
    {
        var width = destination.Width;
        var height = destination.Height;
        var isBgrx = image->BitsPerPixel == 32 && image->ByteOrder == Xlib.LsbFirst && image->RedMask == 0xFF0000 && image->GreenMask == 0xFF00 && image->BlueMask == 0xFF;

        if (isBgrx)
        {
            for (var y = 0; y < height; y++)
            {
                new ReadOnlySpan<byte>(image->Data + ((long)y * image->BytesPerLine), width * PixelBuffer.BytesPerPixel).CopyTo(destination.GetRow(y));
            }

            return;
        }

        var bytesPerPixel = image->BitsPerPixel / 8;
        if (bytesPerPixel is < 2 or > 4)
        {
            throw new NotSupportedException(Messages.Platform_Throw_CaptureFailed.FormatInvariant("Unsupported X11 pixel depth: " + image->BitsPerPixel.ToString(CultureInfo.InvariantCulture)));
        }

        var (redShift, redBits) = DescribeMask(image->RedMask);
        var (greenShift, greenBits) = DescribeMask(image->GreenMask);
        var (blueShift, blueBits) = DescribeMask(image->BlueMask);
        var mostSignificantFirst = image->ByteOrder != Xlib.LsbFirst;

        for (var y = 0; y < height; y++)
        {
            var source = image->Data + ((long)y * image->BytesPerLine);
            var row = destination.GetRow(y);

            for (var x = 0; x < width; x++, source += bytesPerPixel)
            {
                uint pixel = 0;
                for (var b = 0; b < bytesPerPixel; b++)
                {
                    pixel |= (uint)source[mostSignificantFirst ? bytesPerPixel - 1 - b : b] << (8 * b);
                }

                row[x * 4] = Expand(pixel, image->BlueMask, blueShift, blueBits);
                row[(x * 4) + 1] = Expand(pixel, image->GreenMask, greenShift, greenBits);
                row[(x * 4) + 2] = Expand(pixel, image->RedMask, redShift, redBits);
            }
        }
    }

    private static (int Shift, int Bits) DescribeMask(nuint mask)
    {
        return mask == 0 ? (0, 0) : (BitOperations.TrailingZeroCount(mask), BitOperations.PopCount(mask));
    }

    private static byte Expand(uint pixel, nuint mask, int shift, int bits)
    {
        if (bits == 0)
        {
            return 0;
        }

        var value = (pixel & (uint)mask) >> shift;
        return bits >= 8 ? (byte)(value >> (bits - 8)) : (byte)((value * 255) / ((1u << bits) - 1));
    }
}
