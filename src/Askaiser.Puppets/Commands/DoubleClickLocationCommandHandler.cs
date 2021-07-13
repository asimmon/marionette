using System.Threading.Tasks;

namespace Askaiser.Puppets.Commands
{
    internal class DoubleClickLocationCommandHandler : BaseClickLocationCommandHandler
    {
        public DoubleClickLocationCommandHandler(IMouseController mouseController)
            : base(mouseController)
        {
        }

        public override Task Execute(MouseLocationCommand command) => Execute(command, this.MouseController.DoubleClick);
    }
}
