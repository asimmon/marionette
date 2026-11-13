using System;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace Askaiser.Marionette.Platform.Windows;

[SupportedOSPlatform("windows")]
internal sealed unsafe class WindowsKeyboardDevice : IKeyboardDevice
{
    public void Press(VirtualKeyCode key)
    {
        SendKey(key, isPress: true);
    }

    public void Release(VirtualKeyCode key)
    {
        SendKey(key, isPress: false);
    }

    public void TypeText(string text)
    {
        // Characters are sent as Unicode input, two events (down and up) per UTF-16 code unit, independently of the keyboard layout.
        const int charactersPerBatch = 64;
        var inputs = stackalloc NativeMethods.Input[charactersPerBatch * 2];

        for (var start = 0; start < text.Length; start += charactersPerBatch)
        {
            var count = Math.Min(charactersPerBatch, text.Length - start);
            for (var i = 0; i < count; i++)
            {
                inputs[i * 2] = CreateUnicodeInput(text[start + i], isPress: true);
                inputs[(i * 2) + 1] = CreateUnicodeInput(text[start + i], isPress: false);
            }

            Send(inputs, count * 2);
        }
    }

    private static NativeMethods.Input CreateUnicodeInput(char character, bool isPress)
    {
        var input = new NativeMethods.Input { Type = NativeMethods.InputKeyboard };
        input.Data.Keyboard.ScanCode = character;
        input.Data.Keyboard.Flags = NativeMethods.KeyEventUnicode | (isPress ? 0 : NativeMethods.KeyEventKeyUp);
        return input;
    }

    private static void SendKey(VirtualKeyCode key, bool isPress)
    {
        // Key values are Windows virtual-key codes, except for the platform-neutral shortcut modifier.
        var virtualKey = key == VirtualKeyCode.ControlOrCommand ? VirtualKeyCode.Control : key;
        if ((int)virtualKey is <= 0 or > 0xFF)
        {
            throw new PlatformNotSupportedException(Messages.Platform_Throw_KeyNotSupported.FormatInvariant(key, "Windows"));
        }

        var input = new NativeMethods.Input { Type = NativeMethods.InputKeyboard };
        input.Data.Keyboard.VirtualKey = (ushort)virtualKey;
        input.Data.Keyboard.ScanCode = (ushort)NativeMethods.MapVirtualKey((uint)virtualKey, NativeMethods.MapVirtualKeyToScanCode);
        input.Data.Keyboard.Flags = (isPress ? 0 : NativeMethods.KeyEventKeyUp) | (IsExtendedKey(virtualKey) ? NativeMethods.KeyEventExtendedKey : 0);

        Send(&input, 1);
    }

    /// <summary>
    /// Some keys share their scan code with a key of the numeric keypad and are told apart by the extended flag.
    /// </summary>
    private static bool IsExtendedKey(VirtualKeyCode key) => key
        is VirtualKeyCode.Insert or VirtualKeyCode.Delete or VirtualKeyCode.Home or VirtualKeyCode.End
        or VirtualKeyCode.PageUp or VirtualKeyCode.PageDown
        or VirtualKeyCode.LeftArrow or VirtualKeyCode.RightArrow or VirtualKeyCode.UpArrow or VirtualKeyCode.DownArrow
        or VirtualKeyCode.RightControl or VirtualKeyCode.RightAlt
        or VirtualKeyCode.LeftWindows or VirtualKeyCode.RightWindows or VirtualKeyCode.Applications
        or VirtualKeyCode.NumLock or VirtualKeyCode.Divide or VirtualKeyCode.PrintScreen;

    private static void Send(NativeMethods.Input* inputs, int count)
    {
        if (NativeMethods.SendInput((uint)count, inputs, sizeof(NativeMethods.Input)) != (uint)count)
        {
            // Input is rejected when the active window belongs to a process with a higher privilege level.
            throw new InvalidOperationException(new Win32Exception().Message);
        }
    }
}
