using System;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public class MarionetteDriverTests : BaseMarionetteDriverTests
    {
        #region WaitFor with element

        [Fact]
        public async Task WaitFor_WhenNegativeTimeout_Throws()
        {
            using var driver = this.CreateDriver();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => driver.WaitFor(new FakeElement("needle"), waitFor: TimeSpan.FromSeconds(-1)));
            Assert.Equal(0, this.ElementRecognizer.RecognizeCallCount);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        [Fact]
        public async Task WaitFor_WhenSingleLocation_Works()
        {
            using var driver = this.CreateDriver();

            var needle = new FakeElement("needle");
            var expectedResult = this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50) }));

            var actualResult = await driver.WaitFor(needle);
            AssertSearchResult(expectedResult, actualResult);
            Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 0)]
        [InlineData(false, 1000)]
        [InlineData(true, 1000)]
        public async Task Execute_WhenNoLocation_Throws(bool useFailureScreenshotPath, int waitForMs)
        {
            var opts = useFailureScreenshotPath ? new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" } : new DriverOptions();
            using var driver = this.CreateDriver(opts);

            var needle = new FakeElement("needle");
            this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, Array.Empty<Rectangle>()));

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
            var expectedResult = this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50), new Rectangle(100, 400, 200, 500) }));

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
            this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, Array.Empty<Rectangle>()));

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
            var expectedResult = this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50), new Rectangle(100, 400, 200, 500) }));

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

        #endregion

        #region IsVisible with element

        [Fact]
        public async Task IsVisible_WhenNegativeTimeout_Throws()
        {
            using var driver = this.CreateDriver();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => driver.IsVisible(new FakeElement("needle"), waitFor: TimeSpan.FromSeconds(-1)));
            Assert.Equal(0, this.ElementRecognizer.RecognizeCallCount);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        [Fact]
        public async Task IsVisible_WhenSingleLocation_Works()
        {
            using var driver = this.CreateDriver();

            var needle = new FakeElement("needle");
            this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50) }));

            var isVisible = await driver.IsVisible(needle);
            Assert.True(isVisible);
            Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 0)]
        [InlineData(false, 1000)]
        [InlineData(true, 1000)]
        public async Task IsVisible_WhenNoLocation_Throws(bool useFailureScreenshotPath, int waitForMs)
        {
            var opts = useFailureScreenshotPath ? new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" } : new DriverOptions();
            using var driver = this.CreateDriver(opts);

            var needle = new FakeElement("needle");
            this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, Array.Empty<Rectangle>()));

            var isVisible = await driver.IsVisible(needle, waitFor: TimeSpan.FromMilliseconds(waitForMs));
            Assert.False(isVisible);

            if (waitForMs == 0)
            {
                Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
            }
            else
            {
                Assert.True(this.ElementRecognizer.RecognizeCallCount > 1);
            }

            Assert.Empty(this.FileWriter.SavedFailures);
        }

        [Theory]
        [InlineData(false, 1000)]
        [InlineData(true, 1000)]
        public async Task IsVisible_WhenTooManyLocations_Throws(bool useFailureScreenshotPath, int waitForMs)
        {
            var opts = useFailureScreenshotPath ? new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" } : new DriverOptions();
            using var driver = this.CreateDriver(opts);

            var needle = new FakeElement("needle");
            this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50), new Rectangle(100, 400, 200, 500) }));

            var isVisible = await driver.IsVisible(needle, waitFor: TimeSpan.FromMilliseconds(waitForMs));
            Assert.True(isVisible);
            Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        #endregion

        #region WaitForAny

        [Fact]
        public async Task WaitForAny_WhenSingleLocation_Works()
        {
            using var driver = this.CreateDriver();

            var needle1 = new FakeElement("needle1");
            var needle2 = new FakeElement("needle2");

            var expectedResult1 = this.ElementRecognizer.AddExpectedResult(needle1, new SearchResult(needle1, new[] { new Rectangle(10, 40, 20, 50) }));
            var expectedResult2 = this.ElementRecognizer.AddExpectedResult(needle2, new SearchResult(needle2, new[] { new Rectangle(50, 100, 70, 200) }));

            var actualResult = await driver.WaitForAny(new[] { needle1, needle2 });
            var expectedResult = actualResult.Element == needle1 ? expectedResult1 : expectedResult2;
            AssertSearchResult(expectedResult, actualResult);
            Assert.True(this.ElementRecognizer.RecognizeCallCount >= 1);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        #endregion
    }
}
