using System;
using System.Threading.Tasks;

namespace Askaiser.Puppets.Commands
{
    internal abstract class BaseClickLocationCommandHandler
    {
        protected BaseClickLocationCommandHandler(IMouseController mouseController)
        {
            this.MouseController = mouseController;
        }

        protected  IMouseController MouseController { get; }

        public abstract Task Execute(MouseLocationCommand command);

        protected static async Task Execute(MouseLocationCommand command, Func<int, int, MouseSpeed, Task> mouseClickFunc)
        {
            await mouseClickFunc(command.X, command.Y, command.Speed).ConfigureAwait(false);
        }
    }
}
