using TUnit.Assertions.Enums;
using Askaiser.Marionette.Recognition;
using Askaiser.Marionette.Recognition.Images;
using Askaiser.Marionette.Tests.Fakes;

namespace Askaiser.Marionette.Tests;

public class TemplateMatcherTests
{
    // These are the cases of the first versions of the library, which searched images with OpenCV.
    [Test]
    [Arguments(0, 0, 200, 100, "0.95", false, 100, 50)]
    [Arguments(1000, 300, 1100, 400, "0.95", false, 1050, 350)]
    [Arguments(20, 620, 162, 660, "0.95", false, 91, 640)]
    [Arguments(0, 0, 200, 100, "0.8", false, 100, 50)]
    [Arguments(1000, 300, 1100, 400, "0.8", false, 1050, 350)]
    [Arguments(20, 620, 162, 660, "0.8", false, 91, 640)]
    [Arguments(0, 0, 200, 100, "0.95", true, 100, 50)]
    [Arguments(1000, 300, 1100, 400, "0.95", true, 1050, 350)]
    [Arguments(20, 620, 162, 660, "0.95", true, 91, 640)]
    public async Task Find_WhenSingleOccurrence_ReturnsIt(int left, int top, int right, int bottom, string threshold, bool grayscale, int expectedX, int expectedY)
    {
        var screenshot = TestAssets.GoogleNews;
        var element = new ImageElement("searched", screenshot.Crop(new Rectangle(left, top, right, bottom)), decimal.Parse(threshold, System.Globalization.CultureInfo.InvariantCulture), grayscale);

        var locations = Recognize(screenshot, element);

        var location = await Assert.That(locations).HasSingleItem();
        await Assert.That(location.Center).IsEqualTo(new Point(expectedX, expectedY));
    }

