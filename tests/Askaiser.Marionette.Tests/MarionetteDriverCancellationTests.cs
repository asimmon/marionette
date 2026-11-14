using System.Diagnostics;
using System.Text;
using Askaiser.Marionette.Input;
using Askaiser.Marionette.Tests.Fakes;
using TUnit.Assertions.Enums;

namespace Askaiser.Marionette.Tests;

// These tests measure durations: running them alone keeps them independent of the load of the machine.
[NotInParallel]
public class MarionetteDriverCancellationTests
{
    private static readonly Rectangle Location = new(50, 100, 70, 200);

    /// <summary>Far longer than any of these tests should take: a test that waits this long did not notice its cancellation.</summary>
    private static readonly TimeSpan Forever = TimeSpan.FromMinutes(5);

    [Test]
    public async Task WaitFor_WhenAlreadyCanceled_DoesNotLookAtTheScreen(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture(new DriverOptions { FailureScreenshotPath = "failures" });
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, Location);

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.WaitForAsync(needle, cancellationToken: cancellation.Token));

        await Assert.That(fixture.Display.CaptureCount).IsEqualTo(0);
        await Assert.That(fixture.Recognizer.CallCount).IsEqualTo(0);
        await Assert.That(fixture.FailureWriter.Saved).IsEmpty();
    }

    [Test]
    public async Task WaitFor_WhenCanceledWhileWaiting_StopsRightAway(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture(new DriverOptions { FailureScreenshotPath = "failures" });
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle);

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(200));

        var watch = Stopwatch.StartNew();
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.WaitForAsync(needle, waitFor: Forever, cancellationToken: cancellation.Token));

        await Assert.That(watch.Elapsed).IsLessThan(TimeSpan.FromSeconds(30));
        await Assert.That(fixture.Recognizer.CallCount).IsGreaterThan(1);

        // A canceled search did not fail: there is nothing to explain with a screenshot.
        await Assert.That(fixture.FailureWriter.Saved).IsEmpty();
    }

    [Test]
    public async Task WaitFor_WhenCanceledDuringASearch_StopsTheSearch(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        var searching = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        fixture.Recognizer.Setup(needle, (_, searchToken) =>
        {
            searching.SetResult();
            searchToken.WaitHandle.WaitOne(Forever);
            searchToken.ThrowIfCancellationRequested();
            return [Location];
        });

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var search = fixture.Driver.WaitForAsync(needle, cancellationToken: cancellation.Token);

        await searching.Task.WaitAsync(cancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => search);
    }

    [Test]
    public async Task WaitFor_WhenAnotherCallerCancelsTheSearchTheyShare_SearchesAgain(CancellationToken cancellationToken)
    {
        // Both callers look for the same element in the same screenshot, which is only searched once.
        using var fixture = new DriverFixture(new DriverOptions { ScreenshotCacheDuration = TimeSpan.FromMinutes(1) });
        var needle = new FakeElement("needle");
        var searching = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        fixture.Recognizer.Setup(needle, (attempt, searchToken) =>
        {
            if (attempt == 1)
            {
                searching.SetResult();
                searchToken.WaitHandle.WaitOne(Forever);
                searchToken.ThrowIfCancellationRequested();
            }

            return [Location];
        });

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var canceledSearch = fixture.Driver.WaitForAsync(needle, cancellationToken: cancellation.Token);
        await searching.Task.WaitAsync(cancellationToken);

        var otherSearch = fixture.Driver.WaitForAsync(needle, cancellationToken: cancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => canceledSearch);
        var result = await otherSearch;

        await Assert.That(result.Location).IsEqualTo(Location);
        await Assert.That(fixture.Recognizer.CallCount).IsEqualTo(2);
        await Assert.That(fixture.Display.CaptureCount).IsEqualTo(1);
    }

    [Test]
    public async Task WaitForAll_WhenCanceled_Throws(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        var visible = new FakeElement("visible");
        var missing = new FakeElement("missing");
        fixture.Recognizer.Setup(visible, Location);
        fixture.Recognizer.Setup(missing);

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.WaitForAllAsync([visible, missing], waitFor: Forever, cancellationToken: cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.IsAnyVisibleAsync([missing], waitFor: Forever, cancellationToken: cancellation.Token));
    }

    [Test]
    public async Task SingleClick_WhenCanceledWhileTheButtonIsDown_ReleasesIt(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture(new DriverOptions { MouseSpeed = MouseSpeed.Immediate }, InputTimings.Instant with { ButtonHold = Forever });
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        fixture.Mouse.OnEvent = mouseEvent =>
        {
            if (mouseEvent == "Press Left")
            {
                cancellation.Cancel();
            }
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.SingleClickAsync(10, 20, cancellation.Token));

        await Assert.That(fixture.Mouse.Events).IsEquivalentTo(["Move 10,20", "Press Left", "Release Left"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task DoubleClick_WhenCanceledBetweenClicks_DoesNotClickAgain(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture(new DriverOptions { MouseSpeed = MouseSpeed.Immediate }, InputTimings.Instant with { BetweenClicks = Forever });
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        fixture.Mouse.OnEvent = mouseEvent =>
        {
            if (mouseEvent == "Release Left")
            {
                cancellation.Cancel();
            }
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.DoubleClickAsync(10, 20, cancellation.Token));

        await Assert.That(fixture.Mouse.Events).IsEquivalentTo(["Move 10,20", "Press Left", "Release Left"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task MoveTo_WhenCanceled_StopsOnTheWay(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture(new DriverOptions { MouseSpeed = MouseSpeed.Slow }, InputTimings.Instant with { SlowMove = Forever, MoveStep = TimeSpan.FromMilliseconds(5) });
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        fixture.Mouse.OnEvent = _ => cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.MoveToAsync(1000, 800, cancellation.Token));

        await Assert.That(fixture.Mouse.Events).HasSingleItem();
        await Assert.That(fixture.Driver.GetMousePositionAsync()).IsNotEqualTo(new Point(1000, 800));
    }

    [Test]
    public async Task SingleClick_OnAnElement_WhenAlreadyCanceled_DoesNotTouchTheMouse(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture(new DriverOptions { MouseSpeed = MouseSpeed.Immediate });
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, Location);

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.SingleClickAsync(needle, cancellationToken: cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.SingleClickAsync(10, 20, cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.ScrollDownAsync(3, cancellation.Token));

        await Assert.That(fixture.Mouse.Events).IsEmpty();
    }

    [Test]
    public async Task KeyPress_WhenCanceledDuringAShortcut_ReleasesItsKeys(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        fixture.Keyboard.OnEvent = keyboardEvent =>
        {
            if (keyboardEvent == "Press C")
            {
                cancellation.Cancel();
            }
        };

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => fixture.Driver.KeyPressAsync([VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.C, VirtualKeyCode.V], cancellationToken: cancellation.Token));

        // The key that was down came back up, the next one was not pressed, and no modifier key stayed down.
        string[] expected = ["Press Control", "Press Shift", "Press C", "Release C", "Release Shift", "Release Control"];
        await Assert.That(fixture.Keyboard.Events).IsEquivalentTo(expected, CollectionOrdering.Matching);
    }

    [Test]
    public async Task TypeText_WhenCanceled_StopsBeforeTheNextKey(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        fixture.Keyboard.OnEvent = keyboardEvent =>
        {
            if (keyboardEvent == "Type user")
            {
                cancellation.Cancel();
            }
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.TypeTextAsync("user\tsecret", cancellationToken: cancellation.Token));

        await Assert.That(fixture.Keyboard.Events).IsEquivalentTo(["Type user"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task TypeText_WhenCanceledDuringThePauseThatFollows_Throws(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        fixture.Keyboard.OnEvent = _ => cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.TypeTextAsync("user", sleepAfter: Forever, cancellationToken: cancellation.Token));
        await Assert.That(fixture.Keyboard.Events).IsEquivalentTo(["Type user"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ScrollDownUntilVisible_WhenCanceled_StopsScrolling(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle);

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(200));

        var watch = Stopwatch.StartNew();
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.ScrollDownUntilVisibleAsync(needle, Forever, cancellationToken: cancellation.Token));
        await Assert.That(watch.Elapsed).IsLessThan(TimeSpan.FromSeconds(30));
    }

    [Test]
    public async Task Sleep_WhenCanceled_Throws(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.SleepAsync(Forever, cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.SleepAsync(300_000, cancellation.Token));
    }

    [Test]
    public async Task ScreenMethods_WhenAlreadyCanceled_Throw(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.GetMonitorsAsync(cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.GetCurrentMonitorAsync(cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.GetScreenshotAsync(cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.SaveScreenshotAsync(new MemoryStream(), cancellation.Token));

        await Assert.That(fixture.Display.CaptureCount).IsEqualTo(0);
    }

    [Test]
    public async Task ElementCollection_WhenAlreadyCanceled_IsNotLoadedNorSaved(CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await cancellation.CancelAsync();

        var collection = new ElementCollection();
        collection.Add(new TextElement("lorem", "Lorem ipsum", TextOptions.None));

        await Assert.ThrowsAsync<OperationCanceledException>(() => collection.SaveAsync(new MemoryStream(), cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => collection.LoadAsync(new MemoryStream(Encoding.UTF8.GetBytes("[]")), cancellation.Token));

        await Assert.That(collection.Count).IsEqualTo(1);
    }

    [Test]
    public async Task WaitFor_AfterInputThatHappenedDuringACapture_CapturesTheScreenAgain(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture(new DriverOptions { ScreenshotCacheDuration = TimeSpan.FromMinutes(1), MouseSpeed = MouseSpeed.Immediate });
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, Location);

        using var capturing = new ManualResetEventSlim();
        using var resume = new ManualResetEventSlim();

        fixture.Display.OnCapture = () =>
        {
            if (fixture.Display.CaptureCount == 1)
            {
                capturing.Set();
                resume.Wait(cancellationToken);
            }
        };

        // The mouse is moved while the screen is being captured: that capture may not show what the move changed.
        var screenshot = fixture.Driver.GetScreenshotAsync(cancellationToken);
        capturing.Wait(cancellationToken);
        await fixture.Driver.MoveToAsync(10, 20, cancellationToken);
        resume.Set();
        await screenshot;

        await fixture.Driver.WaitForAsync(needle, cancellationToken: cancellationToken);

        await Assert.That(fixture.Display.CaptureCount).IsEqualTo(2);
    }
}
