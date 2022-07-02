using System;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests;

public class MarionetteDriverTests_IsVisible : BaseMarionetteDriverTests
{
    [Fact]
    public async Task IsVisible_WhenNegativeTimeout_Throws()
    {
        using var driver = this.CreateDriver();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => driver.IsVisibleAsync(new FakeElement("needle"), waitFor: TimeSpan.FromSeconds(-1)));
        Assert.Equal(0, this.ElementRecognizer.RecognizeCallCount);
        Assert.Empty(this.FileWriter.SavedFailures);
    }

    [Fact]
    public async Task IsVisible_WhenSingleLocation_ReturnsTrue()
    {
        using var driver = this.CreateDriver();

        var needle = new FakeElement("needle");
        this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50) }));

        var isVisible = await driver.IsVisibleAsync(needle);
        Assert.True(isVisible);
        Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
        Assert.Empty(this.FileWriter.SavedFailures);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(null, 1000)]
    [InlineData(FakeFailuresScreenshotPath, 0)]
    [InlineData(FakeFailuresScreenshotPath, 1000)]
    public async Task IsVisible_WhenNoLocation_ReturnsFalse(string failureScreenshotPath, int waitForMs)
    {
        var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
        using var driver = this.CreateDriver(opts);

        var needle = new FakeElement("needle");
        this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, Array.Empty<Rectangle>()));

        var isVisible = await driver.IsVisibleAsync(needle, waitFor: TimeSpan.FromMilliseconds(waitForMs));
        Assert.False(isVisible);

        if (waitForMs == 0)
        {
            Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
        }
        else
        {
            Assert.True(this.ElementRecognizer.RecognizeCallCount >= 1);
        }

        Assert.Empty(this.FileWriter.SavedFailures);
    }

    [Theory]
    [InlineData(null, 1000)]
    [InlineData(FakeFailuresScreenshotPath, 1000)]
    public async Task IsVisible_WhenTooManyLocations_ReturnsTrue(string failureScreenshotPath, int waitForMs)
    {
        var opts = failureScreenshotPath == null ? new DriverOptions() : new DriverOptions { FailureScreenshotPath = failureScreenshotPath };
        using var driver = this.CreateDriver(opts);

        var needle = new FakeElement("needle");
        this.ElementRecognizer.AddExpectedResult(needle, new SearchResult(needle, new[] { new Rectangle(10, 40, 20, 50), new Rectangle(100, 400, 200, 500) }));

        var isVisible = await driver.IsVisibleAsync(needle, waitFor: TimeSpan.FromMilliseconds(waitForMs));
        Assert.True(isVisible);
        Assert.Equal(1, this.ElementRecognizer.RecognizeCallCount);
        Assert.Empty(this.FileWriter.SavedFailures);
    }
}