    [Test]
    public async Task Find_WhenTwoOccurrences_ReturnsBoth()
    {
        var screenshot = TestAssets.GoogleNews;
        var element = new ImageElement("searched", screenshot.Crop(new Rectangle(1376, 390, 1420, 436)));

        var locations = Recognize(screenshot, element);

        await Assert.That(locations.Select(location => location.Center)).IsEquivalentTo([new Point(1398, 413), new Point(1464, 413)], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Find_WhenImageIsEncoded_DecodesIt()
    {
        var screenshot = TestAssets.TestBench;
        var png = screenshot.Crop(new Rectangle(8, 85, 120, 111)).ToPng();
        var element = new ImageElement("save-button", png, 0.95m, grayscale: false);

        var location = await Assert.That(Recognize(screenshot, element)).HasSingleItem();
        await Assert.That(location).IsEqualTo(new Rectangle(8, 85, 120, 111));
    }

    [Test]
    public async Task Find_WhenImageIsLargerThanScreen_ReturnsNothing()
    {
        var element = new ImageElement("large", TestAssets.TestBench.Crop(new Rectangle(0, 0, 400, 300)));
        await Assert.That(Recognize(TestAssets.TestBench.Crop(new Rectangle(0, 0, 399, 300)), element)).IsEmpty();
    }

    [Test]
    public async Task Find_WhenImageIsNotOnScreen_ReturnsNothing()
    {
        var element = new ImageElement("other", TestAssets.GoogleNews.Crop(new Rectangle(20, 620, 162, 660)));
        await Assert.That(Recognize(TestAssets.TestBench, element)).IsEmpty();
    }

    [Test]
    public async Task Find_WhenImageIsSolidColor_Throws()
    {
        var pixels = new byte[16 * 16 * 4];
        Array.Fill(pixels, (byte)200);
        var element = new ImageElement("solid", Image.FromBgra32(16, 16, pixels));

        var exception = Assert.Throws<ArgumentException>(() => Recognize(TestAssets.TestBench, element));
        await Assert.That(exception!.Message).Contains("solid");
    }

    [Test]
    public async Task Find_WhenContrastIsOnlyInColor_FindsIt()
    {
        // A red shape on a blue background of the same brightness: invisible once converted to shades of gray.
        var screen = CreateImage(120, 80, (x, y) => IsInside(x, y, 70, 30, 12, 9) ? (60, 20, 220) : (220, 20, 60));
        var template = screen.Crop(new Rectangle(62, 24, 90, 48));
        var element = new ImageElement("shape", template);

        using var surface = ExhaustiveSearch.CreateSurface(screen);
        var matches = TemplateMatcher.Find(surface, element.GetTemplate()!, cancellationToken: TestContext.Current!.Execution.CancellationToken);

        await Assert.That(matches).Contains(match => match.X == 62 && match.Y == 24);
        await Assert.That(element.GetTemplate()!.PlaneKind).IsNotEqualTo(ColorPlaneKind.Sum);
        await ExhaustiveSearch.AssertSameAsExhaustiveAsync(surface, element.GetTemplate()!, matches);
    }

    [Test]
    [Arguments(1, "0.95", false)]
    [Arguments(2, "0.95", true)]
    [Arguments(3, "0.8", false)]
    [Arguments(4, "0.8", true)]
    [Arguments(5, "0.6", false)]
    [Arguments(6, "1.0", false)]
    [Arguments(7, "0.3", true)]
    public async Task Find_OnSyntheticScreens_ReturnsTheSameAsAnExhaustiveSearch(int seed, string threshold, bool grayscale)
    {
        var random = new Random(seed);
        var screen = CreateSyntheticScreen(random, 220, 150, out var template);
        var element = new ImageElement("planted", template, decimal.Parse(threshold, System.Globalization.CultureInfo.InvariantCulture), grayscale);

        using var surface = ExhaustiveSearch.CreateSurface(screen);
        var matches = TemplateMatcher.Find(surface, element.GetTemplate()!, cancellationToken: TestContext.Current!.Execution.CancellationToken);

        await Assert.That(matches).IsNotEmpty();
        await ExhaustiveSearch.AssertSameAsExhaustiveAsync(surface, element.GetTemplate()!, matches);
    }

    [Test]
    [Arguments(8, 85, 120, 111, "0.95", false)]
    [Arguments(8, 85, 120, 111, "0.9", true)]
    [Arguments(441, 87, 465, 111, "0.95", false)]
    [Arguments(284, 189, 528, 303, "0.95", false)]
    [Arguments(18, 167, 83, 183, "0.7", false)]
    [Arguments(296, 452, 303, 471, "0.9", true)]
    public async Task Find_OnRealScreenshot_ReturnsTheSameAsAnExhaustiveSearch(int left, int top, int right, int bottom, string threshold, bool grayscale)
    {
        // A part of the screenshot keeps the exhaustive search fast, while still containing the image and plenty of look-alikes.
        var screen = TestAssets.TestBench.Crop(new Rectangle(0, 60, 700, 520));
        var template = TestAssets.TestBench.Crop(new Rectangle(left, top, right, bottom));
        var element = new ImageElement("searched", template, decimal.Parse(threshold, System.Globalization.CultureInfo.InvariantCulture), grayscale);

        using var surface = ExhaustiveSearch.CreateSurface(screen);
        var statistics = new TemplateSearchStatistics();
        var matches = TemplateMatcher.Find(surface, element.GetTemplate()!, statistics, TestContext.Current!.Execution.CancellationToken);

        await Assert.That(matches).Contains(match => match.X == left && match.Y == top - 60);
        await ExhaustiveSearch.AssertSameAsExhaustiveAsync(surface, element.GetTemplate()!, matches);

        // The whole point of the coarse levels: almost no position should need an exact score.
        await Assert.That(statistics.Passed[^2] < statistics.Positions / 20).IsTrue().Because($"{statistics.Passed[^2]} of {statistics.Positions} positions were scored exactly");
    }

    [Test]
    public async Task Find_WhenSearchedSeveralTimes_ReturnsTheSameResult()
    {
        var element = new ImageElement("searched", TestAssets.GoogleNews.Crop(new Rectangle(1376, 390, 1420, 436)));
        using var surface = ExhaustiveSearch.CreateSurface(TestAssets.GoogleNews);

        var first = TemplateMatcher.Find(surface, element.GetTemplate()!, cancellationToken: TestContext.Current!.Execution.CancellationToken);
        var second = TemplateMatcher.Find(surface, element.GetTemplate()!, cancellationToken: TestContext.Current!.Execution.CancellationToken);

        await Assert.That(second).IsEquivalentTo(first, CollectionOrdering.Matching);
    }

    private static IReadOnlyList<Rectangle> Recognize(Image screen, ImageElement element)
    {
        using var surface = ExhaustiveSearch.CreateSurface(screen);
        return new ImageElementRecognizer().Recognize(surface, FakeDisplayDevice.Displays[1], element, TestContext.Current!.Execution.CancellationToken);
    }

    private static bool IsInside(int x, int y, int centerX, int centerY, int halfWidth, int halfHeight)
    {
        return Math.Abs(x - centerX) <= halfWidth && Math.Abs(y - centerY) <= halfHeight;
    }

    private static Image CreateImage(int width, int height, Func<int, int, (int B, int G, int R)> pixel)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var (b, g, r) = pixel(x, y);
                var i = ((y * width) + x) * 4;
                pixels[i] = (byte)b;
                pixels[i + 1] = (byte)g;
                pixels[i + 2] = (byte)r;
                pixels[i + 3] = 255;
            }
        }

