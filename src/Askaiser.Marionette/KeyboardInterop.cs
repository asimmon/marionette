using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Askaiser.Marionette.Keyboard;

namespace Askaiser.Marionette;

internal static class KeyboardInterop
{
    private static readonly Lazy<InputSimulator> LazyInputSimulator = new Lazy<InputSimulator>(() => new InputSimulator());

    private static readonly HashSet<VirtualKeyCode> KnownModifiers = new()
    {
        VirtualKeyCode.LeftShift,
        VirtualKeyCode.LeftShift,
        VirtualKeyCode.Shift,
        VirtualKeyCode.LeftWindows,
        VirtualKeyCode.RightWindows,
        VirtualKeyCode.Control,
        VirtualKeyCode.LeftControl,
        VirtualKeyCode.RightControl,
        VirtualKeyCode.LeftAlt,
        VirtualKeyCode.RightAlt,
        VirtualKeyCode.Alt,
    };

    public static async Task TypeText(string text)
    {
        await Task.Run(() => LazyInputSimulator.Value.Keyboard.TextEntry(text)).ConfigureAwait(false);
    }

    public static async Task KeyPress(params VirtualKeyCode[] keyCodes)
    {
        await Task.Run(() =>
        {
            var modifiers = new HashSet<VirtualKeyCode>();
            var otherKeyCodes = new HashSet<VirtualKeyCode>();

            foreach (var keyCode in keyCodes)
            {
                (KnownModifiers.Contains(keyCode) ? modifiers : otherKeyCodes).Add(keyCode);
            }

            _ = modifiers.Count > 0
                ? LazyInputSimulator.Value.Keyboard.ModifiedKeyStroke(modifiers, otherKeyCodes)
                : LazyInputSimulator.Value.Keyboard.KeyPress(otherKeyCodes.ToArray());
        }).ConfigureAwait(false);
    }

    public static async Task KeyDown(params VirtualKeyCode[] keyCodes)
    {
        await Task.Run(() =>
        {
            foreach (var keyCode in keyCodes)
            {
                LazyInputSimulator.Value.Keyboard.KeyDown(keyCode);
            }
        }).ConfigureAwait(false);
    }

    public static async Task KeyUp(params VirtualKeyCode[] keyCodes)
    {
        await Task.Run(() =>
        {
            foreach (var keyCode in keyCodes)
            {
                LazyInputSimulator.Value.Keyboard.KeyUp(keyCode);
            }
        }).ConfigureAwait(false);
    }
}
