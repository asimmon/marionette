using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace Askaiser.Marionette.Platform.Linux;

/// <summary>
/// Simulates a keyboard with the XTEST extension.
/// </summary>
/// <remarks>
/// X11 input events carry key codes, the meaning of which depends on the keyboard layout. A character is typed by pressing the
/// key that produces it in the current layout, with Shift when needed. Characters that the layout cannot produce are typed
/// by temporarily assigning them to a key code that the layout does not use.
/// </remarks>
[SupportedOSPlatform("linux")]
internal sealed unsafe class X11KeyboardDevice : IKeyboardDevice
{
    private const nuint ShiftKeysym = 0xFFE1;
    private const nuint UnicodeKeysymFlag = 0x01000000;

    /// <summary>Applications need a moment to take a change of the keyboard mapping into account.</summary>
    private static readonly TimeSpan MappingChangeDelay = TimeSpan.FromMilliseconds(15);

    private readonly X11Connection _connection;
    private readonly Dictionary<VirtualKeyCode, byte> _temporaryKeycodes = [];

    public X11KeyboardDevice(X11Connection connection)
    {
        this._connection = connection;
        XTest.EnsureAvailable(connection);
    }

    public void Press(VirtualKeyCode key)
    {
        lock (this._connection.Gate)
        {
            var keysym = ToKeysym(key);
            var keycode = Xlib.XKeysymToKeycode(this._connection.Display, keysym);

            if (keycode == 0)
            {
                // The layout has no such key: give it a key code for as long as it is held down.
                keycode = this.AssignToUnusedKeycode(keysym, key);
                this._temporaryKeycodes[key] = keycode;
            }

            this.SendKey(keycode, isPress: true);
        }
    }

    public void Release(VirtualKeyCode key)
    {
        lock (this._connection.Gate)
        {
            if (this._temporaryKeycodes.Remove(key, out var temporaryKeycode))
            {
                this.SendKey(temporaryKeycode, isPress: false);
                this.ClearKeycode(temporaryKeycode);
                return;
            }

            var keycode = Xlib.XKeysymToKeycode(this._connection.Display, ToKeysym(key));
            if (keycode != 0)
            {
                this.SendKey(keycode, isPress: false);
            }
        }
    }

    public void TypeText(string text)
    {
        lock (this._connection.Gate)
        {
            var layout = this.ReadLayout();
            var shiftKeycode = Xlib.XKeysymToKeycode(this._connection.Display, ShiftKeysym);
            byte temporaryKeycode = 0;

            try
            {
                foreach (var rune in text.EnumerateRunes())
                {
                    var keysym = ToKeysym(rune);

                    if (layout.TryGetValue(keysym, out var key) && (!key.NeedsShift || shiftKeycode != 0))
                    {
                        if (key.NeedsShift)
                        {
                            this.SendKey(shiftKeycode, isPress: true);
                        }

                        this.SendKey(key.Keycode, isPress: true);
                        this.SendKey(key.Keycode, isPress: false);

                        if (key.NeedsShift)
                        {
                            this.SendKey(shiftKeycode, isPress: false);
                        }
                    }
                    else
                    {
                        if (temporaryKeycode == 0)
                        {
                            temporaryKeycode = this.FindUnusedKeycode();
                        }

                        this.AssignKeycode(temporaryKeycode, keysym);
                        this.SendKey(temporaryKeycode, isPress: true);
                        this.SendKey(temporaryKeycode, isPress: false);
                    }
                }
            }
            finally
            {
                if (temporaryKeycode != 0)
                {
                    this.ClearKeycode(temporaryKeycode);
                }
            }
        }
    }

    private static nuint ToKeysym(Rune rune)
    {
        // Latin-1 characters have the same value as their keysym. Other characters use the Unicode range of keysyms.
        var value = (uint)rune.Value;
        return value is (>= 0x20 and <= 0x7E) or (>= 0xA0 and <= 0xFF) ? value : UnicodeKeysymFlag | value;
    }

