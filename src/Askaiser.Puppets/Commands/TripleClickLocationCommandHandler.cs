using System.Threading.Tasks;

namespace Askaiser.Puppets.Commands
{
    internal class TripleClickLocationCommandHandler : BaseClickLocationCommandHandler
    {
        public TripleClickLocationCommandHandler(IMouseController mouseController)
            : base(mouseController)
        {
        }

        public override Task Execute(MouseLocationCommand command) => Execute(command, this.MouseController.TripleClick);
    }
}
