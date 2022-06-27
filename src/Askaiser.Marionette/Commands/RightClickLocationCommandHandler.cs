using System.Threading.Tasks;

namespace Askaiser.Marionette.Commands;

internal class RightClickLocationCommandHandler : BaseClickLocationCommandHandler
{
    public RightClickLocationCommandHandler(IMouseController mouseController)
        : base(mouseController)
    {
    }

    public override Task Execute(MouseLocationCommand command) => Execute(command, this.MouseController.RightClick);
}
