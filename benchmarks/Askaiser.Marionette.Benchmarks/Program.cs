using System.Diagnostics;
using Askaiser.Marionette;
using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Recognition.Images;
using Askaiser.Marionette.Recognition.Text;

namespace Askaiser.Marionette.Benchmarks;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Contains("--live"))
        {
            return RunLiveAsync(args[0]).GetAwaiter().GetResult();
        }

        var screenshotPath = args.Length > 0 ? args[0] : "screen.png";
        var verify = args.Contains("--verify");
        var screen = Image.FromFile(screenshotPath);

        if (args.Contains("--text"))
        {
            return BenchmarkTextRecognition(screen);
        }

        Console.WriteLine($"Screen {screen.Width}x{screen.Height}, {Environment.ProcessorCount} logical cores, Vector<float>.Count={System.Numerics.Vector<float>.Count}");

        var cases = new (string Name, Rectangle Region, decimal Threshold, bool Grayscale)[]
        {
            ("button 112x26", new Rectangle(18, 96, 130, 122), 0.95m, false),
            ("button 112x26 gray", new Rectangle(18, 96, 130, 122), 0.95m, true),
            ("icon 24x24", new Rectangle(481, 97, 505, 121), 0.95m, false),
            ("word 70x16", new Rectangle(26, 177, 96, 193), 0.95m, false),
            ("card 244x114", new Rectangle(294, 199, 538, 313), 0.95m, false),
            ("panel 600x400", new Rectangle(280, 140, 880, 540), 0.95m, false),
            ("tiny 10x10", new Rectangle(457, 103, 467, 113), 0.95m, false),
            ("word thr 0.8", new Rectangle(26, 177, 96, 193), 0.80m, false),
            ("letter 9x12 thr 0.9", new Rectangle(28, 179, 37, 191), 0.90m, true),
        };

        var failures = 0;
        foreach (var (name, region, threshold, grayscale) in cases)
        {
            var element = new ImageElement(name, screen.Crop(region), threshold, grayscale);
            var template = element.GetTemplate();
            if (template == null)
            {
                Console.WriteLine($"{name}: solid color, skipped");
                continue;
            }

            // Cold: includes the analysis of the screenshot (color plane and block sums).
            var sw = Stopwatch.StartNew();
            List<TemplateMatch> matches;
            using (var surface = new SearchSurface(PixelBuffer.Wrap(screen.Pixels.ToArray(), screen.Width, screen.Height), ownsPixels: true))
            {
                matches = TemplateMatcher.Find(surface, template);
            }

            var cold = sw.Elapsed.TotalMilliseconds;

            // Warm: screenshot analysis shared with a previous search, as when several images are searched in the same screenshot.
            using var shared = new SearchSurface(PixelBuffer.Wrap(screen.Pixels.ToArray(), screen.Width, screen.Height), ownsPixels: true);
            TemplateMatcher.Find(shared, template);
            var best = double.MaxValue;
            for (var i = 0; i < 10; i++)
            {
                sw.Restart();
                matches = TemplateMatcher.Find(shared, template);
                best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
            }

            var statistics = new TemplateSearchStatistics();
            TemplateMatcher.Find(shared, template, statistics);
            Console.WriteLine($"    positions={statistics.Positions} passed=[{string.Join(" ", statistics.Passed)}]");
            var found = matches.Any(m => m.X == region.Left && m.Y == region.Top);
            Console.WriteLine($"{name,-22} plane={template.PlaneKind,-13} levels=[{string.Join(" ", template.Levels.Select(l => $"{l.BlockSize}:{l.KernelWidth}x{l.KernelHeight}@{l.Bound:F2}")),-38}] | cold {cold,7:F2} ms | warm {best,7:F2} ms | {matches.Count} match(es){(found ? string.Empty : "  <-- ORIGIN NOT FOUND")}");
            if (!found)
            {
                failures++;
            }

            if (verify)
            {
                failures += VerifyAgainstExhaustiveSearch(shared, template, matches) ? 0 : 1;
            }
        }

        return failures;
    }

    private static async Task<int> RunLiveAsync(string outputDirectory)
    {
        using var driver = MarionetteDriver.Create(new DriverOptions { FailureScreenshotPath = outputDirectory });
        var sw = Stopwatch.StartNew();

        foreach (var monitor in await driver.GetMonitorsAsync())
        {
            Console.WriteLine(monitor);
        }

        await driver.SaveScreenshotAsync(Path.Combine(outputDirectory, "live-before.png"));
        Console.WriteLine($"screenshot saved at {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        var result = await driver.WaitForAsync("Cancel", waitFor: TimeSpan.FromSeconds(5));
        Console.WriteLine($"found 'Cancel' at {result.Location} in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        await driver.SingleClickAsync("Export report");
        Console.WriteLine($"clicked 'Export report' in {sw.ElapsedMilliseconds} ms, mouse at {driver.GetMousePositionAsync()}");

        sw.Restart();
        await driver.SingleClickAsync("Cancel");
        await driver.WaitForAsync("Operation cancelled", waitFor: TimeSpan.FromSeconds(5));
        Console.WriteLine($"clicked 'Cancel' and saw the confirmation in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        await driver.SingleClickAsync("Email address");
        await driver.TypeTextAsync("Jürgen.Müller+test@example.com\tS3cr€t ✓");
        await driver.SingleClickAsync("Log in");
        var welcome = await driver.WaitForAsync("Welcome back, Jürgen.Müller+test@example.com", waitFor: TimeSpan.FromSeconds(5));
        Console.WriteLine($"typed, logged in and saw '{welcome.Element}' at {welcome.Location} in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        var before = await driver.GetScreenshotAsync();
        var button = new ImageElement("save-button", before.Crop(new Rectangle(18, 96, 130, 122)));
        await driver.SingleClickAsync(button);
        await driver.WaitForAsync("Changes saved successfully", waitFor: TimeSpan.FromSeconds(5));
        Console.WriteLine($"clicked an image and saw the confirmation in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        await driver.KeyPressAsync([VirtualKeyCode.ControlOrCommand, VirtualKeyCode.A]);
        Console.WriteLine($"is 'Not there' visible: {await driver.IsVisibleAsync("Not there")} ({sw.ElapsedMilliseconds} ms)");

        await driver.SaveScreenshotAsync(Path.Combine(outputDirectory, "live-after.png"));
        return 0;
    }

    private static int BenchmarkTextRecognition(Image screen)
    {
        using var engine = new TextRecognitionEngine(TextRecognitionModel.Default);
        using var pixels = PixelBuffer.Wrap(screen.Pixels.ToArray(), screen.Width, screen.Height);

        var sw = Stopwatch.StartNew();
        var rows = engine.Read(pixels, 1f);
        Console.WriteLine($"First read (loads the models): {sw.ElapsedMilliseconds} ms, {rows.Count} rows");

        // Changing a single pixel far from any text invalidates nothing: lines are recognized from cache.
        sw.Restart();
        rows = engine.Read(pixels, 1f);
        Console.WriteLine($"Second read (same text on screen): {sw.ElapsedMilliseconds} ms");

        foreach (var row in rows)
        {
            Console.WriteLine($"  [{row.Left:F0},{row.Top} - {row.Right:F0},{row.Bottom}] {row.Confidence:F2} \"{row.Text}\"");
        }

        foreach (var text in new[] { "Save changes", "File Edit", "jane.doe@example.com", "lazy dog", "Élodie", "await driver", "Not on screen" })
        {
            var found = TextFinder.Find(rows, text, ignoreCase: true);
            Console.WriteLine($"  search '{text}': {string.Join(", ", found)}");
        }

        return 0;
    }

    private static bool VerifyAgainstExhaustiveSearch(SearchSurface surface, ImageTemplate template, List<TemplateMatch> matches)
    {
        var sw = Stopwatch.StartNew();
        var expected = new System.Collections.Concurrent.ConcurrentBag<(int X, int Y, double Score)>();
        var positionsX = surface.Width - template.Width + 1;
        var positionsY = surface.Height - template.Height + 1;
        surface.GetPlane(ColorPlaneKind.Sum);

        Parallel.For(0, positionsY, y =>
        {
            for (var x = 0; x < positionsX; x++)
            {
                var score = TemplateMatcher.Score(surface, template, x, y);
                if (score >= template.Threshold - 1e-6)
                {
                    expected.Add((x, y, score));
                }
            }
        });

        // Every match must be an above-threshold position, and every above-threshold position must be within half a template of a match.
        var ok = true;
        foreach (var m in matches)
        {
            if (!expected.Any(e => e.X == m.X && e.Y == m.Y))
            {
                Console.WriteLine($"    unexpected match at {m.X},{m.Y} score {m.Score:F4}");
                ok = false;
            }
        }

        foreach (var e in expected)
        {
            if (!matches.Any(m => Math.Abs(m.X - e.X) < (template.Width + 1) / 2 && Math.Abs(m.Y - e.Y) < (template.Height + 1) / 2 && m.Score >= e.Score - 1e-9))
            {
                Console.WriteLine($"    missed position {e.X},{e.Y} score {e.Score:F4}");
                ok = false;
            }
        }

        Console.WriteLine($"    exhaustive search: {expected.Count} position(s) above threshold in {sw.Elapsed.TotalMilliseconds:F0} ms -> {(ok ? "identical" : "DIFFERENT")}");
        return ok;
    }
}