        return Image.FromBgra32(width, height, pixels);
    }

    /// <summary>
    /// Creates a screen made of flat areas, gradients and noise, on which a template is drawn three times:
    /// once as is, once with a different brightness and contrast, and once with noise. Returns the template.
    /// </summary>
    private static Image CreateSyntheticScreen(Random random, int width, int height, out Image template)
    {
        const int templateWidth = 23, templateHeight = 17;

        var templatePixels = new (int B, int G, int R)[templateWidth * templateHeight];
        for (var i = 0; i < templatePixels.Length; i++)
        {
            // Blocky content with some texture, like a small icon.
            var blockValue = (((i % templateWidth) / 5) + ((i / templateWidth) / 4)) * 37 % 200;
            templatePixels[i] = (Math.Clamp(blockValue + random.Next(40), 0, 255), Math.Clamp(blockValue + random.Next(60), 0, 255), Math.Clamp(255 - blockValue + random.Next(30), 0, 255));
        }

        var noise = new byte[width * height];
        random.NextBytes(noise);

        var screen = CreateImage(width, height, (x, y) =>
        {
            if (x < width / 3)
            {
                return (40, 40, 48);
            }

            if (y < height / 2)
            {
                return (x, y * 2 % 256, (x + y) % 256);
            }

            var n = noise[(y * width) + x];
            return (n, n / 2, 255 - n);
        });

        var pixels = screen.Pixels.ToArray();

        void Draw(int left, int top, double gain, int offset, int noiseAmplitude)
        {
            for (var y = 0; y < templateHeight; y++)
            {
                for (var x = 0; x < templateWidth; x++)
                {
                    var (b, g, r) = templatePixels[(y * templateWidth) + x];
                    var i = (((top + y) * width) + left + x) * 4;
                    pixels[i] = (byte)Math.Clamp((b * gain) + offset + random.Next(-noiseAmplitude, noiseAmplitude + 1), 0, 255);
                    pixels[i + 1] = (byte)Math.Clamp((g * gain) + offset + random.Next(-noiseAmplitude, noiseAmplitude + 1), 0, 255);
                    pixels[i + 2] = (byte)Math.Clamp((r * gain) + offset + random.Next(-noiseAmplitude, noiseAmplitude + 1), 0, 255);
                }
            }
        }

        Draw(10, 12, 1.0, 0, 0);
        Draw(101, 33, 0.7, 20, 0);
        Draw(150, 110, 1.0, 0, 12);

        template = CreateImage(templateWidth, templateHeight, (x, y) => templatePixels[(y * templateWidth) + x]);
        return Image.FromBgra32(width, height, pixels);
    }
}
