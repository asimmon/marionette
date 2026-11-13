namespace Askaiser.Marionette;

/// <summary>
/// The keys of a keyboard that can be simulated on Windows, Linux and macOS.
/// </summary>
/// <remarks>
/// <para>
/// Values are those of the Windows virtual-key codes, but a key designates the same physical or logical key on every platform:
/// <see cref="LeftWindows"/> is the Windows key on Windows, the Super key on Linux and the Command key on macOS.
/// </para>
/// <para>
/// Use <see cref="ControlOrCommand"/> for shortcuts that use Control on Windows and Linux but Command on macOS, such as copy and paste.
/// </para>
/// <para>
/// A few keys do not exist everywhere: browser and application launcher keys, as well as media track keys, cannot be simulated on macOS.
/// Simulating them throws a <see cref="System.PlatformNotSupportedException"/>.
/// </para>
/// </remarks>
#pragma warning disable CA1008 // Enums should have zero value: there is no key with a zero code
#pragma warning disable CA1069 // Enums values should not be duplicated: some keys have a different name depending on the platform
#pragma warning disable CA1720 // Identifier contains type name: Decimal is the name of the key
public enum VirtualKeyCode
{
    Backspace = 0x08,
    Tab = 0x09,

    /// <summary>The Clear key, which is the key 5 of the numeric keypad when Num Lock is off.</summary>
    Clear = 0x0C,
    Enter = 0x0D,

    /// <summary>The Shift key, without distinguishing the left one from the right one. The left one is simulated.</summary>
    Shift = 0x10,

    /// <summary>The Control key, without distinguishing the left one from the right one. The left one is simulated.</summary>
    Control = 0x11,

    /// <summary>The Alt key (Option on macOS), without distinguishing the left one from the right one. The left one is simulated.</summary>
    Alt = 0x12,
    Pause = 0x13,

    /// <summary>The Caps Lock key.</summary>
    Caps = 0x14,
    Escape = 0x1B,
    Space = 0x20,
    PageUp = 0x21,
    PageDown = 0x22,
    End = 0x23,
    Home = 0x24,
    LeftArrow = 0x25,
    UpArrow = 0x26,
    RightArrow = 0x27,
    DownArrow = 0x28,
    PrintScreen = 0x2C,
    Insert = 0x2D,

    /// <summary>The Delete key, which deletes the character after the cursor.</summary>
    Delete = 0x2E,
    Help = 0x2F,

    Digit0 = 0x30,
    Digit1 = 0x31,
    Digit2 = 0x32,
    Digit3 = 0x33,
    Digit4 = 0x34,
    Digit5 = 0x35,
    Digit6 = 0x36,
    Digit7 = 0x37,
    Digit8 = 0x38,
    Digit9 = 0x39,

    A = 0x41,
    B = 0x42,
    C = 0x43,
    D = 0x44,
    E = 0x45,
    F = 0x46,
    G = 0x47,
    H = 0x48,
    I = 0x49,
    J = 0x4A,
    K = 0x4B,
    L = 0x4C,
    M = 0x4D,
    N = 0x4E,
    O = 0x4F,
    P = 0x50,
    Q = 0x51,
    R = 0x52,
    S = 0x53,
    T = 0x54,
    U = 0x55,
    V = 0x56,
    W = 0x57,
    X = 0x58,
    Y = 0x59,
    Z = 0x5A,

    /// <summary>The left Windows key. It is the left Super key on Linux and the left Command key on macOS.</summary>
    LeftWindows = 0x5B,

    /// <summary>The left Command key of macOS. Same key as <see cref="LeftWindows"/>.</summary>
    LeftCommand = LeftWindows,

    /// <summary>The right Windows key. It is the right Super key on Linux and the right Command key on macOS.</summary>
    RightWindows = 0x5C,

    /// <summary>The right Command key of macOS. Same key as <see cref="RightWindows"/>.</summary>
    RightCommand = RightWindows,

