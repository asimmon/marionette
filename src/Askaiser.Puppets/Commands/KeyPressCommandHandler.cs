using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
{
    internal class KeyPressCommandHandler
    {
        private readonly IKeyboardController _keyboardController;

        public KeyPressCommandHandler(IKeyboardController keyboardController)
        {
            this._keyboardController = keyboardController;
        }

        public async Task Execute(KeyboardKeysCommand command)
        {
            await this._keyboardController.KeyPress(command.KeyCodes).ConfigureAwait(false);
        }
    }
}