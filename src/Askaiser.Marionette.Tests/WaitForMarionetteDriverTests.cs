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
            Assert.Equal(0, this.ElementRecognizer.RecognizeCallCount);
        }

        [Fact]
        public async Task WaitFor_WhenSingleLocation_Works()
        {
            using var driver = this.CreateDriver();

            var needle = new FakeElement("needle");
            var expectedResult = new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50) });
            this.ElementRecognizer.AddExpectedResult(needle, expectedResult);

            var actualResult = await driver.WaitFor(needle);
            AssertSearchResult(expectedResult, actualResult);
            Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 0)]
        [InlineData(false, 1000)]
        [InlineData(true,  1000)]
        public async Task Execute_WhenNoLocation_Throws(bool useFailureScreenshotPath, int waitForMs)
        {
            var opts = useFailureScreenshotPath ? new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" } : new DriverOptions();
            using var driver = this.CreateDriver(opts);

            var needle = new FakeElement("needle");
            var expectedResult = new SearchResult(needle, Array.Empty<Rectangle>());
            this.ElementRecognizer.AddExpectedResult(needle, expectedResult);

            var ex = await Assert.ThrowsAsync<ElementNotFoundException>(() => driver.WaitFor(needle, waitFor: TimeSpan.FromMilliseconds(waitForMs)));

            Assert.Equal(needle, ex.Element);

            if (waitForMs == 0)
            {
                Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
            }
            else
            {
                Assert.True(this.ElementRecognizer.RecognizeCallCount > 1);
            }

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
        [InlineData(false, 1000)]
        [InlineData(true, 1000)]
        public async Task Execute_WhenTooManyLocations_Throws(bool useFailureScreenshotPath, int waitForMs)
        {
            var opts = useFailureScreenshotPath ? new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" } : new DriverOptions();
            using var driver = this.CreateDriver(opts);

            var needle = new FakeElement("needle");
            var expectedResult = new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50), new Rectangle(100, 400, 200, 500) });
            this.ElementRecognizer.AddExpectedResult(needle, expectedResult);

            var ex = await Assert.ThrowsAsync<MultipleElementFoundException>(() => driver.WaitFor(needle, waitFor: TimeSpan.FromMilliseconds(waitForMs)));

            AssertSearchResult(expectedResult, ex.Result);
            Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);

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
            Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);

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
            Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);

            var failure = Assert.Single(this.FileWriter.SavedFailures);
            Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
            Assert.EndsWith("_needle.png", failure.Path);

            Assert.Equal(searchRect.Width, failure.Width);
            Assert.Equal(searchRect.Height, failure.Height);
        }
    }
}
