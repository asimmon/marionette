using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public class MarionetteDriverTests_WaitForAny : BaseMarionetteDriverTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(FakeFailuresScreenshotPath)]
        public async Task WaitForAny_WhenOneSuccessfulResultWithinAllotedTime_Works1(string failureScreenshotPath)
        {
            var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
            using var driver = this.CreateDriver(opts);

            var needle1 = new FakeElement("needle1");
            var needle2 = new FakeElement("needle2");

            var expectedResult = new SearchResult(needle2, new[] { new Rectangle(50, 100, 70, 200) });

            this.ElementRecognizer.AddExpectedResult(needle1, SearchResult.NotFound(needle1));
            this.ElementRecognizer.AddExpectedResult(needle2, expectedResult);

            var actualResult = await driver.WaitForAny(new[] { needle1, needle2 }, waitFor: TimeSpan.FromSeconds(2));
            await Task.Delay(TimeSpan.FromSeconds(1));

            AssertSearchResult(expectedResult, actualResult);
            Assert.True(this.ElementRecognizer.RecognizeCallCount >= 1);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(FakeFailuresScreenshotPath)]
        public async Task WaitForAny_WhenImmediateNoResultAndResultAfterAllotedTime_Throws(string failureScreenshotPath)
        {
            var opts = string.IsNullOrEmpty(failureScreenshotPath) ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
            using var driver = this.CreateDriver(opts);

            var needle1 = new FakeElement("needle1");
            var needle2 = new FakeElement("needle2");

            var expectedResult = new SearchResult(needle2, new[] { new Rectangle(50, 100, 70, 200) });

            var recognizeCallCount = 0;
            this.ElementRecognizer.AddExpectedResult(needle1, () =>
            {
                Interlocked.Increment(ref recognizeCallCount);
                return Task.FromResult(SearchResult.NotFound(needle1));
            });

            this.ElementRecognizer.AddExpectedResult(needle2, async () =>
            {
                Interlocked.Increment(ref recognizeCallCount);
                await Task.Delay(TimeSpan.FromSeconds(2));
                return expectedResult;
            });

            var ex = await Assert.ThrowsAsync<ElementNotFoundException>(() => driver.WaitForAny(new[] { needle1, needle2 }, waitFor: TimeSpan.FromSeconds(0.5)));
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(needle1, ex.Element);
            Assert.True(recognizeCallCount > 2);
            Assert.Equal(recognizeCallCount, this.ElementRecognizer.RecognizeCallCount);

            if (string.IsNullOrEmpty(failureScreenshotPath))
            {
                Assert.Empty(this.FileWriter.SavedFailures);
            }
            else
            {
                var failure = Assert.Single(this.FileWriter.SavedFailures);
                var monitor = await this.MonitorService.GetMonitor(0);

                Assert.StartsWith(failureScreenshotPath, failure.Path);
                Assert.EndsWith("_needle1.png", failure.Path);

                Assert.Equal(monitor.Width, failure.Width);
                Assert.Equal(monitor.Height, failure.Height);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(FakeFailuresScreenshotPath)]
        public async Task WaitForAny_WhenOneSuccessfulResultWithinAllotedTime_Works2(string failureScreenshotPath)
        {
            var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
            using var driver = this.CreateDriver(opts);

            var needle1 = new FakeElement("needle1");
            var needle2 = new FakeElement("needle2");

            var expectedResult = new SearchResult(needle1, new[] { new Rectangle(50, 100, 70, 200) });

            var recognizeCallCount = 0;
            this.ElementRecognizer.AddExpectedResult(needle1, async () =>
            {
                Interlocked.Increment(ref recognizeCallCount);
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                return expectedResult;
            });

            this.ElementRecognizer.AddExpectedResult(needle2, async () =>
            {
                Interlocked.Increment(ref recognizeCallCount);
                await Task.Delay(TimeSpan.FromSeconds(0.1));
                return SearchResult.NotFound(needle2);
            });

            var actualResult = await driver.WaitForAny(new[] { needle1, needle2 }, waitFor: TimeSpan.FromSeconds(3));
            await Task.Delay(TimeSpan.FromSeconds(1));

            AssertSearchResult(expectedResult, actualResult);
            Assert.True(recognizeCallCount > 2);
            Assert.Equal(recognizeCallCount, this.ElementRecognizer.RecognizeCallCount);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(FakeFailuresScreenshotPath)]
        public async Task WaitForAny_OnlyNoResults_ThrowsAndSaveSingleFailure(string failureScreenshotPath)
        {
            var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
            using var driver = this.CreateDriver(opts);

            var needle1 = new FakeElement("needle1");
            var needle2 = new FakeElement("needle2");
            var needle3 = new FakeElement("needle3");

            var recognizeCallCount = 0;
            this.ElementRecognizer.AddExpectedResult(needle1, () =>
            {
                Interlocked.Increment(ref recognizeCallCount);
                return Task.FromResult(SearchResult.NotFound(needle1));
            });

            this.ElementRecognizer.AddExpectedResult(needle2, () =>
            {
                Interlocked.Increment(ref recognizeCallCount);
                return Task.FromResult(SearchResult.NotFound(needle2));
            });

            this.ElementRecognizer.AddExpectedResult(needle3, async () =>
            {
                Interlocked.Increment(ref recognizeCallCount);
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                return SearchResult.NotFound(needle3);
            });

            await Assert.ThrowsAsync<ElementNotFoundException>(() => driver.WaitForAny(new[] { needle1, needle2, needle3 }, waitFor: TimeSpan.FromSeconds(2)));
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.True(recognizeCallCount > 2);
            Assert.Equal(recognizeCallCount, this.ElementRecognizer.RecognizeCallCount);

            if (failureScreenshotPath == null)
            {
                Assert.Empty(this.FileWriter.SavedFailures);
            }
            else
            {
                Assert.Single(this.FileWriter.SavedFailures);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(FakeFailuresScreenshotPath)]
        public async Task WaitForAny_WhenUnexpectedException_Throws(string failureScreenshotPath)
        {
            var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
            using var driver = this.CreateDriver(opts);

            var needle1 = new FakeElement("needle1");
            var needle2 = new FakeElement("needle2");

            var recognizeCallCount = 0;
            this.ElementRecognizer.AddExpectedResult(needle1, () =>
            {
                Interlocked.Increment(ref recognizeCallCount);
                throw new InvalidOperationException("Yolo");
            });

            this.ElementRecognizer.AddExpectedResult(needle2, async () =>
            {
                Interlocked.Increment(ref recognizeCallCount);
                await Task.Delay(TimeSpan.FromSeconds(1));
                return SearchResult.NotFound(needle2);
            });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => driver.WaitForAny(new[] { needle1, needle2 }, waitFor: TimeSpan.FromSeconds(10)));
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal("Yolo", ex.Message);
            Assert.True(recognizeCallCount >= 1);
            Assert.Equal(recognizeCallCount, this.ElementRecognizer.RecognizeCallCount);

            Assert.Empty(this.FileWriter.SavedFailures);
        }
    }
}