    private static nuint ToKeysym(VirtualKeyCode key)
    {
        if (key is >= VirtualKeyCode.A and <= VirtualKeyCode.Z)
        {
            return (nuint)('a' + (key - VirtualKeyCode.A));
        }

        if (key is >= VirtualKeyCode.Digit0 and <= VirtualKeyCode.Digit9)
        {
            return (nuint)('0' + (key - VirtualKeyCode.Digit0));
        }

        if (key is >= VirtualKeyCode.Numpad0 and <= VirtualKeyCode.Numpad9)
        {
            return (nuint)(0xFFB0 + (key - VirtualKeyCode.Numpad0));
        }

        if (key is >= VirtualKeyCode.F1 and <= VirtualKeyCode.F24)
        {
            return (nuint)(0xFFBE + (key - VirtualKeyCode.F1));
        }

        return key switch
        {
            VirtualKeyCode.Backspace => 0xFF08,
            VirtualKeyCode.Tab => 0xFF09,
            VirtualKeyCode.Clear => 0xFF0B,
            VirtualKeyCode.Enter => 0xFF0D,
            VirtualKeyCode.Shift or VirtualKeyCode.LeftShift => 0xFFE1,
            VirtualKeyCode.RightShift => 0xFFE2,
            VirtualKeyCode.Control or VirtualKeyCode.LeftControl or VirtualKeyCode.ControlOrCommand => 0xFFE3,
            VirtualKeyCode.RightControl => 0xFFE4,
            VirtualKeyCode.Alt or VirtualKeyCode.LeftAlt => 0xFFE9,
            VirtualKeyCode.RightAlt => 0xFFEA,
            VirtualKeyCode.LeftWindows => 0xFFEB,
            VirtualKeyCode.RightWindows => 0xFFEC,
            VirtualKeyCode.Pause => 0xFF13,
            VirtualKeyCode.Caps => 0xFFE5,
            VirtualKeyCode.Escape => 0xFF1B,
            VirtualKeyCode.Space => 0x20,
            VirtualKeyCode.PageUp => 0xFF55,
            VirtualKeyCode.PageDown => 0xFF56,
            VirtualKeyCode.End => 0xFF57,
            VirtualKeyCode.Home => 0xFF50,
            VirtualKeyCode.LeftArrow => 0xFF51,
            VirtualKeyCode.UpArrow => 0xFF52,
            VirtualKeyCode.RightArrow => 0xFF53,
            VirtualKeyCode.DownArrow => 0xFF54,
            VirtualKeyCode.PrintScreen => 0xFF61,
            VirtualKeyCode.Insert => 0xFF63,
            VirtualKeyCode.Delete => 0xFFFF,
            VirtualKeyCode.Help => 0xFF6A,
            VirtualKeyCode.Applications => 0xFF67,
            VirtualKeyCode.Multiply => 0xFFAA,
            VirtualKeyCode.Add => 0xFFAB,
            VirtualKeyCode.Separator => 0xFFAC,
            VirtualKeyCode.Subtract => 0xFFAD,
            VirtualKeyCode.Decimal => 0xFFAE,
            VirtualKeyCode.Divide => 0xFFAF,
            VirtualKeyCode.NumLock => 0xFF7F,
            VirtualKeyCode.ScrollLock => 0xFF14,
            VirtualKeyCode.BrowserBack => 0x1008FF26,
            VirtualKeyCode.BrowserForward => 0x1008FF27,
            VirtualKeyCode.BrowserRefresh => 0x1008FF29,
            VirtualKeyCode.BrowserStop => 0x1008FF28,
            VirtualKeyCode.BrowserSearch => 0x1008FF1B,
            VirtualKeyCode.BrowserFavorites => 0x1008FF30,
            VirtualKeyCode.BrowserHome => 0x1008FF18,
            VirtualKeyCode.VolumeMute => 0x1008FF12,
            VirtualKeyCode.VolumeDown => 0x1008FF11,
            VirtualKeyCode.VolumeUp => 0x1008FF13,
            VirtualKeyCode.MediaNextTrack => 0x1008FF17,
            VirtualKeyCode.MediaPrevTrack => 0x1008FF16,
            VirtualKeyCode.MediaStop => 0x1008FF15,
            VirtualKeyCode.MediaPlayPause => 0x1008FF14,
            VirtualKeyCode.LaunchMail => 0x1008FF19,
            VirtualKeyCode.LaunchMediaSelect => 0x1008FF32,
            VirtualKeyCode.LaunchApplication1 => 0x1008FF40,
            VirtualKeyCode.LaunchApplication2 => 0x1008FF41,
            VirtualKeyCode.Semicolon => ';',
            VirtualKeyCode.Plus => '=',
            VirtualKeyCode.Comma => ',',
            VirtualKeyCode.Minus => '-',
            VirtualKeyCode.Period => '.',
            VirtualKeyCode.Slash => '/',
            VirtualKeyCode.Tilde => '`',
            VirtualKeyCode.LeftBrace => '[',
            VirtualKeyCode.Backslash => '\\',
            VirtualKeyCode.RightBrace => ']',
            VirtualKeyCode.SingleQuote => '\'',
            _ => throw new PlatformNotSupportedException(Messages.Platform_Throw_KeyNotSupported.FormatInvariant(key, "Linux")),
        };
    }

