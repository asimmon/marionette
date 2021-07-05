using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
{
    internal class SingleClickLocationCommandHandler : BaseClickLocationCommandHandler
    {
        public SingleClickLocationCommandHandler(IMouseController mouseController)
            : base(mouseController)
        {
        }

        public override Task Execute(MouseLocationCommand command) => Execute(command, this.MouseController.SingleClick);
    }
}