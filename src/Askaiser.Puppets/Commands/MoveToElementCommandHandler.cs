using System.Threading.Tasks;

namespace Askaiser.Puppets.Commands
{
    internal class MoveToLocationCommandHandler
    {
        private readonly IMouseController _mouseController;

        public MoveToLocationCommandHandler(IMouseController mouseController)
        {
            this._mouseController = mouseController;
        }

        public async Task Execute(MouseLocationCommand command)
        {
            await this._mouseController.Move(command.X, command.Y, command.Speed).ConfigureAwait(false);
        }
    }
}
