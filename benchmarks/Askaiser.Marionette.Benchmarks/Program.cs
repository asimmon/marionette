using System.Diagnostics;
using System.Globalization;
using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Recognition.Images;
using Askaiser.Marionette.Recognition.Text;
using Askaiser.Marionette.Screen;

namespace Askaiser.Marionette.Benchmarks;

/// <summary>
/// Measures image recognition, text recognition and screen recording on a screenshot, without needing a desktop.
/// Usage: dotnet run -c Release --project benchmarks/Askaiser.Marionette.Benchmarks [path/to/screenshot.png] [path/to/video.png]
/// </summary>
public static class Program
{
    private const int Iterations = 20;

    public static void Main(string[] args)
    {
        var screen = Image.FromFile(args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "test-bench.png"));
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Screenshot of {screen.Width}x{screen.Height} pixels, {Environment.ProcessorCount} logical processors"));
        Console.WriteLine();

        BenchmarkImageRecognition(screen);
        BenchmarkScreenRecording(screen, args.Length > 1 ? args[1] : null);
        BenchmarkTextRecognition(screen);
    }

    private static void BenchmarkImageRecognition(Image screen)
    {
        // Parts of the screenshot of various sizes, searched in the whole screenshot.
        (string Name, Rectangle Region, decimal Threshold, bool Grayscale)[] cases =
        [
            ("icon, 24x24", Scale(screen, 441, 87, 465, 111), 0.95m, false),
            ("word, 65x16", Scale(screen, 18, 167, 83, 183), 0.95m, false),
            ("button, 112x26", Scale(screen, 8, 85, 120, 111), 0.95m, false),
            ("button, 112x26, grayscale", Scale(screen, 8, 85, 120, 111), 0.95m, true),
            ("button, 112x26, threshold 0.8", Scale(screen, 8, 85, 120, 111), 0.80m, false),
            ("card, 244x114", Scale(screen, 284, 189, 528, 303), 0.95m, false),
            ("panel, 600x400", Scale(screen, 280, 130, 880, 530), 0.95m, false),
        ];

        Console.WriteLine("Image recognition                    first search    next searches    exact scores");

        foreach (var (name, region, threshold, grayscale) in cases)
        {
            var template = new ImageElement(name, screen.Crop(region), threshold, grayscale).GetTemplate();
            if (template == null)
            {
                Console.WriteLine($"{name,-34}  skipped, this part of the screenshot is a solid color");
                continue;
            }

            // First search in a screenshot: includes preparing the screenshot.
            var watch = Stopwatch.StartNew();
            using (var surface = CreateSurface(screen))
            {
                TemplateMatcher.Find(surface, template);
            }

            var first = watch.Elapsed.TotalMilliseconds;

            // Next searches: the screenshot is already prepared, like when several images are searched in the same screenshot.
            using var prepared = CreateSurface(screen);
            TemplateMatcher.Find(prepared, template);

            var statistics = new TemplateSearchStatistics();
            var next = double.MaxValue;
            var matches = 0;
            for (var i = 0; i < Iterations; i++)
            {
                watch.Restart();
                matches = TemplateMatcher.Find(prepared, template, i == 0 ? statistics : null).Count;
                next = Math.Min(next, watch.Elapsed.TotalMilliseconds);
            }

            var scored = statistics.Passed[^2];
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{name,-34}  {first,9:F1} ms    {next,10:F1} ms    {scored,7} of {statistics.Positions} positions, {matches} found"));
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Records what a test typically does to a screen, and measures what each capture costs and what the video weighs.
    /// </summary>
    private static void BenchmarkScreenRecording(Image screen, string? videoPath)
    {
        using var device = new NoDisplayDevice();
        using var screenService = new ScreenService(device, TimeSpan.Zero);
        using var recorder = new ScreenRecorder(screenService, TimeSpan.FromMinutes(1), ScreenRecorderSettings.Default with { MaxPixels = int.MaxValue }, TimeProvider.System);

        var display = new DisplayInfo(new Rectangle(0, 0, screen.Width, screen.Height), IsPrimary: true, 1f, 0);
        var sequence = 0L;
        var pixels = screen.Pixels.ToArray();

        Console.WriteLine("Screen recording                         time per capture");

        double Record(string name, Action<byte[]> change, int captures)
        {
            var total = 0d;
            for (var i = 0; i < captures; i++)
            {
                change(pixels);

                var buffer = PixelBuffer.Rent(screen.Width, screen.Height);
                pixels.CopyTo(buffer.Span);
                var frame = new ScreenFrame(buffer, display, ++sequence);

                var watch = Stopwatch.StartNew();
                recorder.Record(frame);
                total += watch.Elapsed.TotalMilliseconds;
                frame.Release();

                // Videos are paced by the clock: leave some time between two captures.
                Thread.Sleep(20);
            }

            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{name,-38}  {total / captures,9:F2} ms"));
            return total;
        }

        var stride = screen.Width * 4;
        var step = 0;

        Record("first capture", static _ => { }, 1);
        Record("same screen, compared", static _ => { }, 10);
        Record("cursor moves, 32x32 changed", current =>
        {
            // A square that moves along a diagonal, like a cursor: its previous position changes too.
            step++;
            for (var y = 0; y < 32; y++)
            {
                current.AsSpan((((step * 9) + y) * stride) + (step * 16 * 4), 32 * 4).Fill((byte)(step * 37));
            }
        }, 20);
        Record("text typed, 300x20 changed", current =>
        {
            step++;
            for (var y = 0; y < 20; y++)
            {
                var row = current.AsSpan(((400 + y) * stride) + (500 * 4), 300 * 4);
                for (var x = 0; x < row.Length; x += 4)
                {
                    row[x] = row[x + 1] = row[x + 2] = (byte)(((x / 4) + step) % 7 == 0 ? 0 : 255);
                }
            }
        }, 20);
        Record("page scrolled, whole screen changed", current =>
        {
            // Everything moves up by 40 rows.
            current.AsSpan(40 * stride).CopyTo(current);
        }, 5);

        using var video = new MemoryStream();
        var encodeWatch = Stopwatch.StartNew();
        recorder.TryEncode(video);
        var encodeTime = encodeWatch.Elapsed.TotalMilliseconds;

        var screenshotSize = screen.ToPng().Length;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"video of these {sequence} captures written in {encodeTime:F0} ms: {video.Length / 1024} KB, a screenshot is {screenshotSize / 1024} KB"));
        Console.WriteLine();

        if (videoPath != null)
        {
            File.WriteAllBytes(videoPath, video.ToArray());
        }
    }

    private static void BenchmarkTextRecognition(Image screen)
    {
        using var engine = new TextRecognitionEngine(TextRecognitionModel.Default);
        using var pixels = PixelBuffer.Wrap(screen.Pixels.ToArray(), screen.Width, screen.Height);

        var watch = Stopwatch.StartNew();
        var rows = engine.Read(pixels, 1f);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Text recognition: {rows.Count} rows of text read in {watch.ElapsedMilliseconds} ms, including loading the models"));

        // Reading the same screen again only locates text: what was read is remembered by its pixels.
        var best = long.MaxValue;
        for (var i = 0; i < 3; i++)
        {
            watch.Restart();
            engine.Read(pixels, 1f);
            best = Math.Min(best, watch.ElapsedMilliseconds);
        }

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Text recognition: {best} ms when the text on screen did not change"));
    }

    private static SearchSurface CreateSurface(Image image)
    {
        return new SearchSurface(PixelBuffer.Wrap(image.Pixels.ToArray(), image.Width, image.Height), ownsPixels: true);
    }

    /// <summary>
    /// The recorder is given frames directly: this device is never asked for anything.
    /// </summary>
    private sealed class NoDisplayDevice : IDisplayDevice
    {
        public IReadOnlyList<DisplayInfo> GetDisplays() => [];

        public PixelBuffer Capture(DisplayInfo display) => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Regions are expressed for a 1920x1080 screenshot and adapted to the size of the actual one.
    /// </summary>
    private static Rectangle Scale(Image screen, int left, int top, int right, int bottom)
    {
        var horizontal = screen.Width / 1920.0;
        var vertical = screen.Height / 1080.0;
        return new Rectangle((int)(left * horizontal), (int)(top * vertical), Math.Max((int)(left * horizontal) + 4, (int)(right * horizontal)), Math.Max((int)(top * vertical) + 4, (int)(bottom * vertical)));
    }
}
