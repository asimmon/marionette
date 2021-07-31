using System.Threading.Tasks;

namespace Askaiser.Marionette.Tests
{
    internal sealed class FakeKeyboardController : IKeyboardController
    {
        public Task TypeText(string text)
        {
            return Task.CompletedTask;
        }

        public Task KeyPress(params VirtualKeyCode[] keyCodes)
        {
            return Task.CompletedTask;
        }

        public Task KeyDown(params VirtualKeyCode[] keyCodes)
        {
            return Task.CompletedTask;
        }

        public Task KeyUp(params VirtualKeyCode[] keyCodes)
        {
            return Task.CompletedTask;
        }
    }
}
