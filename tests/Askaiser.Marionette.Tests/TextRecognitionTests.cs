using Askaiser.Marionette.Recognition;
using Askaiser.Marionette.Tests.Fakes;

namespace Askaiser.Marionette.Tests;

/// <summary>
/// These tests run the real text recognition models. They share a recognizer, because loading the models takes a moment.
/// </summary>
[ClassDataSource<RecognizerFixture>(Shared = SharedType.PerClass)]
public sealed class TextRecognitionTests(TextRecognitionTests.RecognizerFixture fixture)
{
    /// <summary>Text recognition locates characters within a few pixels.</summary>
    private const int Tolerance = 8;

    private readonly TextElementRecognizer _recognizer = fixture.Recognizer;

    // These are the cases of the first versions of the library, which read text with Tesseract.
    [Test]
    [Arguments(0, 0, 120, 30, "Google News", TextOptions.BlackAndWhite | TextOptions.Negative, 80, 18)]
    [Arguments(380, 170, 880, 336, "Headlines", TextOptions.None, 62, 17)]
    [Arguments(380, 170, 880, 336, "mperatures in southern califor", TextOptions.None, 172, 150)]
    [Arguments(20, 810, 160, 860, "English (United States)", TextOptions.None, 67, 36)]
    [Arguments(0, 0, 1920, 1080, "historic miami-dade courthouse closed due to ", TextOptions.None, 600, 665)]
    public async Task Recognize_WhenSingleOccurrence_ReturnsIt(int left, int top, int right, int bottom, string searched, TextOptions options, int expectedX, int expectedY)
    {
        var locations = this.Recognize(TestAssets.GoogleNews.Crop(new Rectangle(left, top, right, bottom)), new TextElement(searched, options));

        var location = await Assert.That(locations).HasSingleItem();
        await AssertNearAsync(new Point(expectedX, expectedY), location.Center);
    }

    [Test]
    public async Task Recognize_WhenSeveralOccurrences_ReturnsThemAll()
    {
        var element = new TextElement("California") { IgnoreCase = false };
        var locations = this.Recognize(TestAssets.GoogleNews.Crop(new Rectangle(410, 300, 800, 550)), element);

        await Assert.That(locations.Count).IsEqualTo(2);
        await AssertNearAsync(new Point(255, 18), locations[0].Center);
        await AssertNearAsync(new Point(306, 132), locations[1].Center);
    }

    [Test]
    [Arguments("Cancel", 161, 98)]
    [Arguments("Delete account", 261, 98)]
    [Arguments("Export report", 378, 98)]
    [Arguments("Account overview", 394, 148)]
    [Arguments("RECENT PROJECTS", 66, 285)]
    [Arguments("website_redesign_2026", 88, 338)]
    [Arguments("$48,210.75", 333, 224)]
    [Arguments("99.98 %", 1091, 224)]
    [Arguments("jane.doe@example.com", 660, 174)]
    [Arguments("Élodie Lefèvre", 407, 485)]
    [Arguments("Jürgen Müller", 404, 508)]
    [Arguments("María Peña", 402, 531)]
    [Arguments("€58.40", 576, 508)]
    [Arguments("MarionetteDriver.Create();", 484, 566)]
    [Arguments("the quick brown fox jumps over the lazy dog", 610, 610)]
    public async Task Recognize_OnUserInterface_FindsText(string searched, int expectedX, int expectedY)
    {
        var locations = this.Recognize(TestAssets.TestBench, new TextElement(searched) { IgnoreCase = false });

        var location = await Assert.That(locations).HasSingleItem();
        await AssertNearAsync(new Point(expectedX, expectedY), location.Center);
    }

    [Test]
    public async Task Recognize_WhenTextSpansSeveralWidelySpacedWords_FindsIt()
    {
        // The entries of a menu bar are far from each other, yet they can be searched as a phrase.
        var locations = this.Recognize(TestAssets.TestBench, new TextElement("File Edit View"));

        var location = await Assert.That(locations).HasSingleItem();
        await AssertNearAsync(new Point(60, 67), location.Center);
    }

    [Test]
    public async Task Recognize_WhenTextIsNotVisible_ReturnsNothing()
    {
        await Assert.That(this.Recognize(TestAssets.TestBench, new TextElement("This text is not on the screen"))).IsEmpty();
    }

    [Test]
    public async Task Recognize_WhenCaseDiffers_HonorsIgnoreCase()
    {
        await Assert.That(this.Recognize(TestAssets.TestBench, new TextElement("ACCOUNT OVERVIEW"))).HasSingleItem();
        await Assert.That(this.Recognize(TestAssets.TestBench, new TextElement("ACCOUNT OVERVIEW") { IgnoreCase = false })).IsEmpty();
    }

    [Test]
    public async Task Recognize_OnHighDensityDisplay_FindsText()
    {
        // The same interface displayed at twice the pixel density: text is located on a reduced image.
        var doubled = Upscale(TestAssets.TestBench.Crop(new Rectangle(0, 60, 700, 320)));
        using var surface = ExhaustiveSearch.CreateSurface(doubled);
        var display = FakeDisplayDevice.Displays[1] with { ScaleFactor = 2f };

        var locations = this._recognizer.Recognize(surface, display, new TextElement("Delete account"), TestContext.Current!.Execution.CancellationToken);

        var location = await Assert.That(locations).HasSingleItem();
        await AssertNearAsync(new Point(261 * 2, (98 - 60) * 2), location.Center);
    }

    private static async Task AssertNearAsync(Point expected, Point actual)
    {
        await Assert.That(Math.Abs(expected.X - actual.X) <= Tolerance && Math.Abs(expected.Y - actual.Y) <= Tolerance).IsTrue().Because($"Expected a location near {expected}, got {actual}");
    }

    private static Image Upscale(Image image)
    {
        var source = image.Pixels;
        var pixels = new byte[image.Width * 2 * image.Height * 2 * 4];

        for (var y = 0; y < image.Height * 2; y++)
        {
            for (var x = 0; x < image.Width * 2; x++)
            {
                source.Slice((((y / 2) * image.Width) + (x / 2)) * 4, 4).CopyTo(pixels.AsSpan(((y * image.Width * 2) + x) * 4, 4));
            }
        }

        return Image.FromBgra32(image.Width * 2, image.Height * 2, pixels);
    }

    private IReadOnlyList<Rectangle> Recognize(Image image, TextElement element)
    {
        using var surface = ExhaustiveSearch.CreateSurface(image);
        return this._recognizer.Recognize(surface, FakeDisplayDevice.Displays[1], element, TestContext.Current!.Execution.CancellationToken);
    }

    public sealed class RecognizerFixture : IDisposable
    {
        internal TextElementRecognizer Recognizer { get; } = new(TextRecognitionModel.Default);

        public void Dispose()
        {
            this.Recognizer.Dispose();
        }
    }
}
