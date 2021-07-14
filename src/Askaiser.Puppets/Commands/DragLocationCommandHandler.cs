﻿using System.Threading.Tasks;

namespace Askaiser.Puppets.Commands
{
    internal class DragLocationCommandHandler : BaseClickLocationCommandHandler
    {
        public DragLocationCommandHandler(IMouseController mouseController)
            : base(mouseController)
        {
        }

        public override Task Execute(MouseLocationCommand command) => Execute(command, this.MouseController.Drag);
    }
}