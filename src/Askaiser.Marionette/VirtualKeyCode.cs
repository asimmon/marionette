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
    /// <summary>The Backspace key, which deletes the character before the cursor. It is the Delete key of Mac keyboards.</summary>
    Backspace = 0x08,

    /// <summary>The Tab key.</summary>
    Tab = 0x09,

    /// <summary>The Clear key, which is the key 5 of the numeric keypad when Num Lock is off.</summary>
    Clear = 0x0C,

    /// <summary>The Enter key of the main part of the keyboard. It is the Return key of Mac keyboards.</summary>
    Enter = 0x0D,

    /// <summary>The Shift key, without distinguishing the left one from the right one. The left one is simulated.</summary>
    Shift = 0x10,

    /// <summary>The Control key, without distinguishing the left one from the right one. The left one is simulated.</summary>
    Control = 0x11,

    /// <summary>The Alt key (Option on macOS), without distinguishing the left one from the right one. The left one is simulated.</summary>
    Alt = 0x12,

    /// <summary>The Pause key. It is the F15 key on macOS.</summary>
    Pause = 0x13,

    /// <summary>The Caps Lock key.</summary>
    Caps = 0x14,

    /// <summary>The Escape key.</summary>
    Escape = 0x1B,

    /// <summary>The space bar.</summary>
    Space = 0x20,

    /// <summary>The Page Up key.</summary>
    PageUp = 0x21,

    /// <summary>The Page Down key.</summary>
    PageDown = 0x22,

    /// <summary>The End key.</summary>
    End = 0x23,

    /// <summary>The Home key.</summary>
    Home = 0x24,

    /// <summary>The left arrow key.</summary>
    LeftArrow = 0x25,

    /// <summary>The up arrow key.</summary>
    UpArrow = 0x26,

    /// <summary>The right arrow key.</summary>
    RightArrow = 0x27,

    /// <summary>The down arrow key.</summary>
    DownArrow = 0x28,

    /// <summary>The Print Screen key. It is the F13 key on macOS.</summary>
    PrintScreen = 0x2C,

    /// <summary>The Insert key. It is the Help key on macOS.</summary>
    Insert = 0x2D,

    /// <summary>The Delete key, which deletes the character after the cursor.</summary>
    Delete = 0x2E,

    /// <summary>The Help key.</summary>
    Help = 0x2F,

    /// <summary>The 0 key of the main part of the keyboard.</summary>
    Digit0 = 0x30,

    /// <summary>The 1 key of the main part of the keyboard.</summary>
    Digit1 = 0x31,

    /// <summary>The 2 key of the main part of the keyboard.</summary>
    Digit2 = 0x32,

    /// <summary>The 3 key of the main part of the keyboard.</summary>
    Digit3 = 0x33,

    /// <summary>The 4 key of the main part of the keyboard.</summary>
    Digit4 = 0x34,

    /// <summary>The 5 key of the main part of the keyboard.</summary>
    Digit5 = 0x35,

    /// <summary>The 6 key of the main part of the keyboard.</summary>
    Digit6 = 0x36,

    /// <summary>The 7 key of the main part of the keyboard.</summary>
    Digit7 = 0x37,

    /// <summary>The 8 key of the main part of the keyboard.</summary>
    Digit8 = 0x38,

    /// <summary>The 9 key of the main part of the keyboard.</summary>
    Digit9 = 0x39,

    /// <summary>The A key.</summary>
    A = 0x41,

    /// <summary>The B key.</summary>
    B = 0x42,

    /// <summary>The C key.</summary>
    C = 0x43,

    /// <summary>The D key.</summary>
    D = 0x44,

    /// <summary>The E key.</summary>
    E = 0x45,

    /// <summary>The F key.</summary>
    F = 0x46,

    /// <summary>The G key.</summary>
    G = 0x47,

    /// <summary>The H key.</summary>
    H = 0x48,

    /// <summary>The I key.</summary>
    I = 0x49,

    /// <summary>The J key.</summary>
    J = 0x4A,

    /// <summary>The K key.</summary>
    K = 0x4B,

    /// <summary>The L key.</summary>
    L = 0x4C,

    /// <summary>The M key.</summary>
    M = 0x4D,

    /// <summary>The N key.</summary>
    N = 0x4E,

    /// <summary>The O key.</summary>
    O = 0x4F,

    /// <summary>The P key.</summary>
    P = 0x50,

    /// <summary>The Q key.</summary>
    Q = 0x51,

    /// <summary>The R key.</summary>
    R = 0x52,

    /// <summary>The S key.</summary>
    S = 0x53,

    /// <summary>The T key.</summary>
    T = 0x54,

    /// <summary>The U key.</summary>
    U = 0x55,

    /// <summary>The V key.</summary>
    V = 0x56,

    /// <summary>The W key.</summary>
    W = 0x57,

    /// <summary>The X key.</summary>
    X = 0x58,

    /// <summary>The Y key.</summary>
    Y = 0x59,

    /// <summary>The Z key.</summary>
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

    /// <summary>The 0 key of the numeric keypad.</summary>
    Numpad0 = 0x60,

    /// <summary>The 1 key of the numeric keypad.</summary>
    Numpad1 = 0x61,

    /// <summary>The 2 key of the numeric keypad.</summary>
    Numpad2 = 0x62,

    /// <summary>The 3 key of the numeric keypad.</summary>
    Numpad3 = 0x63,

    /// <summary>The 4 key of the numeric keypad.</summary>
    Numpad4 = 0x64,

    /// <summary>The 5 key of the numeric keypad.</summary>
    Numpad5 = 0x65,

    /// <summary>The 6 key of the numeric keypad.</summary>
    Numpad6 = 0x66,

    /// <summary>The 7 key of the numeric keypad.</summary>
    Numpad7 = 0x67,

    /// <summary>The 8 key of the numeric keypad.</summary>
    Numpad8 = 0x68,

    /// <summary>The 9 key of the numeric keypad.</summary>
    Numpad9 = 0x69,

    /// <summary>The <c>*</c> key of the numeric keypad.</summary>
    Multiply = 0x6A,

    /// <summary>The <c>+</c> key of the numeric keypad.</summary>
    Add = 0x6B,

    /// <summary>The separator key of the numeric keypad, which only some keyboards have.</summary>
    Separator = 0x6C,

    /// <summary>The <c>-</c> key of the numeric keypad.</summary>
    Subtract = 0x6D,

    /// <summary>The decimal point key of the numeric keypad.</summary>
    Decimal = 0x6E,

    /// <summary>The <c>/</c> key of the numeric keypad.</summary>
    Divide = 0x6F,

    /// <summary>The F1 key.</summary>
    F1 = 0x70,

    /// <summary>The F2 key.</summary>
    F2 = 0x71,

    /// <summary>The F3 key.</summary>
    F3 = 0x72,

    /// <summary>The F4 key.</summary>
    F4 = 0x73,

    /// <summary>The F5 key.</summary>
    F5 = 0x74,

    /// <summary>The F6 key.</summary>
    F6 = 0x75,

    /// <summary>The F7 key.</summary>
    F7 = 0x76,

    /// <summary>The F8 key.</summary>
    F8 = 0x77,

    /// <summary>The F9 key.</summary>
    F9 = 0x78,

    /// <summary>The F10 key.</summary>
    F10 = 0x79,

    /// <summary>The F11 key.</summary>
    F11 = 0x7A,

    /// <summary>The F12 key.</summary>
    F12 = 0x7B,

    /// <summary>The F13 key.</summary>
    F13 = 0x7C,

    /// <summary>The F14 key.</summary>
    F14 = 0x7D,

    /// <summary>The F15 key.</summary>
    F15 = 0x7E,

    /// <summary>The F16 key.</summary>
    F16 = 0x7F,

    /// <summary>The F17 key.</summary>
    F17 = 0x80,

    /// <summary>The F18 key.</summary>
    F18 = 0x81,

    /// <summary>The F19 key.</summary>
    F19 = 0x82,

    /// <summary>The F20 key.</summary>
    F20 = 0x83,

    /// <summary>The F21 key. It cannot be simulated on macOS.</summary>
    F21 = 0x84,

    /// <summary>The F22 key. It cannot be simulated on macOS.</summary>
    F22 = 0x85,

    /// <summary>The F23 key. It cannot be simulated on macOS.</summary>
    F23 = 0x86,

    /// <summary>The F24 key. It cannot be simulated on macOS.</summary>
    F24 = 0x87,

    /// <summary>The Num Lock key. It is the Clear key of the numeric keypad on macOS.</summary>
    NumLock = 0x90,

    /// <summary>The Scroll Lock key. It is the F14 key on macOS.</summary>
    ScrollLock = 0x91,

    /// <summary>The left Shift key.</summary>
    LeftShift = 0xA0,

    /// <summary>The right Shift key.</summary>
    RightShift = 0xA1,

    /// <summary>The left Control key.</summary>
    LeftControl = 0xA2,

    /// <summary>The right Control key.</summary>
    RightControl = 0xA3,

    /// <summary>The left Alt key. It is the left Option key on macOS.</summary>
    LeftAlt = 0xA4,

    /// <summary>The right Alt key. It is the right Option key on macOS.</summary>
    RightAlt = 0xA5,

    /// <summary>The key that goes back to the previous page in a web browser. It cannot be simulated on macOS.</summary>
    BrowserBack = 0xA6,

    /// <summary>The key that goes forward to the next page in a web browser. It cannot be simulated on macOS.</summary>
    BrowserForward = 0xA7,

    /// <summary>The key that reloads the page in a web browser. It cannot be simulated on macOS.</summary>
    BrowserRefresh = 0xA8,

    /// <summary>The key that stops the loading of the page in a web browser. It cannot be simulated on macOS.</summary>
    BrowserStop = 0xA9,

    /// <summary>The key that opens the search of a web browser. It cannot be simulated on macOS.</summary>
    BrowserSearch = 0xAA,

    /// <summary>The key that opens the favorites of a web browser. It cannot be simulated on macOS.</summary>
    BrowserFavorites = 0xAB,

    /// <summary>The key that opens the home page of a web browser. It cannot be simulated on macOS.</summary>
    BrowserHome = 0xAC,

    /// <summary>The key that mutes the sound.</summary>
    VolumeMute = 0xAD,

    /// <summary>The key that turns the volume down.</summary>
    VolumeDown = 0xAE,

    /// <summary>The key that turns the volume up.</summary>
    VolumeUp = 0xAF,

    /// <summary>The key that skips to the next media track. It cannot be simulated on macOS.</summary>
    MediaNextTrack = 0xB0,

    /// <summary>The key that goes back to the previous media track. It cannot be simulated on macOS.</summary>
    MediaPrevTrack = 0xB1,

    /// <summary>The key that stops media playback. It cannot be simulated on macOS.</summary>
    MediaStop = 0xB2,

    /// <summary>The key that starts or pauses media playback. It cannot be simulated on macOS.</summary>
    MediaPlayPause = 0xB3,

    /// <summary>The key that opens the mail application. It cannot be simulated on macOS.</summary>
    LaunchMail = 0xB4,

    /// <summary>The key that opens the media player. It cannot be simulated on macOS.</summary>
    LaunchMediaSelect = 0xB5,

    /// <summary>The first key that opens an application chosen by the user or the keyboard manufacturer. It cannot be simulated on macOS.</summary>
    LaunchApplication1 = 0xB6,

    /// <summary>The second key that opens an application chosen by the user or the keyboard manufacturer. It cannot be simulated on macOS.</summary>
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
