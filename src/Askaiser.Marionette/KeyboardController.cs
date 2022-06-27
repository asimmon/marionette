using System;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

internal class KeyboardController : IKeyboardController
{
    private static readonly string[] EndOfLines =
    {
        "\r\n", "\r", "\n",
    };

    public async Task TypeText(string text)
    {
        var lines = text.Split(EndOfLines, StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (line.Length > 0)
            {
                await KeyboardInterop.TypeText(line).ConfigureAwait(false);
            }

            if (i < lines.Length - 1)
            {
                await KeyboardInterop.KeyPress(VirtualKeyCode.RETURN).ConfigureAwait(false);
            }
        }
    }

    public async Task KeyPress(params VirtualKeyCode[] keyCodes)
    {
        await KeyboardInterop.KeyPress(keyCodes).ConfigureAwait(false);
    }

    public async Task KeyDown(params VirtualKeyCode[] keyCodes)
    {
        await KeyboardInterop.KeyDown(keyCodes).ConfigureAwait(false);
    }

    public async Task KeyUp(params VirtualKeyCode[] keyCodes)
    {
        await KeyboardInterop.KeyUp(keyCodes).ConfigureAwait(false);
    }
}
