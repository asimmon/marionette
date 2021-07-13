using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
{
    internal class KeyUpCommandHandler
    {
        private readonly IKeyboardController _keyboardController;

        public KeyUpCommandHandler(IKeyboardController keyboardController)
        {
            this._keyboardController = keyboardController;
        }

        public async Task Execute(KeyboardKeysCommand command)
        {
            await this._keyboardController.KeyUp(command.KeyCodes).ConfigureAwait(false);
        }
    }
}