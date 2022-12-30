using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.Marionette.ConsoleApp;

[ImageLibrary("images", singleton: true)]
public partial class MyLibrary
{
}

public static class Program
{
    public static async Task Main()
    {
        await AutomaticallyGeneratedLibrary();
        await ManuallyCreatedLibrary();
    }

    private static async Task AutomaticallyGeneratedLibrary()
    {
        using (var driver = MarionetteDriver.Create())
        {
            // We expect the IDE logo to be in a 200x200 square at the top left of the current monitor.
            var monitor = await driver.GetCurrentMonitorAsync();
            var ideLogoRect = monitor.FromTopLeft(200, 200);

            // RiderLogo and VsLogo properties will be generated from the *.png files in the images directory specified in the MyLibrary class definition
            // VsLogo is an array because there are multiple images suffixed with "_n" (vs-logo_0.png, vs-logo_1.png)
            var ideLogos = new[] { MyLibrary.Instance.RiderLogo }.Concat(MyLibrary.Instance.VsLogo);
            await driver.MoveToAnyAsync(ideLogos, waitFor: TimeSpan.FromSeconds(2), searchRect: ideLogoRect);

            // Also, in the same area, we expect to find the toolbar item "Edit". Negative preprocessing should be used if the IDE use a dark theme.
            await driver.MoveToAsync("Edit", searchRect: ideLogoRect, textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);
        }
    }

    private static async Task ManuallyCreatedLibrary()
    {
        var library = new ElementCollection();

        var currentDirectoryPath = AppContext.BaseDirectory;
        var serializedLibraryPath = Path.Combine(currentDirectoryPath, "library.json");

        if (File.Exists(serializedLibraryPath))
        {
            await library.LoadAsync(serializedLibraryPath);
        }
        else
        {
            var riderLogoPath = Path.Combine(currentDirectoryPath, "images", "rider-logo.png");
            var vsLogoPath = Path.Combine(currentDirectoryPath, "images", "vs-logo.png");

            var riderLogoBytes = await File.ReadAllBytesAsync(riderLogoPath);
            var vsLogoBytes = await File.ReadAllBytesAsync(vsLogoPath);

            library.Add(new ImageElement(name: "rider-logo", content: riderLogoBytes, threshold: 0.95m, grayscale: false));
            library.Add(new ImageElement(name: "vs-logo", content: vsLogoBytes, threshold: 0.95m, grayscale: false));

            await library.SaveAsync(serializedLibraryPath);
        }

        using (var driver = MarionetteDriver.Create())
        {
            await driver.MoveToAnyAsync(new[] { library["rider-logo"], library["vs-logo"] }, waitFor: TimeSpan.FromSeconds(2));
        }
    }
}
