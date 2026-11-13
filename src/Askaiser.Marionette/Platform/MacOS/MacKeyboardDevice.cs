using System;
using System.Runtime.Versioning;
using System.Threading;

namespace Askaiser.Marionette.Platform.MacOS;

/// <summary>
/// Simulates a keyboard with Quartz events.
/// </summary>
/// <remarks>
/// A Quartz key event carries the modifier keys that are held down while it happens, so those are tracked here.
/// Key codes designate physical keys named after the US layout.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed unsafe class MacKeyboardDevice : IKeyboardDevice
{
    private readonly Lock _gate = new();
    private ulong _modifiers;

    public void Press(VirtualKeyCode key)
    {
        this.Send(key, isPress: true);
    }

    public void Release(VirtualKeyCode key)
    {
        this.Send(key, isPress: false);
    }

    public void TypeText(string text)
    {
        EventPermission.EnsureGranted();

        // A character is attached to an event of an arbitrary key. Surrogate pairs must stay in the same event.
        var buffer = stackalloc ushort[2];

        foreach (var rune in text.EnumerateRunes())
        {
            var length = rune.EncodeToUtf16(new Span<char>(buffer, 2));

            for (var isPress = 1; isPress >= 0; isPress--)
            {
                var cgEvent = CoreGraphics.CGEventCreateKeyboardEvent(0, 0, (byte)isPress);
                if (cgEvent == 0)
                {
                    continue;
                }

                // Modifier keys that are held down must not turn the character into a shortcut.
                CoreGraphics.CGEventSetFlags(cgEvent, 0);
                CoreGraphics.CGEventKeyboardSetUnicodeString(cgEvent, (nuint)length, buffer);
                CoreGraphics.CGEventPost(CoreGraphics.HidEventTap, cgEvent);
                CoreGraphics.CFRelease(cgEvent);
            }
        }
    }

    private static ulong ToModifierFlag(VirtualKeyCode key) => key switch
    {
        VirtualKeyCode.Shift or VirtualKeyCode.LeftShift or VirtualKeyCode.RightShift => CoreGraphics.EventFlagShift,
        VirtualKeyCode.Control or VirtualKeyCode.LeftControl or VirtualKeyCode.RightControl => CoreGraphics.EventFlagControl,
        VirtualKeyCode.Alt or VirtualKeyCode.LeftAlt or VirtualKeyCode.RightAlt => CoreGraphics.EventFlagAlternate,
        VirtualKeyCode.LeftWindows or VirtualKeyCode.RightWindows or VirtualKeyCode.ControlOrCommand => CoreGraphics.EventFlagCommand,
        _ => 0,
    };

    private static ushort ToKeyCode(VirtualKeyCode key) => key switch
    {
        VirtualKeyCode.A => 0x00,
        VirtualKeyCode.S => 0x01,
        VirtualKeyCode.D => 0x02,
        VirtualKeyCode.F => 0x03,
        VirtualKeyCode.H => 0x04,
        VirtualKeyCode.G => 0x05,
        VirtualKeyCode.Z => 0x06,
        VirtualKeyCode.X => 0x07,
        VirtualKeyCode.C => 0x08,
        VirtualKeyCode.V => 0x09,
        VirtualKeyCode.B => 0x0B,
        VirtualKeyCode.Q => 0x0C,
        VirtualKeyCode.W => 0x0D,
        VirtualKeyCode.E => 0x0E,
        VirtualKeyCode.R => 0x0F,
        VirtualKeyCode.Y => 0x10,
        VirtualKeyCode.T => 0x11,
        VirtualKeyCode.Digit1 => 0x12,
        VirtualKeyCode.Digit2 => 0x13,
        VirtualKeyCode.Digit3 => 0x14,
        VirtualKeyCode.Digit4 => 0x15,
        VirtualKeyCode.Digit6 => 0x16,
        VirtualKeyCode.Digit5 => 0x17,
        VirtualKeyCode.Plus => 0x18,
        VirtualKeyCode.Digit9 => 0x19,
        VirtualKeyCode.Digit7 => 0x1A,
        VirtualKeyCode.Minus => 0x1B,
        VirtualKeyCode.Digit8 => 0x1C,
        VirtualKeyCode.Digit0 => 0x1D,
        VirtualKeyCode.RightBrace => 0x1E,
        VirtualKeyCode.O => 0x1F,
        VirtualKeyCode.U => 0x20,
        VirtualKeyCode.LeftBrace => 0x21,
        VirtualKeyCode.I => 0x22,
        VirtualKeyCode.P => 0x23,
        VirtualKeyCode.Enter => 0x24,
        VirtualKeyCode.L => 0x25,
        VirtualKeyCode.J => 0x26,
        VirtualKeyCode.SingleQuote => 0x27,
        VirtualKeyCode.K => 0x28,
        VirtualKeyCode.Semicolon => 0x29,
        VirtualKeyCode.Backslash => 0x2A,
        VirtualKeyCode.Comma => 0x2B,
        VirtualKeyCode.Slash => 0x2C,
        VirtualKeyCode.N => 0x2D,
        VirtualKeyCode.M => 0x2E,
        VirtualKeyCode.Period => 0x2F,
        VirtualKeyCode.Tab => 0x30,
        VirtualKeyCode.Space => 0x31,
        VirtualKeyCode.Tilde => 0x32,
        VirtualKeyCode.Backspace => 0x33,
        VirtualKeyCode.Escape => 0x35,
        VirtualKeyCode.RightWindows => 0x36,
        VirtualKeyCode.LeftWindows or VirtualKeyCode.ControlOrCommand => 0x37,
        VirtualKeyCode.Shift or VirtualKeyCode.LeftShift => 0x38,
        VirtualKeyCode.Caps => 0x39,
        VirtualKeyCode.Alt or VirtualKeyCode.LeftAlt => 0x3A,
        VirtualKeyCode.Control or VirtualKeyCode.LeftControl => 0x3B,
        VirtualKeyCode.RightShift => 0x3C,
        VirtualKeyCode.RightAlt => 0x3D,
        VirtualKeyCode.RightControl => 0x3E,
        VirtualKeyCode.F17 => 0x40,
        VirtualKeyCode.Decimal => 0x41,
        VirtualKeyCode.Multiply => 0x43,
        VirtualKeyCode.Add => 0x45,
        VirtualKeyCode.Clear or VirtualKeyCode.NumLock => 0x47,
        VirtualKeyCode.VolumeUp => 0x48,
        VirtualKeyCode.VolumeDown => 0x49,
        VirtualKeyCode.VolumeMute => 0x4A,
        VirtualKeyCode.Divide => 0x4B,
        VirtualKeyCode.Subtract => 0x4E,
        VirtualKeyCode.F18 => 0x4F,
        VirtualKeyCode.F19 => 0x50,
        VirtualKeyCode.Numpad0 => 0x52,
        VirtualKeyCode.Numpad1 => 0x53,
        VirtualKeyCode.Numpad2 => 0x54,
        VirtualKeyCode.Numpad3 => 0x55,
        VirtualKeyCode.Numpad4 => 0x56,
        VirtualKeyCode.Numpad5 => 0x57,
        VirtualKeyCode.Numpad6 => 0x58,
        VirtualKeyCode.Numpad7 => 0x59,
        VirtualKeyCode.F20 => 0x5A,
        VirtualKeyCode.Numpad8 => 0x5B,
        VirtualKeyCode.Numpad9 => 0x5C,
        VirtualKeyCode.Separator => 0x5F,
        VirtualKeyCode.F5 => 0x60,
        VirtualKeyCode.F6 => 0x61,
        VirtualKeyCode.F7 => 0x62,
        VirtualKeyCode.F3 => 0x63,
        VirtualKeyCode.F8 => 0x64,
        VirtualKeyCode.F9 => 0x65,
        VirtualKeyCode.F11 => 0x67,
        VirtualKeyCode.F13 or VirtualKeyCode.PrintScreen => 0x69,
        VirtualKeyCode.F16 => 0x6A,
        VirtualKeyCode.F14 or VirtualKeyCode.ScrollLock => 0x6B,
        VirtualKeyCode.F10 => 0x6D,
        VirtualKeyCode.F12 => 0x6F,
        VirtualKeyCode.F15 or VirtualKeyCode.Pause => 0x71,
        VirtualKeyCode.Help or VirtualKeyCode.Insert => 0x72,
        VirtualKeyCode.Home => 0x73,
        VirtualKeyCode.PageUp => 0x74,
        VirtualKeyCode.Delete => 0x75,
        VirtualKeyCode.F4 => 0x76,
        VirtualKeyCode.End => 0x77,
        VirtualKeyCode.F2 => 0x78,
        VirtualKeyCode.PageDown => 0x79,
        VirtualKeyCode.F1 => 0x7A,
        VirtualKeyCode.LeftArrow => 0x7B,
        VirtualKeyCode.RightArrow => 0x7C,
        VirtualKeyCode.DownArrow => 0x7D,
        VirtualKeyCode.UpArrow => 0x7E,
        _ => throw new PlatformNotSupportedException(Messages.Platform_Throw_KeyNotSupported.FormatInvariant(key, "macOS")),
    };

    private void Send(VirtualKeyCode key, bool isPress)
    {
        var keyCode = ToKeyCode(key);
        EventPermission.EnsureGranted();

        lock (this._gate)
        {
            var modifier = ToModifierFlag(key);
            this._modifiers = isPress ? this._modifiers | modifier : this._modifiers & ~modifier;

            var cgEvent = CoreGraphics.CGEventCreateKeyboardEvent(0, keyCode, isPress ? (byte)1 : (byte)0);
            if (cgEvent == 0)
            {
                return;
            }

            CoreGraphics.CGEventSetFlags(cgEvent, this._modifiers);
            CoreGraphics.CGEventPost(CoreGraphics.HidEventTap, cgEvent);
            CoreGraphics.CFRelease(cgEvent);
        }
    }
}
