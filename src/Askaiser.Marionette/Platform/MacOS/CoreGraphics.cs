using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Askaiser.Marionette.Platform.MacOS;

/// <summary>
/// Bindings to the Quartz display and event services of macOS.
/// </summary>
[SupportedOSPlatform("macos")]
internal static unsafe partial class CoreGraphics
{
    public const uint HidEventTap = 0;

    public const uint EventLeftMouseDown = 1;
    public const uint EventLeftMouseUp = 2;
    public const uint EventRightMouseDown = 3;
    public const uint EventRightMouseUp = 4;
    public const uint EventMouseMoved = 5;
    public const uint EventLeftMouseDragged = 6;
    public const uint EventRightMouseDragged = 7;
    public const uint EventOtherMouseDown = 25;
    public const uint EventOtherMouseUp = 26;
    public const uint EventOtherMouseDragged = 27;

    public const uint MouseButtonLeft = 0;
    public const uint MouseButtonRight = 1;
    public const uint MouseButtonCenter = 2;

    public const uint MouseEventClickState = 1;
    public const uint ScrollEventUnitLine = 1;

    public const ulong EventFlagShift = 0x00020000;
    public const ulong EventFlagControl = 0x00040000;
    public const ulong EventFlagAlternate = 0x00080000;
    public const ulong EventFlagCommand = 0x00100000;

    public const uint BitmapAlphaInfoMask = 0x1F;
    public const uint BitmapByteOrderMask = 0x7000;
    public const uint BitmapByteOrder32Little = 0x2000;
    public const uint BitmapPixelFormatMask = 0xF0000;
    public const uint BitmapPremultipliedFirst = 2;

    private const string CoreGraphicsLibrary = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial uint CGMainDisplayID();

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial int CGGetActiveDisplayList(uint maxDisplays, uint* activeDisplays, out uint displayCount);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial CGRect CGDisplayBounds(uint display);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGDisplayCopyDisplayMode(uint display);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nuint CGDisplayModeGetPixelWidth(nint mode);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nuint CGDisplayModeGetWidth(nint mode);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial void CGDisplayModeRelease(nint mode);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGDisplayCreateImage(uint display);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGWindowListCreateImage(CGRect screenBounds, uint listOption, uint windowId, uint imageOption);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nuint CGImageGetWidth(nint image);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nuint CGImageGetHeight(nint image);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nuint CGImageGetBytesPerRow(nint image);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nuint CGImageGetBitsPerPixel(nint image);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial uint CGImageGetBitmapInfo(nint image);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGImageGetDataProvider(nint image);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGDataProviderCopyData(nint provider);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial void CGImageRelease(nint image);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGColorSpaceCreateDeviceRGB();

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial void CGColorSpaceRelease(nint colorSpace);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGBitmapContextCreate(void* data, nuint width, nuint height, nuint bitsPerComponent, nuint bytesPerRow, nint colorSpace, uint bitmapInfo);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial void CGContextDrawImage(nint context, CGRect rectangle, nint image);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial void CGContextRelease(nint context);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial byte CGPreflightScreenCaptureAccess();

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial byte CGRequestScreenCaptureAccess();

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial byte CGPreflightPostEventAccess();

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial byte CGRequestPostEventAccess();

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGEventCreate(nint source);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial CGPoint CGEventGetLocation(nint cgEvent);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGEventCreateMouseEvent(nint source, uint mouseType, CGPoint position, uint mouseButton);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGEventCreateKeyboardEvent(nint source, ushort virtualKey, byte keyDown);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial nint CGEventCreateScrollWheelEvent2(nint source, uint units, uint wheelCount, int wheel1, int wheel2, int wheel3);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial void CGEventSetIntegerValueField(nint cgEvent, uint field, long value);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial void CGEventSetFlags(nint cgEvent, ulong flags);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial void CGEventKeyboardSetUnicodeString(nint cgEvent, nuint length, ushort* characters);

    [LibraryImport(CoreGraphicsLibrary)]
    public static partial void CGEventPost(uint tap, nint cgEvent);

    [LibraryImport(CoreFoundationLibrary)]
    public static partial byte* CFDataGetBytePtr(nint data);

    [LibraryImport(CoreFoundationLibrary)]
    public static partial nint CFDataGetLength(nint data);

    [LibraryImport(CoreFoundationLibrary)]
    public static partial void CFRelease(nint cfObject);

    [StructLayout(LayoutKind.Sequential)]
    public struct CGPoint
    {
        public double X;
        public double Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CGRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
    }
}
