using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public class MarionetteDriverTests : BaseMarionetteDriverTests
    {
        #region Fields and constants

        private const string FakeFailuresScreenshotPath = "C:\\foo\\bar\\";

        #endregion

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
        [InlineData(null, 0)]
        [InlineData(null, 1000)]
        [InlineData(FakeFailuresScreenshotPath, 0)]
        [InlineData(FakeFailuresScreenshotPath, 1000)]
        public async Task Execute_WhenNoLocation_Throws(string failureScreenshotPath, int waitForMs)
        {
            var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
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

            if (opts.FailureScreenshotPath != null)
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
        [InlineData(null, 1000)]
        [InlineData(FakeFailuresScreenshotPath, 1000)]
        public async Task Execute_WhenTooManyLocations_Throws(string failureScreenshotPath, int waitForMs)
        {
            var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
            using var driver = this.CreateDriver(opts);

            var needle = new FakeElement("needle");
            var expectedResult = this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50), new Rectangle(100, 400, 200, 500) }));

            var ex = await Assert.ThrowsAsync<MultipleElementFoundException>(() => driver.WaitFor(needle, waitFor: TimeSpan.FromMilliseconds(waitForMs)));

            AssertSearchResult(expectedResult, ex.Result);
            Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);

            if (opts.FailureScreenshotPath != null)
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
            var opts = new DriverOptions { FailureScreenshotPath = FakeFailuresScreenshotPath };
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
            var opts = new DriverOptions { FailureScreenshotPath = FakeFailuresScreenshotPath };
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
        [InlineData(null, 0)]
        [InlineData(null, 1000)]
        [InlineData(FakeFailuresScreenshotPath, 0)]
        [InlineData(FakeFailuresScreenshotPath, 1000)]
        public async Task IsVisible_WhenNoLocation_Throws(string failureScreenshotPath, int waitForMs)
        {
            var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
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
        [InlineData(null, 1000)]
        [InlineData(FakeFailuresScreenshotPath, 1000)]
        public async Task IsVisible_WhenTooManyLocations_Throws(string failureScreenshotPath, int waitForMs)
        {
            var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
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
                await Task.Delay(TimeSpan.FromSeconds(1));
                return expectedResult;
            });

            this.ElementRecognizer.AddExpectedResult(needle2, async () =>
            {
                Interlocked.Increment(ref recognizeCallCount);
                await Task.Delay(TimeSpan.FromSeconds(0.5));
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

        #endregion

        #region WaitForAll

        [Theory]
        [InlineData(null)]
        [InlineData(FakeFailuresScreenshotPath)]
        public async Task WaitForAll_WhenImmediateNoResultAndResultAfterAllotedTime_Throws(string failureScreenshotPath)
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

            var ex = await Assert.ThrowsAsync<ElementNotFoundException>(() => driver.WaitForAll(new[] { needle1, needle2 }, waitFor: TimeSpan.FromSeconds(0.5)));
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
                Assert.Equal(2, this.FileWriter.SavedFailures.Count);

                foreach (var failure in this.FileWriter.SavedFailures)
                {
                    var monitor = await this.MonitorService.GetMonitor(0);

                    Assert.StartsWith(failureScreenshotPath, failure.Path);
                    Assert.Contains("_needle", failure.Path);

                    Assert.Equal(monitor.Width, failure.Width);
                    Assert.Equal(monitor.Height, failure.Height);
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(FakeFailuresScreenshotPath)]
        public async Task WaitForAll_OnlyNoResults_ThrowsAndSaveSingleFailure(string failureScreenshotPath)
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

            await Assert.ThrowsAsync<ElementNotFoundException>(() => driver.WaitForAll(new[] { needle1, needle2, needle3 }, waitFor: TimeSpan.FromSeconds(2)));
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.True(recognizeCallCount > 2);
            Assert.Equal(recognizeCallCount, this.ElementRecognizer.RecognizeCallCount);

            if (failureScreenshotPath == null)
            {
                Assert.Empty(this.FileWriter.SavedFailures);
            }
            else
            {
                Assert.Equal(3, this.FileWriter.SavedFailures.Count);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(FakeFailuresScreenshotPath)]
        public async Task WaitForAll_WhenUnexpectedException_Throws(string failureScreenshotPath)
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

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => driver.WaitForAll(new[] { needle1, needle2 }, waitFor: TimeSpan.FromSeconds(10)));
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal("Yolo", ex.Message);
            Assert.True(recognizeCallCount >= 1);
            Assert.Equal(recognizeCallCount, this.ElementRecognizer.RecognizeCallCount);

            Assert.Empty(this.FileWriter.SavedFailures);
        }

        [Fact]
        public async Task WaitForAll_WhenSuccessfulResults_Works()
        {
            using var driver = this.CreateDriver();

            var needle1 = new FakeElement("needle1");
            var needle2 = new FakeElement("needle2");
            var needle3 = new FakeElement("needle3");

            var expectedResult1 = new SearchResult(needle1, new[] { new Rectangle(50, 100, 70, 200) });
            var expectedResult2 = new SearchResult(needle2, new[] { new Rectangle(10, 75, 660, 230) });
            var expectedResult3 = new SearchResult(needle3, new[] { new Rectangle(0, 120, 20, 450) });

            this.ElementRecognizer.AddExpectedResult(needle1, expectedResult1);
            this.ElementRecognizer.AddExpectedResult(needle2, expectedResult2);
            this.ElementRecognizer.AddExpectedResult(needle3, expectedResult3);

            var searchRect = new Rectangle(20, 20, 50, 70);
            var result = await driver.WaitForAll(new[] { needle1, needle2, needle3 }, searchRect: searchRect);

            var actualResult1 = Assert.Single(result, x => expectedResult1.Element == x.Element);
            var actualResult2 = Assert.Single(result, x => expectedResult2.Element == x.Element);
            var actualResult3 = Assert.Single(result, x => expectedResult3.Element == x.Element);

            Assert.Empty(this.FileWriter.SavedFailures);

            AssertSearchResult(expectedResult1, actualResult1, searchRect);
            AssertSearchResult(expectedResult2, actualResult2, searchRect);
            AssertSearchResult(expectedResult3, actualResult3, searchRect);
        }

        #endregion

        #region AreAllVisible

        [Fact]
        public async Task AreAllVisible_WhenNoResult_Throws()
        {
            var opts = new DriverOptions { FailureScreenshotPath = FakeFailuresScreenshotPath };
            using var driver = this.CreateDriver(opts);

            var needle1 = new FakeElement("needle1");
            var needle2 = new FakeElement("needle2");

            this.ElementRecognizer.AddExpectedResult(needle1, SearchResult.NotFound(needle1));
            this.ElementRecognizer.AddExpectedResult(needle2, new SearchResult(needle2, new[] { new Rectangle(50, 100, 70, 200) }));

            var result = await driver.AreAllVisible(new[] { needle1, needle2 });

            Assert.False(result);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        [Fact]
        public async Task AreAllVisible_WhenTimeout_Throws()
        {
            var opts = new DriverOptions { FailureScreenshotPath = FakeFailuresScreenshotPath };
            using var driver = this.CreateDriver(opts);

            var needle1 = new FakeElement("needle1");
            var needle2 = new FakeElement("needle2");

            var callCount = 0;
            this.ElementRecognizer.AddExpectedResult(needle1, new SearchResult(needle1, new[] { new Rectangle(50, 100, 70, 200) }));
            this.ElementRecognizer.AddExpectedResult(needle2, async () =>
            {
                if (Interlocked.Increment(ref callCount) == 1)
                {
                    // First call returns not found
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    return SearchResult.NotFound(needle1);
                }

                return new SearchResult(needle2, new[] { new Rectangle(10, 75, 660, 230) });
            });

            var result = await driver.AreAllVisible(new[] { needle1, needle2 }, waitFor: TimeSpan.FromSeconds(1));

            Assert.False(result);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        [Fact]
        public async Task AreAllVisible_WhenSuccessfulResults_Works()
        {
            var opts = new DriverOptions { FailureScreenshotPath = FakeFailuresScreenshotPath };
            using var driver = this.CreateDriver(opts);

            var needle1 = new FakeElement("needle1");
            var needle2 = new FakeElement("needle2");

            this.ElementRecognizer.AddExpectedResult(needle1, new SearchResult(needle1, new[] { new Rectangle(50, 100, 70, 200) }));
            this.ElementRecognizer.AddExpectedResult(needle2, new SearchResult(needle2, new[] { new Rectangle(10, 75, 660, 230) }));

            var result = await driver.AreAllVisible(new[] { needle1, needle2 }, waitFor: TimeSpan.FromSeconds(0.5));

            Assert.True(result);
            Assert.Empty(this.FileWriter.SavedFailures);
        }

        #endregion
    }
}
