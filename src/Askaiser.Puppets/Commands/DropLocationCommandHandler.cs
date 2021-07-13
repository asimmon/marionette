using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
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