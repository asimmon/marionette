namespace Askaiser.Marionette.ConsoleApp;

[ImageLibrary("images")]
public partial class MyLibrary
{
}

public static class Program
{
    public static async Task Main()
    {
        var options = new DriverOptions
        {
            DefaultKeyboardSleepAfterDuration = TimeSpan.FromSeconds(0.5),
            FailureScreenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "consoleapp-failures-screenshots"),
            MouseSpeed = MouseSpeed.Fast,
        };

        using (var driver = MarionetteDriver.Create(options))
        {
            // 1) Move the cursor to the currently opened IDE logo (Rider or Visual Studio)
            // We expect the IDE logo to be in a 200x200 square at the top left of the current monitor.
            var monitor = await driver.GetCurrentMonitorAsync();
            var ideLogoSearchRect = monitor.FromTopLeft(200, 200);

            // RiderLogo and VsLogo properties will be generated from the *.png files in the images directory specified in the MyLibrary class' attribute
            // VsLogo is an array because there are multiple images suffixed with "_n" (vs-logo_0.png, vs-logo_1.png)
            var riderOrVsLogos = new[] { MyLibrary.Instance.RiderLogo }.Concat(MyLibrary.Instance.VsLogo);
            await driver.MoveToAnyAsync(riderOrVsLogos, searchRect: ideLogoSearchRect);

            // 2) Open Run command window, type "Hello world", then locate this text and move the cursor above it
            await driver.KeyPressAsync(new[] { VirtualKeyCode.LeftWindows, VirtualKeyCode.R });
            await driver.TypeTextAsync("Hello world");

            // We expect the Run command window to appear at the bottom left of the current monitor
            var runCommandSearchRect = monitor.FromBottomLeft(400, 250);
            await driver.MoveToAsync("Hello world", searchRect: runCommandSearchRect);

            // Close the Run command window
            await driver.KeyPressAsync(VirtualKeyCode.Escape);
        }
    }
}
