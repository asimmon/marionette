using System.Threading.Tasks;

namespace Askaiser.Puppets.Commands
{
    internal class DropLocationCommandHandler : BaseClickLocationCommandHandler
    {
        public DropLocationCommandHandler(IMouseController mouseController)
            : base(mouseController)
        {
        }

        public override Task Execute(MouseLocationCommand command) => Execute(command, this.MouseController.Drop);
    }
}
