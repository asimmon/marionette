using Askaiser.Marionette.Screen;
using Askaiser.Marionette.Tests.Fakes;

namespace Askaiser.Marionette.Tests;

// These tests record the screen in the background: running them alone keeps them independent of the load of the machine.
[NotInParallel]
public class MarionetteDriverFailureVideoTests
{
    private static readonly ScreenRecorderSettings FastRecording = ScreenRecorderSettings.Default with { Interval = TimeSpan.FromMilliseconds(20), MaxProcessorShare = 1 };

    [Test]
    public async Task WaitFor_WhenItFails_SavesAVideoOfTheLastMoments(CancellationToken cancellationToken)
    {
        var options = new DriverOptions { FailureVideoPath = "videos", FailureVideoDuration = TimeSpan.FromSeconds(30) };
        using var fixture = new DriverFixture(options, recorderSettings: FastRecording);
        var needle = new FakeElement("My Needle (1)");
        fixture.Recognizer.Setup(needle);

        await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.WaitForAsync(needle, waitFor: TimeSpan.FromMilliseconds(400), cancellationToken: cancellationToken));

        var (path, image, content) = await Assert.That(fixture.FailureWriter.Saved).HasSingleItem();
        await Assert.That(path).StartsWith("videos");
        await Assert.That(path).EndsWith("_my-needle-1.png");

        // The file is a PNG image of the whole monitor for programs that do not play animations...
        await Assert.That((image.Width, image.Height)).IsEqualTo((1920, 1080));

        // ... and a video for the others. The fake screen changes at every capture.
        var video = AnimatedPngReader.Read(content);
        await Assert.That((video.Width, video.Height)).IsEqualTo((1920, 1080));
        await Assert.That(video.Frames.Count).IsGreaterThan(2);
        await Assert.That(video.Duration).IsGreaterThan(TimeSpan.FromMilliseconds(350));

