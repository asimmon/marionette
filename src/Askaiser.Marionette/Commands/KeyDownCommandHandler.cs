using System.Threading.Tasks;

namespace Askaiser.Marionette.Commands
{
    internal class KeyDownCommandHandler
    {
        private readonly IKeyboardController _keyboardController;

        public KeyDownCommandHandler(IKeyboardController keyboardController)
        {
            this._keyboardController = keyboardController;
        }

        public async Task Execute(KeyboardKeysCommand command)
        {
            await this._keyboardController.KeyDown(command.KeyCodes).ConfigureAwait(false);
        }
    }
}
