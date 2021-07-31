using System.Collections.Generic;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Tests
{
    internal sealed class FakeMouseController : IMouseController
    {
        private readonly List<string> _actions;

        public FakeMouseController()
        {
            this._actions = new List<string>();
        }

        public IReadOnlyList<string> Actions
        {
            get => this._actions;
        }

        public Task Move(int x, int y, MouseSpeed speed)
        {
            this.AddAction("Move", x, y, speed);
            return Task.CompletedTask;
        }

        public Task SingleClick(int x, int y, MouseSpeed speed)
        {
            this.AddAction("SingleClick", x, y, speed);
            return Task.CompletedTask;
        }

        public Task DoubleClick(int x, int y, MouseSpeed speed)
        {
            this.AddAction("DoubleClick", x, y, speed);
            return Task.CompletedTask;
        }

        public Task TripleClick(int x, int y, MouseSpeed speed)
        {
            this.AddAction("TripleClick", x, y, speed);
            return Task.CompletedTask;
        }

        public Task RightClick(int x, int y, MouseSpeed speed)
        {
            this.AddAction("RightClick", x, y, speed);
            return Task.CompletedTask;
        }

        public Task DragFrom(int x, int y, MouseSpeed speed)
        {
            this.AddAction("DragFrom", x, y, speed);
            return Task.CompletedTask;
        }

        public Task DropTo(int x, int y, MouseSpeed speed)
        {
            this.AddAction("DropTo", x, y, speed);
            return Task.CompletedTask;
        }

        public Task WheelUp()
        {
            this.AddAction("WheelUp");
            return Task.CompletedTask;
        }

        public Task WheelDown()
        {
            this.AddAction("WheelDown");
            return Task.CompletedTask;
        }

        private void AddAction(string action, int x, int y, MouseSpeed speed)
        {
            this._actions.Add($"{action}({x}, {y}, {speed})");
        }

        private void AddAction(string action)
        {
            this._actions.Add($"{action}()");
        }
    }
}