        // Both end with what the search saw last.
        await Assert.That(video.Frames[^1].Picture.Pixels.SequenceEqual(video.DefaultImage.Pixels)).IsTrue();
        await Assert.That(image.Pixels.SequenceEqual(video.DefaultImage.Pixels)).IsTrue();
    }

    [Test]
    public async Task WaitFor_WhenItFailsAfterTheMonitorChanged_SavesAVideoOfBothMonitors(CancellationToken cancellationToken)
    {
        // The recorder does not capture anything by itself here: videos are only made of what searches saw.
        var settings = ScreenRecorderSettings.Default with { Interval = TimeSpan.FromHours(1) };
        var options = new DriverOptions { FailureVideoPath = "videos", FailureVideoDuration = TimeSpan.FromMinutes(10) };
        using var fixture = new DriverFixture(options, recorderSettings: settings);
        fixture.Display.ChangesOnEveryCapture = false;

        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle);

        var monitors = await fixture.Driver.GetMonitorsAsync(cancellationToken);
        await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.WaitForAsync(needle, cancellationToken: cancellationToken));
        await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.SetCurrentMonitor(monitors[1]).WaitForAsync(needle, cancellationToken: cancellationToken));

        await Assert.That(fixture.FailureWriter.Saved.Count).IsEqualTo(2);

        var firstVideo = AnimatedPngReader.Read(fixture.FailureWriter.Saved[0].Content);
        var firstColor = GetColor(monitors[0]);
        await Assert.That(firstVideo.Frames.Count).IsEqualTo(1);
        await Assert.That(firstVideo.Frames[0].Picture.Pixels.Slice(0, 4).SequenceEqual(MakePixel(firstColor))).IsTrue();

        var secondVideo = AnimatedPngReader.Read(fixture.FailureWriter.Saved[1].Content);
        var secondColor = GetColor(monitors[1]);
        await Assert.That(secondVideo.Frames.Count).IsEqualTo(2);
        await Assert.That(secondVideo.Frames[0].Picture.Pixels.Slice(0, 4).SequenceEqual(MakePixel(firstColor))).IsTrue();
        await Assert.That(secondVideo.Frames[1].Picture.Pixels.Slice(0, 4).SequenceEqual(MakePixel(secondColor))).IsTrue();
        await Assert.That(secondVideo.Frames[1].Picture.Pixels.Slice(secondVideo.Frames[1].Picture.Pixels.Length - 4).SequenceEqual(MakePixel(secondColor))).IsTrue();
    }

    [Test]
    public async Task VisibilityChecksAndCanceledSearches_DoNotSaveVideos(CancellationToken cancellationToken)
    {
        var options = new DriverOptions { FailureVideoPath = "videos" };
        using var fixture = new DriverFixture(options, recorderSettings: FastRecording);
        var hidden = new FakeElement("hidden");
        fixture.Recognizer.Setup(hidden);

        await Assert.That(await fixture.Driver.IsVisibleAsync(hidden, cancellationToken: cancellationToken)).IsFalse();

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.WaitForAsync(hidden, waitFor: TimeSpan.FromMinutes(5), cancellationToken: cancellation.Token));

        await Assert.That(fixture.FailureWriter.Saved).IsEmpty();
    }

    [Test]
    public async Task Recording_CapturesTheScreenByItself_AndStopsWithTheDriver(CancellationToken cancellationToken)
    {
        var options = new DriverOptions { FailureVideoPath = "videos", ScreenshotCacheDuration = TimeSpan.FromMinutes(1) };
        var fixture = new DriverFixture(options, recorderSettings: FastRecording);

        try
        {
            // The recorder captures the screen by itself, whatever the lifetime of screenshots is for searches.
            while (fixture.Display.CaptureCount < 5)
            {
                await Task.Delay(10, cancellationToken);
            }
        }
        finally
        {
            fixture.Dispose();
        }

        var captureCount = fixture.Display.CaptureCount;
        await Task.Delay(200, cancellationToken);
        await Assert.That(fixture.Display.CaptureCount).IsEqualTo(captureCount);
    }

    [Test]
    public async Task Driver_WithBothFailureScreenshotsAndVideos_CannotBeCreated()
    {
        var options = new DriverOptions { FailureScreenshotPath = "screenshots", FailureVideoPath = "videos" };

        var exception = Assert.Throws<ArgumentException>(() => _ = new DriverFixture(options));
        await Assert.That(exception.Message).Contains("FailureVideoPath");

        Assert.Throws<ArgumentException>(() => _ = MarionetteDriver.Create(options));
    }

    [Test]
    public async Task Options_ValidateFailurePathsAndDuration()
    {
        Assert.Throws<ArgumentException>(() => _ = new DriverOptions { FailureVideoPath = "  " });
        Assert.Throws<ArgumentException>(() => _ = new DriverOptions { FailureScreenshotPath = string.Empty });
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new DriverOptions { FailureVideoDuration = TimeSpan.Zero });
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new DriverOptions { FailureVideoDuration = TimeSpan.FromSeconds(-1) });

        var options = new DriverOptions { FailureVideoPath = " videos ", FailureScreenshotPath = null, FailureVideoDuration = TimeSpan.FromSeconds(3) };
        await Assert.That(options.FailureVideoPath).IsEqualTo("videos");
        await Assert.That(options.FailureScreenshotPath).IsNull();
        await Assert.That(new DriverOptions().FailureVideoPath).IsNull();
        await Assert.That(new DriverOptions().FailureVideoDuration).IsEqualTo(TimeSpan.FromSeconds(5));
    }

    private static byte[] MakePixel(byte color) => [color, color, color, 0xFF];

    private static byte GetColor(MonitorDescription monitor)
    {
        var display = FakeDisplayDevice.Displays.Single(candidate => candidate.Bounds.Left == monitor.Left && candidate.Bounds.Top == monitor.Top);
        return FakeDisplayDevice.GetSolidColor(display);
    }
}
