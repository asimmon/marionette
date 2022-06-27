using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests;

public class MarionetteDriverTests_WaitForAll : BaseMarionetteDriverTests
{
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
        Assert.True(recognizeCallCount >= 1);
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

        Assert.True(recognizeCallCount >= 1);
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
}
