using TUnit.Assertions.Enums;
using System.Diagnostics;
using Askaiser.Marionette.Tests.Fakes;

namespace Askaiser.Marionette.Tests;

public class MarionetteDriverSearchTests
{
    private static readonly Rectangle Location = new(50, 100, 70, 200);
    private static readonly Rectangle OtherLocation = new(500, 600, 520, 700);

    [Test]
    public async Task WaitFor_WhenElementIsVisible_ReturnsItsLocation()
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, Location);

        var result = await fixture.Driver.WaitForAsync(needle);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Element).IsSameReferenceAs(needle);
        await Assert.That(result.Location).IsEqualTo(Location);
        await Assert.That(fixture.Recognizer.CallCount).IsEqualTo(1);
    }

    [Test]
    public async Task WaitFor_WhenElementIsNotVisible_ThrowsAfterASingleAttempt()
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle);

        var exception = await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.WaitForAsync(needle));

        await Assert.That(exception!.Element).IsSameReferenceAs(needle);
        await Assert.That(fixture.Recognizer.CallCount).IsEqualTo(1);
        await Assert.That(fixture.FailureWriter.Saved).IsEmpty();
    }

    [Test]
    public async Task WaitFor_WhenElementShowsUpLater_KeepsLooking()
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, attempt => attempt >= 4 ? [Location] : []);

        var result = await fixture.Driver.WaitForAsync(needle, waitFor: TimeSpan.FromSeconds(10));

        await Assert.That(result.Location).IsEqualTo(Location);
        await Assert.That(fixture.Recognizer.CallCount).IsEqualTo(4);
    }

    [Test]
    public async Task WaitFor_WhenElementNeverShowsUp_ThrowsAfterTheAllottedTime()
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle);

        var watch = Stopwatch.StartNew();
        await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.WaitForAsync(needle, waitFor: TimeSpan.FromMilliseconds(400)));

        await Assert.That(watch.Elapsed >= TimeSpan.FromMilliseconds(350)).IsTrue().Because($"Waited for {watch.Elapsed}");
        await Assert.That(fixture.Recognizer.CallCount > 1).IsTrue();
    }

    [Test]
    public async Task WaitFor_WhenElementIsVisibleSeveralTimes_Throws()
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, Location, OtherLocation);

        var exception = await Assert.ThrowsAsync<MultipleElementFoundException>(() => fixture.Driver.WaitForAsync(needle));

        await Assert.That(exception!.Result.Locations).IsEquivalentTo([Location, OtherLocation], CollectionOrdering.Matching);
    }

    [Test]
    public async Task WaitFor_WhenWaitForIsNegative_Throws()
    {
        using var fixture = new DriverFixture();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Driver.WaitForAsync(new FakeElement("needle"), waitFor: TimeSpan.FromSeconds(-1)));
    }

    [Test]
    public async Task WaitFor_WhenElementKindIsNotSupported_Throws()
    {
        using var fixture = new DriverFixture();

        // The fixture only knows how to find fake elements.
        await Assert.ThrowsAsync<NotSupportedException>(() => fixture.Driver.WaitForAsync(new TextElement("some text")));
    }

    [Test]
    public async Task WaitFor_UsesTheDefaultDuration()
    {
        using var fixture = new DriverFixture(new DriverOptions { DefaultWaitForDuration = TimeSpan.FromSeconds(10) });
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, attempt => attempt >= 3 ? [Location] : []);

        var result = await fixture.Driver.WaitForAsync(needle);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(fixture.Recognizer.CallCount).IsEqualTo(3);
    }

    [Test]
    public async Task WaitFor_WhenScreenDoesNotChange_SearchesOnlyOnce()
    {
        using var fixture = new DriverFixture(new DriverOptions { ScreenshotCacheDuration = TimeSpan.Zero });
        fixture.Display.ChangesOnEveryCapture = false;

        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle);

        await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.WaitForAsync(needle, waitFor: TimeSpan.FromMilliseconds(400)));

        // The screen was captured at every attempt, but as it never changed there was nothing new to search.
        await Assert.That(fixture.Display.CaptureCount > 1).IsTrue();
        await Assert.That(fixture.Recognizer.CallCount).IsEqualTo(1);
    }

    [Test]
    public async Task WaitFor_WhenScreenshotIsRecent_DoesNotCaptureAgain()
    {
        using var fixture = new DriverFixture(new DriverOptions { ScreenshotCacheDuration = TimeSpan.FromMinutes(1) });
        var first = new FakeElement("first");
        var second = new FakeElement("second");
        fixture.Recognizer.Setup(first, Location);
        fixture.Recognizer.Setup(second, OtherLocation);

        await fixture.Driver.WaitForAsync(first);
        await fixture.Driver.WaitForAsync(second);

        await Assert.That(fixture.Display.CaptureCount).IsEqualTo(1);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    public async Task WaitFor_OnAnyMonitor_ReturnsDesktopCoordinates(int monitorIndex)
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, Location);

        var monitor = (await fixture.Driver.GetMonitorsAsync())[monitorIndex];
        var result = await fixture.Driver.SetCurrentMonitor(monitor).WaitForAsync(needle);

        await Assert.That(result.Location).IsEqualTo(Location + (monitor.Left, monitor.Top));
    }

    [Test]
    public async Task WaitFor_WithSearchRectangle_SearchesThatPartOfTheMonitor()
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, new Rectangle(10, 20, 30, 40));

        var monitor = (await fixture.Driver.GetMonitorsAsync())[4];
        var searchRect = new Rectangle(monitor.Left + 100, monitor.Top + 200, monitor.Left + 400, monitor.Top + 600);

        var result = await fixture.Driver.SetCurrentMonitor(monitor).WaitForAsync(needle, searchRect: searchRect);

        // The recognizer only saw the rectangle, and its result is relative to it.
        await Assert.That(fixture.Recognizer.LastSurfaceSize).IsEqualTo((300, 400));
        await Assert.That(result.Location).IsEqualTo(new Rectangle(searchRect.Left + 10, searchRect.Top + 20, searchRect.Left + 30, searchRect.Top + 40));
    }

    [Test]
    public async Task WaitFor_WhenSearchRectangleExceedsTheMonitor_SearchesTheVisiblePart()
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, new Rectangle(0, 0, 10, 10));

        await fixture.Driver.WaitForAsync(needle, searchRect: new Rectangle(1800, 1000, 2500, 1500));

        await Assert.That(fixture.Recognizer.LastSurfaceSize).IsEqualTo((120, 80));
    }

    [Test]
    public async Task WaitFor_WhenSearchRectangleIsOutsideTheMonitor_Throws()
    {
        using var fixture = new DriverFixture();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Driver.WaitForAsync(new FakeElement("needle"), searchRect: new Rectangle(5000, 5000, 5100, 5100)));
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task WaitFor_WhenItFails_SavesWhatWasSearched(bool withSearchRectangle)
    {
        using var fixture = new DriverFixture(new DriverOptions { FailureScreenshotPath = "failures" });
        var needle = new FakeElement("My Needle (1)");
        fixture.Recognizer.Setup(needle);

        var searchRect = withSearchRectangle ? new Rectangle(100, 200, 400, 600) : null;
        await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.WaitForAsync(needle, searchRect: searchRect));

        var (path, image) = await Assert.That(fixture.FailureWriter.Saved).HasSingleItem();
        await Assert.That(path).StartsWith("failures");
        await Assert.That(path).EndsWith("_my-needle-1.png");
        await Assert.That((image.Width, image.Height)).IsEqualTo(withSearchRectangle ? (300, 400) : (1920, 1080));
    }

    [Test]
    public async Task WaitFor_WhenElementIsAmbiguous_SavesAFailureScreenshot()
    {
        using var fixture = new DriverFixture(new DriverOptions { FailureScreenshotPath = "failures" });
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, Location, OtherLocation);

        await Assert.ThrowsAsync<MultipleElementFoundException>(() => fixture.Driver.WaitForAsync(needle));

        await Assert.That(fixture.FailureWriter.Saved).HasSingleItem();
    }

    [Test]
    public async Task WaitForAny_ReturnsTheFirstVisibleElement()
    {
        using var fixture = new DriverFixture();
        var hidden = new FakeElement("hidden");
        var visible = new FakeElement("visible");
        var alsoVisible = new FakeElement("also-visible");
        fixture.Recognizer.Setup(hidden);
        fixture.Recognizer.Setup(visible, Location);
        fixture.Recognizer.Setup(alsoVisible, OtherLocation);

        var result = await fixture.Driver.WaitForAnyAsync([hidden, visible, alsoVisible], waitFor: TimeSpan.FromSeconds(10));

        await Assert.That(result.Element).IsSameReferenceAs(visible);
        await Assert.That(result.Location).IsEqualTo(Location);
    }

    [Test]
    public async Task WaitForAny_WhenAnElementShowsUpLater_ReturnsIt()
    {
        using var fixture = new DriverFixture();
        var hidden = new FakeElement("hidden");
        var late = new FakeElement("late");
        fixture.Recognizer.Setup(hidden);
        fixture.Recognizer.Setup(late, attempt => attempt >= 3 ? [Location] : []);

        var result = await fixture.Driver.WaitForAnyAsync([hidden, late], waitFor: TimeSpan.FromSeconds(10));

        await Assert.That(result.Element).IsSameReferenceAs(late);
        await Assert.That(fixture.Recognizer.GetCallCount(hidden)).IsEqualTo(3);
    }

    [Test]
    public async Task WaitForAny_WhenNothingIsVisible_ThrowsAndSavesASingleFailure()
    {
        using var fixture = new DriverFixture(new DriverOptions { FailureScreenshotPath = "failures" });
        var first = new FakeElement("first");
        var second = new FakeElement("second");
        fixture.Recognizer.Setup(first);
        fixture.Recognizer.Setup(second);

        var exception = await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.WaitForAnyAsync([first, second], waitFor: TimeSpan.FromMilliseconds(200)));

        await Assert.That(exception!.Element).IsSameReferenceAs(first);
        await Assert.That(fixture.FailureWriter.Saved).HasSingleItem();
    }

    [Test]
    public async Task WaitForAny_WhenARecognizerFails_Throws()
    {
        using var fixture = new DriverFixture();
        var broken = new FakeElement("broken");
        var visible = new FakeElement("visible");
        fixture.Recognizer.Setup(broken, _ => throw new InvalidOperationException("Yolo"));
        fixture.Recognizer.Setup(visible, Location);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Driver.WaitForAnyAsync([broken, visible]));
        await Assert.That(exception!.Message).IsEqualTo("Yolo");
    }

    [Test]
    public async Task WaitForAny_WhenNoElements_Throws()
    {
        using var fixture = new DriverFixture();
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Driver.WaitForAnyAsync([]));
    }

    [Test]
    public async Task WaitForAll_ReturnsEveryElement()
    {
        using var fixture = new DriverFixture();
        var first = new FakeElement("first");
        var second = new FakeElement("second");
        fixture.Recognizer.Setup(first, Location);
        fixture.Recognizer.Setup(second, OtherLocation);

        var results = await fixture.Driver.WaitForAllAsync([first, second]);

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results[first].Location).IsEqualTo(Location);
        await Assert.That(results["second"].Location).IsEqualTo(OtherLocation);
    }

    [Test]
    public async Task WaitForAll_WaitsForTheLastElement_WithoutSearchingTheOthersAgain()
    {
        using var fixture = new DriverFixture();
        var immediate = new FakeElement("immediate");
        var late = new FakeElement("late");
        fixture.Recognizer.Setup(immediate, Location);
        fixture.Recognizer.Setup(late, attempt => attempt >= 4 ? [OtherLocation] : []);

        var results = await fixture.Driver.WaitForAllAsync([immediate, late], waitFor: TimeSpan.FromSeconds(10));

        await Assert.That(results[immediate].Success).IsTrue();
        await Assert.That(results[late].Success).IsTrue();
        await Assert.That(fixture.Recognizer.GetCallCount(immediate)).IsEqualTo(1);
        await Assert.That(fixture.Recognizer.GetCallCount(late)).IsEqualTo(4);
    }

    [Test]
    public async Task WaitForAll_WhenAnElementIsMissing_Throws()
    {
        using var fixture = new DriverFixture(new DriverOptions { FailureScreenshotPath = "failures" });
        var visible = new FakeElement("visible");
        var missing = new FakeElement("missing");
        fixture.Recognizer.Setup(visible, Location);
        fixture.Recognizer.Setup(missing);

        var exception = await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.WaitForAllAsync([visible, missing], waitFor: TimeSpan.FromMilliseconds(200)));

        await Assert.That(exception!.Element).IsSameReferenceAs(missing);
        await Assert.That(fixture.FailureWriter.Saved).HasSingleItem();
    }

    [Test]
    public async Task VisibilityChecks_DoNotThrow()
    {
        using var fixture = new DriverFixture(new DriverOptions { FailureScreenshotPath = "failures" });
        var visible = new FakeElement("visible");
        var hidden = new FakeElement("hidden");
        var ambiguous = new FakeElement("ambiguous");
        fixture.Recognizer.Setup(visible, Location);
        fixture.Recognizer.Setup(hidden);
        fixture.Recognizer.Setup(ambiguous, Location, OtherLocation);

        await Assert.That(await fixture.Driver.IsVisibleAsync(visible)).IsTrue();
        await Assert.That(await fixture.Driver.IsVisibleAsync(hidden)).IsFalse();
        await Assert.That(await fixture.Driver.IsVisibleAsync(ambiguous)).IsTrue();

        await Assert.That(await fixture.Driver.IsAnyVisibleAsync([hidden, visible])).IsTrue();
        await Assert.That(await fixture.Driver.IsAnyVisibleAsync([hidden])).IsFalse();

        await Assert.That(await fixture.Driver.AreAllVisibleAsync([visible, ambiguous])).IsTrue();
        await Assert.That(await fixture.Driver.AreAllVisibleAsync([visible, hidden])).IsFalse();

        await Assert.That(await fixture.Driver.FindLocationsAsync(ambiguous)).IsEquivalentTo([Location, OtherLocation], CollectionOrdering.Matching);
        await Assert.That(await fixture.Driver.FindLocationsAsync(hidden)).IsEmpty();

        await Assert.That(fixture.FailureWriter.Saved).IsEmpty();
    }

    [Test]
    public async Task GetMonitors_ReturnsThePrimaryMonitorFirst_ThenFromLeftToRightAndTopToBottom()
    {
        using var fixture = new DriverFixture();

        var monitors = await fixture.Driver.GetMonitorsAsync();

        await Assert.That(monitors.Select(monitor => monitor.Index)).IsEquivalentTo([0, 1, 2, 3, 4], CollectionOrdering.Matching);
        Point[] expectedOrigins = [new Point(0, 0), new Point(-1920, 0), new Point(0, -1080), new Point(0, 1080), new Point(1920, 0)];
        await Assert.That(monitors.Select(monitor => monitor.TopLeft)).IsEquivalentTo(expectedOrigins, CollectionOrdering.Matching);
        await Assert.That(monitors[0].IsPrimary).IsTrue();
        await Assert.That(await fixture.Driver.GetCurrentMonitorAsync()).IsEqualTo(monitors[0]);
    }

    [Test]
    public async Task GetCurrentMonitor_WhenMonitorDoesNotExist_Throws()
    {
        using var fixture = new DriverFixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Driver.SetCurrentMonitor(12).GetCurrentMonitorAsync());
    }

    [Test]
    public async Task GetScreenshot_ReturnsWhatTheCurrentMonitorDisplays()
    {
        using var fixture = new DriverFixture();
        fixture.Display.ChangesOnEveryCapture = false;
        fixture.Display.Content = TestAssets.TestBench;

        var screenshot = await fixture.Driver.GetScreenshotAsync();

        await Assert.That(screenshot.Pixels.SequenceEqual(TestAssets.TestBench.Pixels)).IsTrue();

        using var stream = new MemoryStream();
        await fixture.Driver.SaveScreenshotAsync(stream);
        await Assert.That(Image.FromBytes(stream.ToArray()).Pixels.SequenceEqual(TestAssets.TestBench.Pixels)).IsTrue();
    }
}