    private void SendKey(byte keycode, bool isPress)
    {
        _ = Xlib.XTestFakeKeyEvent(this._connection.Display, keycode, isPress ? 1 : 0, 0);
        _ = Xlib.XFlush(this._connection.Display);
    }

    /// <summary>
    /// Returns, for each character or key the current layout can produce, the key code to press and whether Shift must be held.
    /// </summary>
    private Dictionary<nuint, (byte Keycode, bool NeedsShift)> ReadLayout()
    {
        var layout = new Dictionary<nuint, (byte Keycode, bool NeedsShift)>();
        _ = Xlib.XDisplayKeycodes(this._connection.Display, out var minKeycode, out var maxKeycode);

        var keysyms = Xlib.XGetKeyboardMapping(this._connection.Display, (byte)minKeycode, maxKeycode - minKeycode + 1, out var keysymsPerKeycode);
        if (keysyms == null)
        {
            return layout;
        }

        try
        {
            // The first two keysyms of a key code are what it produces alone and with Shift. A first pass
            // gives priority to keys that do not need Shift.
            for (var level = 0; level < Math.Min(2, keysymsPerKeycode); level++)
            {
                for (var keycode = minKeycode; keycode <= maxKeycode; keycode++)
                {
                    var keysym = keysyms[((keycode - minKeycode) * keysymsPerKeycode) + level];
                    if (keysym != 0)
                    {
                        layout.TryAdd(keysym, ((byte)keycode, level == 1));
                    }
                }
            }
        }
        finally
        {
            _ = Xlib.XFree(keysyms);
        }

        return layout;
    }

    private byte FindUnusedKeycode()
    {
        _ = Xlib.XDisplayKeycodes(this._connection.Display, out var minKeycode, out var maxKeycode);

        var keysyms = Xlib.XGetKeyboardMapping(this._connection.Display, (byte)minKeycode, maxKeycode - minKeycode + 1, out var keysymsPerKeycode);
        if (keysyms == null)
        {
            return (byte)maxKeycode;
        }

        try
        {
            for (var keycode = maxKeycode; keycode >= minKeycode; keycode--)
            {
                var isUnused = true;
                for (var level = 0; level < keysymsPerKeycode && isUnused; level++)
                {
                    isUnused = keysyms[((keycode - minKeycode) * keysymsPerKeycode) + level] == 0;
                }

                if (isUnused && !this._temporaryKeycodes.ContainsValue((byte)keycode))
                {
                    return (byte)keycode;
                }
            }
        }
        finally
        {
            _ = Xlib.XFree(keysyms);
        }

        return (byte)maxKeycode;
    }

    private byte AssignToUnusedKeycode(nuint keysym, VirtualKeyCode key)
    {
        var keycode = this.FindUnusedKeycode();
        if (keycode == 0)
        {
            throw new PlatformNotSupportedException(Messages.Platform_Throw_KeyNotSupported.FormatInvariant(key, "Linux"));
        }

        this.AssignKeycode(keycode, keysym);
        return keycode;
    }

    private void AssignKeycode(byte keycode, nuint keysym)
    {
        _ = Xlib.XChangeKeyboardMapping(this._connection.Display, keycode, 1, &keysym, 1);
        _ = Xlib.XSync(this._connection.Display, 0);
        Thread.Sleep(MappingChangeDelay);
    }

    private void ClearKeycode(byte keycode)
    {
        // Give applications the time to handle the last key event before its key code loses its meaning.
        _ = Xlib.XSync(this._connection.Display, 0);
        Thread.Sleep(MappingChangeDelay);

        nuint noSymbol = 0;
        _ = Xlib.XChangeKeyboardMapping(this._connection.Display, keycode, 1, &noSymbol, 1);
        _ = Xlib.XSync(this._connection.Display, 0);
    }
}
