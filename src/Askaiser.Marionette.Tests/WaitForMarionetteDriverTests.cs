using System;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public class WaitForMarionetteDriverTests : BaseMarionetteDriverTests
    {
        [Fact]
        public async Task WaitFor_WhenNegativeTimeout_Throws()
        {
            using var driver = this.CreateDriver();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => driver.WaitFor(new FakeElement("needle"), TimeSpan.FromSeconds(-1)));
        }

        [Fact]
        public async Task WaitFor_WhenSingleLocation_Works()
        {
            using var driver = this.CreateDriver();

            var needle = new FakeElement("needle");
            var expectedResult = new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50) });
            this.ElementRecognizer.AddExpectedResult(needle, expectedResult);

            var actualResult = await driver.WaitFor(needle);
            AssertSearchResult(expectedResult, actualResult, null);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Execute_WhenNoLocation_Throws(bool useFailureScreenshotPath)
        {
            var opts = useFailureScreenshotPath ? new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" } : new DriverOptions();
            using var driver = this.CreateDriver(opts);

            var needle = new FakeElement("needle");
            var expectedResult = new SearchResult(needle, Array.Empty<Rectangle>());
            this.ElementRecognizer.AddExpectedResult(needle, expectedResult);

            var ex = await Assert.ThrowsAsync<ElementNotFoundException>(() => driver.WaitFor(needle));

            Assert.Equal(needle, ex.Element);

            if (useFailureScreenshotPath)
            {
                var failure = Assert.Single(this.FileWriter.SavedFailures);
                var monitor = await this.MonitorService.GetMonitor(0);

                Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
                Assert.EndsWith("_needle.png", failure.Path);

                Assert.Equal(monitor.Width, failure.Width);
                Assert.Equal(monitor.Height, failure.Height);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Execute_WhenTooManyLocations_Throws(bool useFailureScreenshotPath)
        {
            var opts = useFailureScreenshotPath ? new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" } : new DriverOptions();
            using var driver = this.CreateDriver(opts);

            var needle = new FakeElement("needle");
            var expectedResult = new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50), new Rectangle(100, 400, 200, 500) });
            this.ElementRecognizer.AddExpectedResult(needle, expectedResult);

            var ex = await Assert.ThrowsAsync<MultipleElementFoundException>(() => driver.WaitFor(needle));

            AssertSearchResult(expectedResult, ex.Result, null);

            if (useFailureScreenshotPath)
            {
                var failure = Assert.Single(this.FileWriter.SavedFailures);
                var monitor = await this.MonitorService.GetMonitor(0);

                Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
                Assert.EndsWith("_needle.png", failure.Path);

                Assert.Equal(monitor.Width, failure.Width);
                Assert.Equal(monitor.Height, failure.Height);
            }
        }

        [Fact]
        public async Task Execute_WhenNoLocationWithSearchRect_Throws()
        {
            var opts = new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" };
            using var driver = this.CreateDriver(opts);

            var needle = new FakeElement("needle");
            var expectedResult = new SearchResult(needle, Array.Empty<Rectangle>());
            this.ElementRecognizer.AddExpectedResult(needle, expectedResult);

            var searchRect = new Rectangle(10, 20, 210, 320);
            var ex = await Assert.ThrowsAsync<ElementNotFoundException>(() => driver.WaitFor(needle, searchRect: searchRect));
            Assert.Equal(needle, ex.Element);

            var failure = Assert.Single(this.FileWriter.SavedFailures);

            Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
            Assert.EndsWith("_needle.png", failure.Path);

            Assert.Equal(searchRect.Width, failure.Width);
            Assert.Equal(searchRect.Height, failure.Height);
        }

        [Fact]
        public async Task Execute_WhenTooManyLocationsWithSearchRect_Throws()
        {
            var opts = new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" };
            using var driver = this.CreateDriver(opts);

            var needle = new FakeElement("needle");
            var expectedResult = new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50), new Rectangle(100, 400, 200, 500) });
            this.ElementRecognizer.AddExpectedResult(needle, expectedResult);

            var searchRect = new Rectangle(5, 7, 450, 630);
            var ex = await Assert.ThrowsAsync<MultipleElementFoundException>(() => driver.WaitFor(needle, searchRect: searchRect));

            AssertSearchResult(expectedResult, ex.Result, searchRect);

            var failure = Assert.Single(this.FileWriter.SavedFailures);
            Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
            Assert.EndsWith("_needle.png", failure.Path);

            Assert.Equal(searchRect.Width, failure.Width);
            Assert.Equal(searchRect.Height, failure.Height);
        }

        private static void AssertSearchResult(SearchResult expected, SearchResult actual, Rectangle searchRect)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);

            expected = expected.AdjustToSearchRectangle(searchRect);

            Assert.Equal(expected.Success, actual.Success);
            Assert.Equal(expected.Element, actual.Element);

            Assert.Equal(expected.Locations.Count, actual.Locations.Count);

            for (var i = 0; i < expected.Locations.Count; i++)
            {
                Assert.Equal(expected.Locations[i], actual.Locations[i]);
            }
        }
    }
}
