using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Askaiser.Marionette.Tests.Desktop;

/// <summary>
/// Desktop tests drive the real screen, mouse and keyboard of the machine. They only run when the
/// MARIONETTE_DESKTOP_TESTS environment variable is set to 1, which continuous integration does on every operating system.
/// </summary>
internal static class DesktopTestEnvironment
{
    public static bool IsEnabled => Environment.GetEnvironmentVariable("MARIONETTE_DESKTOP_TESTS") == "1";

    public static void SkipUnlessEnabled()
    {
        Skip.Unless(IsEnabled, "Desktop tests are disabled. Set MARIONETTE_DESKTOP_TESTS=1 to run them.");
    }

    /// <summary>
    /// Finds a Chromium-based browser to display the test page with. It can be forced with the MARIONETTE_TEST_BROWSER environment variable.
    /// </summary>
    public static string? FindBrowser()
    {
        if (Environment.GetEnvironmentVariable("MARIONETTE_TEST_BROWSER") is { Length: > 0 } configured)
        {
            return configured;
        }

        string[] candidates;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            candidates =
            [
                Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"),
            ];
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            candidates =
            [
                "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
                "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
                "/Applications/Chromium.app/Contents/MacOS/Chromium",
            ];
        }
        else
        {
            candidates = ["/usr/bin/google-chrome", "/usr/bin/google-chrome-stable", "/usr/bin/chromium", "/usr/bin/chromium-browser", "/snap/bin/chromium"];
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Opens the test page in full screen, in a browser profile of its own.
    /// </summary>
    public static Process StartBrowser(string browserPath, string pagePath, string profileDirectory)
    {
        var startInfo = new ProcessStartInfo(browserPath)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        string[] arguments =
        [
            "--kiosk",
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-search-engine-choice-screen",
            "--disable-session-crashed-bubble",
            "--disable-infobars",
            "--disable-features=Translate",
            "--window-position=0,0",
            "--user-data-dir=" + profileDirectory,
        ];

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Continuous integration runs as root in a virtual display without a graphics card.
            startInfo.ArgumentList.Add("--no-sandbox");
            startInfo.ArgumentList.Add("--disable-gpu");
        }

        startInfo.ArgumentList.Add(new Uri(pagePath).AbsoluteUri);

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("The browser could not be started.");

        // Nothing reads the output of the browser: discard it so that it never blocks on a full pipe.
        process.OutputDataReceived += static (_, _) => { };
        process.ErrorDataReceived += static (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }
}
