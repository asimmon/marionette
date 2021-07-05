using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
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