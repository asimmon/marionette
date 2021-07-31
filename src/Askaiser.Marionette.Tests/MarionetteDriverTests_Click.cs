using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public class MarionetteDriverTests_Click : BaseMarionetteDriverTests
    {
        private const string SingleClick = "SingleClick";
        private const string DoubleClick = "DoubleClick";
        private const string TripleClick = "TripleClick";
        private const string RightClick = "RightClick";
        private const string DragFrom = "DragFrom";
        private const string DropTo = "DropTo";

        [Theory]
        [InlineData(SingleClick)]
        [InlineData(DoubleClick)]
        [InlineData(TripleClick)]
        [InlineData(RightClick)]
        [InlineData(DragFrom)]
        [InlineData(DropTo)]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Driver cannot be used outside of test scope.")]
        public async Task AnyClickAction_Works(string actionName)
        {
            using var driver = this.CreateDriver();
            driver.SetCurrentMonitor(0);
            driver.SetMouseSpeed(MouseSpeed.Fast);
            var expectedMonitor = FakeMonitorService.Monitors[0];
            var actualMonitor = await driver.GetCurrentMonitor();
            Assert.Equal(expectedMonitor, actualMonitor);

            var needle = new FakeElement("needle");
            var searchRect = actualMonitor.FromCenter(100, 100);
            this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 10, 90, 90) }));

            Func<Task> action = actionName switch
            {
                SingleClick => () => driver.SingleClick(needle, searchRect: searchRect),
                DoubleClick => () => driver.DoubleClick(needle, searchRect: searchRect),
                TripleClick => () => driver.TripleClick(needle, searchRect: searchRect),
                RightClick => () => driver.RightClick(needle, searchRect: searchRect),
                DragFrom => () => driver.DragFrom(needle, searchRect: searchRect),
                DropTo => () => driver.DropTo(needle, searchRect: searchRect),
                _ => throw new ArgumentOutOfRangeException(nameof(actionName), actionName)
            };

            await action();

            var (expectedX, expectedY) = actualMonitor.Center;
            var expectedResult = $"{actionName}({expectedX}, {expectedY}, Fast)";
            var actualResult = Assert.Single(this.MouseController.Actions);

            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(FakeMonitorService.Screen0)]
        [InlineData(FakeMonitorService.Screen1)]
        [InlineData(FakeMonitorService.Screen2)]
        [InlineData(FakeMonitorService.Screen3)]
        [InlineData(FakeMonitorService.Screen4)]
        public async Task SingleClick_AnyMonitor_Works(int monitorIndex)
        {
            using var driver = this.CreateDriver();
            driver.SetCurrentMonitor(monitorIndex);
            driver.SetMouseSpeed(MouseSpeed.Slow);

            var expectedMonitor = FakeMonitorService.Monitors[monitorIndex];
            var actualMonitor = await driver.GetCurrentMonitor();
            Assert.Equal(expectedMonitor, actualMonitor);

            var needle = new FakeElement("needle");
            var searchRect = actualMonitor.FromCenter(100, 100);
            this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 10, 90, 90) }));

            await driver.SingleClick(needle, searchRect: searchRect);

            var (expectedX, expectedY) = actualMonitor.Center;
            var expectedResult = $"SingleClick({expectedX}, {expectedY}, Slow)";
            var actualResult = Assert.Single(this.MouseController.Actions);

            Assert.Equal(expectedResult, actualResult);
        }
    }
}
