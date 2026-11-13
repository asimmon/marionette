using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Askaiser.Marionette.Platform.Linux;

/// <summary>
/// Bindings to the X11 client libraries. On 64-bit systems, the C types <c>long</c>, <c>Window</c>, <c>Atom</c> and <c>KeySym</c> are all 64 bits wide.
/// </summary>
[SupportedOSPlatform("linux")]
internal static unsafe partial class Xlib
{
    public const int ZPixmap = 2;
    public const int LsbFirst = 0;
    public const int IpcPrivate = 0;
    public const int IpcCreate = 0x200;
    public const int IpcRemoveId = 0;

    public static readonly nuint AllPlanes = nuint.MaxValue;

    private const string X11 = "libX11.so.6";
    private const string Xtst = "libXtst.so.6";
    private const string Xext = "libXext.so.6";
    private const string Xrandr = "libXrandr.so.2";
    private const string Libc = "libc";

    [LibraryImport(X11)]
    public static partial int XInitThreads();

    [LibraryImport(X11)]
    public static partial nint XOpenDisplay(byte* displayName);

    [LibraryImport(X11)]
    public static partial int XCloseDisplay(nint display);

    [LibraryImport(X11)]
    public static partial int XDefaultScreen(nint display);

    [LibraryImport(X11)]
    public static partial nuint XRootWindow(nint display, int screen);

    [LibraryImport(X11)]
    public static partial int XDisplayWidth(nint display, int screen);

    [LibraryImport(X11)]
    public static partial int XDisplayHeight(nint display, int screen);

    [LibraryImport(X11)]
    public static partial nint XDefaultVisual(nint display, int screen);

    [LibraryImport(X11)]
    public static partial int XDefaultDepth(nint display, int screen);

    [LibraryImport(X11)]
    public static partial int XFlush(nint display);

    [LibraryImport(X11)]
    public static partial int XSync(nint display, int discard);

    [LibraryImport(X11)]
    public static partial nint XSetErrorHandler(delegate* unmanaged[Cdecl]<nint, XErrorEvent*, int> handler);

    [LibraryImport(X11)]
    public static partial int XQueryPointer(nint display, nuint window, out nuint root, out nuint child, out int rootX, out int rootY, out int windowX, out int windowY, out uint mask);

    [LibraryImport(X11)]
    public static partial byte XKeysymToKeycode(nint display, nuint keysym);

    [LibraryImport(X11)]
    public static partial int XDisplayKeycodes(nint display, out int minKeycode, out int maxKeycode);

    [LibraryImport(X11)]
    public static partial nuint* XGetKeyboardMapping(nint display, byte firstKeycode, int keycodeCount, out int keysymsPerKeycode);

    [LibraryImport(X11)]
    public static partial int XChangeKeyboardMapping(nint display, int firstKeycode, int keysymsPerKeycode, nuint* keysyms, int keycodeCount);

    [LibraryImport(X11)]
    public static partial int XFree(void* data);

    [LibraryImport(X11)]
    public static partial XImage* XGetImage(nint display, nuint drawable, int x, int y, uint width, uint height, nuint planeMask, int format);

    [LibraryImport(X11, StringMarshalling = StringMarshalling.Utf8)]
    public static partial byte* XGetDefault(nint display, string program, string option);

    [LibraryImport(Xtst)]
    public static partial int XTestQueryExtension(nint display, out int eventBase, out int errorBase, out int majorVersion, out int minorVersion);

    [LibraryImport(Xtst)]
    public static partial int XTestFakeMotionEvent(nint display, int screen, int x, int y, nuint delay);

    [LibraryImport(Xtst)]
    public static partial int XTestFakeButtonEvent(nint display, uint button, int isPress, nuint delay);

    [LibraryImport(Xtst)]
    public static partial int XTestFakeKeyEvent(nint display, uint keycode, int isPress, nuint delay);

    [LibraryImport(Xext)]
    public static partial int XShmQueryExtension(nint display);

    [LibraryImport(Xext)]
    public static partial XImage* XShmCreateImage(nint display, nint visual, uint depth, int format, byte* data, XShmSegmentInfo* segment, uint width, uint height);

    [LibraryImport(Xext)]
    public static partial int XShmAttach(nint display, XShmSegmentInfo* segment);

    [LibraryImport(Xext)]
    public static partial int XShmDetach(nint display, XShmSegmentInfo* segment);

    [LibraryImport(Xext)]
    public static partial int XShmGetImage(nint display, nuint drawable, XImage* image, int x, int y, nuint planeMask);

    [LibraryImport(Xrandr)]
    public static partial XRRMonitorInfo* XRRGetMonitors(nint display, nuint window, int getActive, out int monitorCount);

    [LibraryImport(Xrandr)]
    public static partial void XRRFreeMonitors(XRRMonitorInfo* monitors);

    [LibraryImport(Libc, SetLastError = true)]
    public static partial int shmget(int key, nuint size, int flags);

    [LibraryImport(Libc, SetLastError = true)]
    public static partial void* shmat(int segmentId, void* address, int flags);

    [LibraryImport(Libc)]
    public static partial int shmdt(void* address);

    [LibraryImport(Libc)]
    public static partial int shmctl(int segmentId, int command, void* buffer);

    [StructLayout(LayoutKind.Sequential)]
    public struct XErrorEvent
    {
        public int Type;
        public nint Display;
        public nuint ResourceId;
        public nuint Serial;
        public byte ErrorCode;
        public byte RequestCode;
        public byte MinorCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XImage
    {
        public int Width;
        public int Height;
        public int XOffset;
        public int Format;
        public byte* Data;
        public int ByteOrder;
        public int BitmapUnit;
        public int BitmapBitOrder;
        public int BitmapPad;
        public int Depth;
        public int BytesPerLine;
        public int BitsPerPixel;
        public nuint RedMask;
        public nuint GreenMask;
        public nuint BlueMask;
        public nint ObjectData;
        public nint CreateImage;
        public delegate* unmanaged[Cdecl]<XImage*, int> DestroyImage;
        public nint GetPixel;
        public nint PutPixel;
        public nint SubImage;
        public nint AddPixel;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XShmSegmentInfo
    {
        public nuint Segment;
        public int Id;
        public byte* Address;
        public int ReadOnly;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XRRMonitorInfo
    {
        public nuint Name;
        public int Primary;
        public int Automatic;
        public int OutputCount;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int PhysicalWidth;
        public int PhysicalHeight;
        public nint Outputs;
    }
}
