using System.Diagnostics;
using System.Globalization;
using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Recognition.Images;
using Askaiser.Marionette.Recognition.Text;

namespace Askaiser.Marionette.Benchmarks;

/// <summary>
/// Measures image and text recognition on a screenshot, without needing a desktop.
/// Usage: dotnet run -c Release --project benchmarks/Askaiser.Marionette.Benchmarks [path/to/screenshot.png]
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
    /// Regions are expressed for a 1920x1080 screenshot and adapted to the size of the actual one.
    /// </summary>
    private static Rectangle Scale(Image screen, int left, int top, int right, int bottom)
    {
        var horizontal = screen.Width / 1920.0;
        var vertical = screen.Height / 1080.0;
        return new Rectangle((int)(left * horizontal), (int)(top * vertical), Math.Max((int)(left * horizontal) + 4, (int)(right * horizontal)), Math.Max((int)(top * vertical) + 4, (int)(bottom * vertical)));
    }
}
