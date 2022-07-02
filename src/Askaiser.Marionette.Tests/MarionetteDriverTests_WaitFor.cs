using System;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests;

public class MarionetteDriverTests_WaitFor : BaseMarionetteDriverTests
{
    [Fact]
    public async Task WaitFor_WhenNegativeTimeout_Throws()
    {
        using var driver = this.CreateDriver();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => driver.WaitForAsync(new FakeElement("needle"), waitFor: TimeSpan.FromSeconds(-1)));
        Assert.Equal(0, this.ElementRecognizer.RecognizeCallCount);
        Assert.Empty(this.FileWriter.SavedFailures);
    }

    [Fact]
    public async Task WaitFor_WhenSingleLocation_Works()
    {
        using var driver = this.CreateDriver();

        var needle = new FakeElement("needle");
        var expectedResult = this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50) }));

        var actualResult = await driver.WaitForAsync(needle);
        AssertSearchResult(expectedResult, actualResult);
        Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
        Assert.Empty(this.FileWriter.SavedFailures);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(null, 1000)]
    [InlineData(FakeFailuresScreenshotPath, 0)]
    [InlineData(FakeFailuresScreenshotPath, 1000)]
    public async Task WaitFor_WhenNoLocation_Throws(string failureScreenshotPath, int waitForMs)
    {
        var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
        using var driver = this.CreateDriver(opts);

        var needle = new FakeElement("needle");
        this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, Array.Empty<Rectangle>()));

        var ex = await Assert.ThrowsAsync<ElementNotFoundException>(() => driver.WaitForAsync(needle, waitFor: TimeSpan.FromMilliseconds(waitForMs)));

        Assert.Equal(needle, ex.Element);

        if (waitForMs == 0)
        {
            Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
        }
        else
        {
            Assert.True(this.ElementRecognizer.RecognizeCallCount >= 1);
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
    public async Task WaitFor_WhenTooManyLocations_Throws(string failureScreenshotPath, int waitForMs)
    {
        var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
        using var driver = this.CreateDriver(opts);

        var needle = new FakeElement("needle");
        var expectedResult = this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50), new Rectangle(100, 400, 200, 500) }));

        var ex = await Assert.ThrowsAsync<MultipleElementFoundException>(() => driver.WaitForAsync(needle, waitFor: TimeSpan.FromMilliseconds(waitForMs)));

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
    public async Task WaitFor_WhenNoLocationWithSearchRect_Throws()
    {
        var opts = new DriverOptions { FailureScreenshotPath = FakeFailuresScreenshotPath };
        using var driver = this.CreateDriver(opts);

        var needle = new FakeElement("needle");
        this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, Array.Empty<Rectangle>()));

        var searchRect = new Rectangle(10, 20, 210, 320);
        var ex = await Assert.ThrowsAsync<ElementNotFoundException>(() => driver.WaitForAsync(needle, searchRect: searchRect));
        Assert.Equal(needle, ex.Element);
        Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);

        var failure = Assert.Single(this.FileWriter.SavedFailures);

        Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
        Assert.EndsWith("_needle.png", failure.Path);

        Assert.Equal(searchRect.Width, failure.Width);
        Assert.Equal(searchRect.Height, failure.Height);
    }

    [Fact]
    public async Task WaitFor_WhenTooManyLocationsWithSearchRect_Throws()
    {
        var opts = new DriverOptions { FailureScreenshotPath = FakeFailuresScreenshotPath };
        using var driver = this.CreateDriver(opts);

        var needle = new FakeElement("needle");
        var expectedResult = this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50), new Rectangle(100, 400, 200, 500) }));

        var searchRect = new Rectangle(5, 7, 450, 630);
        var ex = await Assert.ThrowsAsync<MultipleElementFoundException>(() => driver.WaitForAsync(needle, searchRect: searchRect));

        AssertSearchResult(expectedResult, ex.Result, searchRect);
        Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);

        var failure = Assert.Single(this.FileWriter.SavedFailures);
        Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
        Assert.EndsWith("_needle.png", failure.Path);

        Assert.Equal(searchRect.Width, failure.Width);
        Assert.Equal(searchRect.Height, failure.Height);
    }
}