    /// <summary>The Applications key, which opens a context menu. It does not exist on macOS.</summary>
    Applications = 0x5D,

    Numpad0 = 0x60,
    Numpad1 = 0x61,
    Numpad2 = 0x62,
    Numpad3 = 0x63,
    Numpad4 = 0x64,
    Numpad5 = 0x65,
    Numpad6 = 0x66,
    Numpad7 = 0x67,
    Numpad8 = 0x68,
    Numpad9 = 0x69,
    Multiply = 0x6A,
    Add = 0x6B,
    Separator = 0x6C,
    Subtract = 0x6D,
    Decimal = 0x6E,
    Divide = 0x6F,

    F1 = 0x70,
    F2 = 0x71,
    F3 = 0x72,
    F4 = 0x73,
    F5 = 0x74,
    F6 = 0x75,
    F7 = 0x76,
    F8 = 0x77,
    F9 = 0x78,
    F10 = 0x79,
    F11 = 0x7A,
    F12 = 0x7B,
    F13 = 0x7C,
    F14 = 0x7D,
    F15 = 0x7E,
    F16 = 0x7F,
    F17 = 0x80,
    F18 = 0x81,
    F19 = 0x82,
    F20 = 0x83,
    F21 = 0x84,
    F22 = 0x85,
    F23 = 0x86,
    F24 = 0x87,

    NumLock = 0x90,
    ScrollLock = 0x91,

    LeftShift = 0xA0,
    RightShift = 0xA1,
    LeftControl = 0xA2,
    RightControl = 0xA3,

    /// <summary>The left Alt key. It is the left Option key on macOS.</summary>
    LeftAlt = 0xA4,

    /// <summary>The right Alt key. It is the right Option key on macOS.</summary>
    RightAlt = 0xA5,

    BrowserBack = 0xA6,
    BrowserForward = 0xA7,
    BrowserRefresh = 0xA8,
    BrowserStop = 0xA9,
    BrowserSearch = 0xAA,
    BrowserFavorites = 0xAB,
    BrowserHome = 0xAC,

    VolumeMute = 0xAD,
    VolumeDown = 0xAE,
    VolumeUp = 0xAF,
    MediaNextTrack = 0xB0,
    MediaPrevTrack = 0xB1,
    MediaStop = 0xB2,
    MediaPlayPause = 0xB3,

    LaunchMail = 0xB4,
    LaunchMediaSelect = 0xB5,
    LaunchApplication1 = 0xB6,
    LaunchApplication2 = 0xB7,

    /// <summary>The <c>;:</c> key of a US keyboard.</summary>
    Semicolon = 0xBA,

    /// <summary>The <c>=+</c> key of a US keyboard.</summary>
    Plus = 0xBB,

    /// <summary>The <c>,&lt;</c> key of a US keyboard.</summary>
    Comma = 0xBC,

    /// <summary>The <c>-_</c> key of a US keyboard.</summary>
    Minus = 0xBD,

    /// <summary>The <c>.&gt;</c> key of a US keyboard.</summary>
    Period = 0xBE,

    /// <summary>The <c>/?</c> key of a US keyboard.</summary>
    Slash = 0xBF,

    /// <summary>The <c>`~</c> key of a US keyboard.</summary>
    Tilde = 0xC0,

    /// <summary>The <c>[{</c> key of a US keyboard.</summary>
    LeftBrace = 0xDB,

    /// <summary>The <c>\|</c> key of a US keyboard.</summary>
    Backslash = 0xDC,

    /// <summary>The <c>]}</c> key of a US keyboard.</summary>
    RightBrace = 0xDD,

    /// <summary>The <c>'"</c> key of a US keyboard.</summary>
    SingleQuote = 0xDE,

    /// <summary>
    /// The main modifier key of keyboard shortcuts on the current platform: Command on macOS, Control on Windows and Linux.
    /// </summary>
    ControlOrCommand = 0x1000,
}
