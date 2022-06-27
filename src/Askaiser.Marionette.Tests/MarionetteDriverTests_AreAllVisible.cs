using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests;

public class MarionetteDriverTests_AreAllVisible : BaseMarionetteDriverTests
{
    [Fact]
    public async Task AreAllVisible_WhenNoResult_ReturnsFalse()
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
    public async Task AreAllVisible_WhenTimeout_ReturnsFalse()
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
    public async Task AreAllVisible_WhenSuccessfulResults_ReturnsTrue()
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
}
