using System.Diagnostics;

namespace Askaiser.Marionette.Tests.Desktop;

/// <summary>
/// End-to-end tests on the real desktop: they capture the screen, read it, and control the mouse and the keyboard.
/// </summary>
[NotInParallel]
public sealed class DesktopTests
{
    private static readonly TimeSpan PageTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ReactionTimeout = TimeSpan.FromSeconds(15);

    [Test]
    public async Task Monitors_AreListed_WithThePrimaryOneFirst()
    {
        DesktopTestEnvironment.SkipUnlessEnabled();
        using var driver = MarionetteDriver.Create();

        var monitors = await driver.GetMonitorsAsync();
        var current = await driver.GetCurrentMonitorAsync();

        await Assert.That(monitors).IsNotEmpty();
        await Assert.That(monitors.Select(monitor => monitor.Index)).IsEquivalentTo(Enumerable.Range(0, monitors.Length), TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(current).IsEqualTo(monitors[0]);
        await Assert.That(current.Width).IsGreaterThan(0);
        await Assert.That(current.Height).IsGreaterThan(0);
    }

    [Test]
    public async Task Screenshot_HasTheSizeOfTheMonitor_AndIsOpaque()
    {
        DesktopTestEnvironment.SkipUnlessEnabled();
        using var driver = MarionetteDriver.Create();

        var monitor = await driver.GetCurrentMonitorAsync();
        var screenshot = await driver.GetScreenshotAsync();

        await Assert.That(screenshot.Width).IsEqualTo(monitor.Width);
        await Assert.That(screenshot.Height).IsEqualTo(monitor.Height);

        var hasTransparency = false;
        for (var i = 3; i < screenshot.Pixels.Length && !hasTransparency; i += 4)
        {
            hasTransparency = screenshot.Pixels[i] != 255;
        }

        await Assert.That(hasTransparency).IsFalse();
    }

    [Test]
    public async Task Mouse_MovesToTheRequestedPosition()
    {
        DesktopTestEnvironment.SkipUnlessEnabled();
        using var driver = MarionetteDriver.Create(new DriverOptions { MouseSpeed = MouseSpeed.Immediate });
        var monitor = await driver.GetCurrentMonitorAsync();

        foreach (var (x, y) in new[] { (monitor.Left + 200, monitor.Top + 150), (monitor.Left + (monitor.Width / 2), monitor.Top + (monitor.Height / 2)), (monitor.Left + 31, monitor.Top + 57) })
        {
            await driver.MoveToAsync(x, y);
            await driver.SleepAsync(100);

            var position = driver.GetMousePositionAsync();

            // High pixel density screens position the cursor in points, which can round a pixel away.
            await Assert.That(Math.Abs(position.X - x)).IsLessThanOrEqualTo(1);
            await Assert.That(Math.Abs(position.Y - y)).IsLessThanOrEqualTo(1);
        }

        await driver.SetMouseSpeed(MouseSpeed.Fast).MoveToAsync(monitor.Left + 300, monitor.Top + 300);
        var final = driver.GetMousePositionAsync();
        await Assert.That(Math.Abs(final.X - (monitor.Left + 300))).IsLessThanOrEqualTo(1);
        await Assert.That(Math.Abs(final.Y - (monitor.Top + 300))).IsLessThanOrEqualTo(1);
    }

    [Test]
    public async Task UserInterface_CanBeDrivenWithTextAndImages()
    {
        DesktopTestEnvironment.SkipUnlessEnabled();

        var browserPath = DesktopTestEnvironment.FindBrowser();
        Skip.Unless(browserPath != null, "No Chromium-based browser was found to display the test page.");

        var failuresDirectory = Path.Combine(AppContext.BaseDirectory, "desktop-failures");
        var profileDirectory = Path.Combine(Path.GetTempPath(), "marionette-tests-" + Guid.NewGuid().ToString("N"));
        using var browser = DesktopTestEnvironment.StartBrowser(browserPath!, TestAssets.GetPath("test-bench.html"), profileDirectory);

        try
        {
            using var driver = MarionetteDriver.Create(new DriverOptions { FailureScreenshotPath = failuresDirectory });

            // 1) Read the screen
            await driver.WaitForAsync("Account overview", waitFor: PageTimeout);
            await Assert.That(await driver.IsVisibleAsync("Recent orders")).IsTrue();
            await Assert.That(await driver.IsVisibleAsync("This text is not on the page")).IsFalse();

            // 2) Click on text and observe the reaction of the page
            await driver.SingleClickAsync("Cancel");
            await driver.WaitForAsync("Operation cancelled", waitFor: ReactionTimeout);

            // 3) Type text, including characters that are not on a US keyboard, and a tabulation to change field
            await driver.SingleClickAsync("Email address");
            await driver.TypeTextAsync("Jürgen.Müller@example.com\tsecret", sleepAfter: TimeSpan.FromMilliseconds(300));
            await driver.SingleClickAsync("Log in");
            await driver.WaitForAsync("Welcome back, Jürgen.Müller@example.com", waitFor: ReactionTimeout);

            // 4) Find an image taken from the screen itself, and click on it
            var saveButton = await driver.WaitForAsync("Save changes", searchRect: (await driver.GetCurrentMonitorAsync()).FromTop(300));
            var screenshot = await driver.GetScreenshotAsync();
            var monitor = await driver.GetCurrentMonitorAsync();
            var buttonImage = screenshot.Crop(saveButton.Location - (monitor.Left, monitor.Top));

            var imageLocation = await driver.WaitForAsync(buttonImage);
            await Assert.That(imageLocation.Location).IsEqualTo(saveButton.Location);

            await driver.SingleClickAsync(buttonImage);
            await driver.WaitForAsync("Changes saved successfully", waitFor: ReactionTimeout);

            // 5) Keyboard shortcut: select the content of the email field and replace it
            await driver.SingleClickAsync("Jürgen.Müller@example.com", searchRect: (await driver.GetCurrentMonitorAsync()).FromTop(700));
            await driver.KeyPressAsync([VirtualKeyCode.ControlOrCommand, VirtualKeyCode.A]);
            await driver.TypeTextAsync("replaced@example.com", sleepAfter: TimeSpan.FromMilliseconds(300));
            await driver.SingleClickAsync("Log in");
            await driver.WaitForAsync("Welcome back, replaced@example.com", waitFor: ReactionTimeout);
        }
        finally
        {
            try
            {
                browser.Kill(entireProcessTree: true);
                await browser.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                Directory.Delete(profileDirectory, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or OperationCanceledException)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
