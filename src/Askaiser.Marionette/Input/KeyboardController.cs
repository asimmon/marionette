using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Askaiser.Marionette.Platform;

namespace Askaiser.Marionette.Input;

/// <summary>
/// Turns text and keyboard shortcuts into sequences of device events.
/// </summary>
/// <remarks>
/// Sequences can be canceled between two keys, but a canceled shortcut never leaves a key down.
/// </remarks>
internal sealed class KeyboardController
{
    private readonly IKeyboardDevice _device;
    private readonly InputTimings _timings;

    public KeyboardController(IKeyboardDevice device, InputTimings timings)
    {
        this._device = device;
        this._timings = timings;
    }

    /// <summary>
    /// Types text. Line breaks press the Enter key and tabulations press the Tab key, everything else is typed as characters.
    /// </summary>
    public Task TypeTextAsync(string text, CancellationToken cancellationToken)
    {
        return Task.Run(
            () =>
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
                        cancellationToken.ThrowIfCancellationRequested();
                        this._device.TypeText(text.Substring(start, i - start));
                    }

                    // A carriage return followed by a line feed is a single line break.
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var key = c == '\t' ? VirtualKeyCode.Tab : VirtualKeyCode.Enter;
                    this._device.Press(key);
                    this._device.Release(key);
                    start = i + 1;
                }

                if (start < text.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    this._device.TypeText(start == 0 ? text : text.Substring(start));
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Presses keys as a shortcut: modifier keys are held down while the other keys are pressed and released one after the other.
    /// </summary>
    public Task KeyPressAsync(IReadOnlyList<VirtualKeyCode> keys, CancellationToken cancellationToken)
    {
        return Task.Run(
            () =>
            {
                var modifiers = new List<VirtualKeyCode>(keys.Count);
                var others = new List<VirtualKeyCode>(keys.Count);

                foreach (var key in keys)
                {
                    var target = IsModifier(key) ? modifiers : others;
                    if (!target.Contains(key))
                    {
                        target.Add(key);
                    }
                }

                var pressedModifiers = 0;
                try
                {
                    foreach (var modifier in modifiers)
                    {
                        this._device.Press(modifier);
                        pressedModifiers++;
                    }

                    this.Settle(modifiers.Count > 0);
                    foreach (var key in others)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        this._device.Press(key);
                        this._device.Release(key);
                    }

                    this.Settle(modifiers.Count > 0);
                }
                finally
                {
                    // Never leave a modifier key down, even if a key could not be simulated or the shortcut was canceled.
                    for (var i = pressedModifiers - 1; i >= 0; i--)
                    {
                        this._device.Release(modifiers[i]);
                    }
                }
            },
            cancellationToken);
    }

    public Task KeyDownAsync(IReadOnlyList<VirtualKeyCode> keys, CancellationToken cancellationToken)
    {
        return Task.Run(
            () =>
            {
                foreach (var key in keys)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    this._device.Press(key);
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Releases keys. Once started, this is not canceled: stopping halfway would leave keys down.
    /// </summary>
    public Task KeyUpAsync(IReadOnlyList<VirtualKeyCode> keys, CancellationToken cancellationToken)
    {
        return Task.Run(
            () =>
            {
                foreach (var key in keys)
                {
                    this._device.Release(key);
                }
            },
            cancellationToken);
    }

    private static bool IsModifier(VirtualKeyCode key) => key
        is VirtualKeyCode.Shift or VirtualKeyCode.LeftShift or VirtualKeyCode.RightShift
        or VirtualKeyCode.Control or VirtualKeyCode.LeftControl or VirtualKeyCode.RightControl
        or VirtualKeyCode.Alt or VirtualKeyCode.LeftAlt or VirtualKeyCode.RightAlt
        or VirtualKeyCode.LeftWindows or VirtualKeyCode.RightWindows
        or VirtualKeyCode.ControlOrCommand;

    private void Settle(bool hasModifiers)
    {
        if (hasModifiers && this._timings.ModifierSettle > TimeSpan.Zero)
        {
            Thread.Sleep(this._timings.ModifierSettle);
        }
    }
}
