using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading;
using Askaiser.Marionette.Imaging;

namespace Askaiser.Marionette.Platform.MacOS;

/// <summary>
/// Enumerates and captures monitors with Quartz Display Services.
/// </summary>
/// <remarks>
/// macOS positions windows and the cursor in points, while a capture is made of pixels: there are two pixels per point on
/// most Mac screens. Marionette works in pixels everywhere, so this class also converts between both systems. The pixel
/// bounds of a monitor are its bounds in points multiplied by its own scale factor.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed unsafe class MacDisplayDevice : IDisplayDevice
{
    private const int MaxDisplays = 32;

    private DisplayGeometry[] _geometries = [];
    private int _captureAccessRequested;

    public IReadOnlyList<DisplayInfo> GetDisplays()
    {
        var geometries = ReadGeometries();
        Volatile.Write(ref this._geometries, geometries);

        var displays = new List<DisplayInfo>(geometries.Length);
        foreach (var geometry in geometries)
        {
            displays.Add(new DisplayInfo(geometry.Pixels, geometry.IsMain, (float)geometry.Scale, (nint)geometry.Id));
        }

        return displays;
    }

    public PixelBuffer Capture(DisplayInfo display)
    {
        var image = CreateImage((uint)display.Handle);
        if (image == 0)
        {
            // Asking for access makes macOS show its permission dialog the first time.
            if (Interlocked.Exchange(ref this._captureAccessRequested, 1) == 0)
            {
                try
                {
                    _ = CoreGraphics.CGRequestScreenCaptureAccess();
                }
                catch (EntryPointNotFoundException)
                {
                    // Screen recording permissions do not exist before macOS 10.15.
                }
            }

            throw new InvalidOperationException(Messages.Platform_Throw_CaptureFailed.FormatInvariant(Messages.Platform_MacOS_ScreenRecordingHint));
        }

        try
        {
            var buffer = PixelBuffer.Rent(display.Bounds.Width, display.Bounds.Height);
            try
            {
                CopyImage(image, buffer);
                PixelOperations.MakeOpaque(buffer.Span);
                return buffer;
            }
            catch
            {
                buffer.Dispose();
                throw;
            }
        }
        finally
        {
            CoreGraphics.CGImageRelease(image);
        }
    }

    /// <summary>
    /// Converts a position in pixels to the points macOS expects for mouse events.
    /// </summary>
    public CoreGraphics.CGPoint ToPoints(int x, int y)
    {
        var geometries = this.GetGeometries();
        var geometry = geometries[0];

        foreach (var candidate in geometries)
        {
            if (x >= candidate.Pixels.Left && x < candidate.Pixels.Right && y >= candidate.Pixels.Top && y < candidate.Pixels.Bottom)
            {
                geometry = candidate;
                break;
            }
        }

        return new CoreGraphics.CGPoint
        {
            X = geometry.Points.X + ((x - geometry.Pixels.Left) / geometry.Scale),
            Y = geometry.Points.Y + ((y - geometry.Pixels.Top) / geometry.Scale),
        };
    }

    /// <summary>
    /// Converts a position in points, as reported by macOS, to pixels.
    /// </summary>
    public Point ToPixels(CoreGraphics.CGPoint point)
    {
        var geometries = this.GetGeometries();
        var geometry = geometries[0];

        foreach (var candidate in geometries)
        {
            var points = candidate.Points;
            if (point.X >= points.X && point.X < points.X + points.Width && point.Y >= points.Y && point.Y < points.Y + points.Height)
            {
                geometry = candidate;
                break;
            }
        }

        return new Point(
            geometry.Pixels.Left + (int)Math.Round((point.X - geometry.Points.X) * geometry.Scale),
            geometry.Pixels.Top + (int)Math.Round((point.Y - geometry.Points.Y) * geometry.Scale));
    }

    public void Dispose()
    {
    }

    private static DisplayGeometry[] ReadGeometries()
    {
        var ids = stackalloc uint[MaxDisplays];
        if (CoreGraphics.CGGetActiveDisplayList(MaxDisplays, ids, out var count) != 0 || count == 0)
        {
            ids[0] = CoreGraphics.CGMainDisplayID();
            count = 1;
        }

        var mainId = CoreGraphics.CGMainDisplayID();
        var geometries = new DisplayGeometry[count];

        for (var i = 0; i < count; i++)
        {
            var points = CoreGraphics.CGDisplayBounds(ids[i]);
            var scale = 1.0;

            var mode = CoreGraphics.CGDisplayCopyDisplayMode(ids[i]);
            if (mode != 0)
            {
                var pixelWidth = (double)CoreGraphics.CGDisplayModeGetPixelWidth(mode);
                var pointWidth = (double)CoreGraphics.CGDisplayModeGetWidth(mode);
                if (pixelWidth > 0 && pointWidth > 0)
                {
                    scale = pixelWidth / pointWidth;
                }

                CoreGraphics.CGDisplayModeRelease(mode);
            }

            var left = (int)Math.Round(points.X * scale);
            var top = (int)Math.Round(points.Y * scale);
            var width = Math.Max(1, (int)Math.Round(points.Width * scale));
            var height = Math.Max(1, (int)Math.Round(points.Height * scale));

            geometries[i] = new DisplayGeometry(ids[i], ids[i] == mainId, points, new Rectangle(left, top, left + width, top + height), scale);
        }

        return geometries;
    }

    private static nint CreateImage(uint displayId)
    {
        try
        {
            return CoreGraphics.CGDisplayCreateImage(displayId);
        }
        catch (EntryPointNotFoundException)
        {
            // Fall back to the window server API, which composes the same image.
            const uint onScreenOnly = 1;
            return CoreGraphics.CGWindowListCreateImage(CoreGraphics.CGDisplayBounds(displayId), onScreenOnly, 0, 0);
        }
    }

    /// <summary>
    /// Copies a Quartz image to a BGRA buffer. Screen captures normally already use that layout and are copied as is.
    /// Images in another layout, such as those of screens with 10 bits per color, are converted by drawing them.
    /// </summary>
    private static void CopyImage(nint image, PixelBuffer destination)
    {
        var width = (int)CoreGraphics.CGImageGetWidth(image);
        var height = (int)CoreGraphics.CGImageGetHeight(image);
        var bitmapInfo = CoreGraphics.CGImageGetBitmapInfo(image);

        var alphaInfo = bitmapInfo & CoreGraphics.BitmapAlphaInfoMask;
        var isBgra = CoreGraphics.CGImageGetBitsPerPixel(image) == 32
            && (bitmapInfo & CoreGraphics.BitmapByteOrderMask) == CoreGraphics.BitmapByteOrder32Little
            && (bitmapInfo & CoreGraphics.BitmapPixelFormatMask) == 0
            && alphaInfo is 2 or 4 or 6;

        if (isBgra && width == destination.Width && height == destination.Height)
        {
            var data = CoreGraphics.CGDataProviderCopyData(CoreGraphics.CGImageGetDataProvider(image));
            if (data != 0)
            {
                try
                {
                    var bytesPerRow = (long)CoreGraphics.CGImageGetBytesPerRow(image);
                    var source = CoreGraphics.CFDataGetBytePtr(data);
                    if (source != null && CoreGraphics.CFDataGetLength(data) >= ((height - 1) * bytesPerRow) + destination.Stride)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            new ReadOnlySpan<byte>(source + (y * bytesPerRow), destination.Stride).CopyTo(destination.GetRow(y));
                        }

                        return;
                    }
                }
                finally
                {
                    CoreGraphics.CFRelease(data);
                }
            }
        }

        destination.Span.Clear();
        var colorSpace = CoreGraphics.CGColorSpaceCreateDeviceRGB();
        try
        {
            fixed (byte* pixels = destination.Span)
            {
                var context = CoreGraphics.CGBitmapContextCreate(pixels, (nuint)destination.Width, (nuint)destination.Height, 8, (nuint)destination.Stride, colorSpace, CoreGraphics.BitmapByteOrder32Little | CoreGraphics.BitmapPremultipliedFirst);
                if (context == 0)
                {
                    throw new InvalidOperationException(Messages.Platform_Throw_CaptureFailed.FormatInvariant("Unsupported image layout: 0x" + bitmapInfo.ToString("X", CultureInfo.InvariantCulture)));
                }

                CoreGraphics.CGContextDrawImage(context, new CoreGraphics.CGRect { Width = destination.Width, Height = destination.Height }, image);
                CoreGraphics.CGContextRelease(context);
            }
        }
        finally
        {
            CoreGraphics.CGColorSpaceRelease(colorSpace);
        }
    }

    private DisplayGeometry[] GetGeometries()
    {
        var geometries = Volatile.Read(ref this._geometries);
        if (geometries.Length == 0)
        {
            geometries = ReadGeometries();
            Volatile.Write(ref this._geometries, geometries);
        }

        return geometries;
    }

    private readonly record struct DisplayGeometry(uint Id, bool IsMain, CoreGraphics.CGRect Points, Rectangle Pixels, double Scale);
}
