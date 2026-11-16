using System.Diagnostics;

namespace Askaiser.Marionette.ConsoleApp;

public static class Program
{
    public static async Task Main()
    {
        var options = new DriverOptions
        {
            // When an element cannot be found, a screenshot of what was searched is saved here
            FailureScreenshotPath = Path.Combine(Path.GetTempPath(), "marionette-sample-failures"),
            MouseSpeed = MouseSpeed.Fast,
        };

        // Ctrl+C stops the sample, even in the middle of a search or of a mouse move
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };

        var cancellationToken = cancellation.Token;

        using var driver = MarionetteDriver.Create(options);

        // 1) Describe the desktop
        foreach (var monitor in await driver.GetMonitorsAsync(cancellationToken))
        {
            Console.WriteLine(monitor);
        }

        var currentMonitor = await driver.GetCurrentMonitorAsync(cancellationToken);

        // 2) Look for the logo of an IDE in the top left corner of the current monitor, and move the cursor over it if it is there.
        // Searching a small part of the screen is much faster than searching all of it.
        var topLeftCorner = currentMonitor.FromTopLeft(Math.Min(400, currentMonitor.Width), Math.Min(200, currentMonitor.Height));
        var ideLogos = new[] { MyLibrary.Instance.RiderLogo }.Concat(MyLibrary.Instance.VsLogo).ToArray();

        if (await driver.IsAnyVisibleAsync(ideLogos, searchRect: topLeftCorner, cancellationToken: cancellationToken))
        {
            await driver.MoveToAnyAsync(ideLogos, searchRect: topLeftCorner, cancellationToken: cancellationToken);
            Console.WriteLine("Found the logo of an IDE.");
        }
        else
        {
            Console.WriteLine("No IDE logo in the top left corner of the screen.");
        }

        // 3) Read the screen: find every occurrence of a word, wherever it is
        var watch = Stopwatch.StartNew();
        var locations = await driver.FindLocationsAsync("File", cancellationToken: cancellationToken);
        Console.WriteLine($"The text 'File' is visible {locations.Length} time(s), found in {watch.ElapsedMilliseconds} ms.");

        foreach (var location in locations)
        {
            await driver.MoveToAsync(location.Center, cancellationToken);
        }

        // 4) Save what the current monitor displays
        var screenshotPath = Path.Combine(Path.GetTempPath(), "marionette-sample.png");
        await driver.SaveScreenshotAsync(screenshotPath, cancellationToken);
        Console.WriteLine($"Screenshot saved to {screenshotPath}");
    }
}
