using System.Threading.Tasks;

namespace Askaiser.Puppets.Commands
{
    internal class MouseWheelCommandHandler
    {
        private readonly IMouseController _mouseController;

        public MouseWheelCommandHandler(IMouseController mouseController)
        {
            this._mouseController = mouseController;
        }

        public async Task Execute(MouseWheelCommand command)
        {
            var task = command.IsUp ? this._mouseController.WheelUp() : this._mouseController.WheelDown();
            await task.ConfigureAwait(false);
        }
    }
}
