using System.Collections.Generic;
using System.Threading.Tasks;
using Askaiser.Marionette.Platform;

namespace Askaiser.Marionette.Input;

/// <summary>
/// Turns text and keyboard shortcuts into sequences of device events.
/// </summary>
internal sealed class KeyboardController
{
    private readonly IKeyboardDevice _device;

    public KeyboardController(IKeyboardDevice device)
    {
        this._device = device;
    }

    /// <summary>
    /// Types text. Line breaks press the Enter key and tabulations press the Tab key, everything else is typed as characters.
    /// </summary>
    public Task TypeTextAsync(string text)
    {
        return Task.Run(() =>
        {
            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c is not ('\r' or '\n' or '\t'))
                {
                    continue;
                }

                if (i > start)
                {
                    this._device.TypeText(text.Substring(start, i - start));
                }

                // A carriage return followed by a line feed is a single line break.
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                var key = c == '\t' ? VirtualKeyCode.Tab : VirtualKeyCode.Enter;
                this._device.Press(key);
                this._device.Release(key);
                start = i + 1;
            }

            if (start < text.Length)
            {
                this._device.TypeText(text.Substring(start));
            }
        });
    }

    /// <summary>
    /// Presses keys as a shortcut: modifier keys are held down while the other keys are pressed and released one after the other.
    /// </summary>
    public Task KeyPressAsync(IReadOnlyList<VirtualKeyCode> keys)
    {
        return Task.Run(() =>
        {
            var modifiers = new List<VirtualKeyCode>();
            var others = new List<VirtualKeyCode>();

            foreach (var key in keys)
            {
                var target = IsModifier(key) ? modifiers : others;
                if (!target.Contains(key))
                {
                    target.Add(key);
                }
            }

            foreach (var modifier in modifiers)
            {
                this._device.Press(modifier);
            }

            try
            {
                foreach (var key in others)
                {
                    this._device.Press(key);
                    this._device.Release(key);
                }
            }
            finally
            {
                // Never leave a modifier key down, even if a key could not be simulated.
                for (var i = modifiers.Count - 1; i >= 0; i--)
                {
                    this._device.Release(modifiers[i]);
                }
            }
        });
    }

    public Task KeyDownAsync(IReadOnlyList<VirtualKeyCode> keys)
    {
        return Task.Run(() =>
        {
            foreach (var key in keys)
            {
                this._device.Press(key);
            }
        });
    }

    public Task KeyUpAsync(IReadOnlyList<VirtualKeyCode> keys)
    {
        return Task.Run(() =>
        {
            foreach (var key in keys)
            {
                this._device.Release(key);
            }
        });
    }

    private static bool IsModifier(VirtualKeyCode key) => key
        is VirtualKeyCode.Shift or VirtualKeyCode.LeftShift or VirtualKeyCode.RightShift
        or VirtualKeyCode.Control or VirtualKeyCode.LeftControl or VirtualKeyCode.RightControl
        or VirtualKeyCode.Alt or VirtualKeyCode.LeftAlt or VirtualKeyCode.RightAlt
        or VirtualKeyCode.LeftWindows or VirtualKeyCode.RightWindows
        or VirtualKeyCode.ControlOrCommand;
}
