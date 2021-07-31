using System.Threading.Tasks;

namespace Askaiser.Marionette
{
    internal interface IMouseController
    {
        Task Move(int x, int y, MouseSpeed speed);

        Task SingleClick(int x, int y, MouseSpeed speed);

        Task DoubleClick(int x, int y, MouseSpeed speed);

        Task TripleClick(int x, int y, MouseSpeed speed);

        Task RightClick(int x, int y, MouseSpeed speed);

        Task DragFrom(int x, int y, MouseSpeed speed);

        Task DropTo(int x, int y, MouseSpeed speed);

        Task WheelUp();

        Task WheelDown();
    }
}
