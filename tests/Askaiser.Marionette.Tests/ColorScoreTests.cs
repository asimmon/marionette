using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Recognition.Images;

namespace Askaiser.Marionette.Tests;

/// <summary>
/// The exact score of a position is computed with vector instructions that depend on the processor: it must always equal its definition.
/// </summary>
public class ColorScoreTests
{
    [Test]
    [Arguments(2, 1, 0)]
    [Arguments(3, 2, 1)]
    [Arguments(7, 5, 2)]
    [Arguments(8, 8, 3)]
    [Arguments(9, 4, 4)]
    [Arguments(15, 3, 5)]
    [Arguments(16, 16, 6)]
    [Arguments(31, 7, 7)]
    [Arguments(33, 9, 8)]
    [Arguments(100, 30, 9)]
    [Arguments(700, 12, 10)]
    public async Task Score_EqualsTheDefinitionOfTheCorrelation(int templateWidth, int templateHeight, int seed)
    {
        var random = new Random(seed);
        var screenWidth = templateWidth + 13;
        var screenHeight = templateHeight + 5;
        var screen = new byte[screenWidth * screenHeight * 4];
        random.NextBytes(screen);
        var templatePixels = Crop(screen, screenWidth, 6, 2, templateWidth, templateHeight);

        // The template is the screen at the position, plus noise, so that the scores are high and a small error would be visible.
        for (var i = 0; i < templatePixels.Length; i++)
        {
            if (random.Next(5) == 0)
            {
                templatePixels[i] = (byte)random.Next(256);
            }
        }

        var template = ImageTemplate.Create(templatePixels, templateWidth, templateHeight, 0.5, grayscale: false);
        await Assert.That(template).IsNotNull();

        using var surface = new SearchSurface(PixelBuffer.Wrap(screen, screenWidth, screenHeight), ownsPixels: false);

        foreach (var (x, y) in new[] { (0, 0), (6, 2), (13, 5), (3, 4) })
        {
            var expected = Reference(screen, screenWidth, templatePixels, templateWidth, templateHeight, x, y);
            await Assert.That(TemplateMatcher.Score(surface, template!, x, y)).IsEqualTo(expected).Within(1e-9);
        }
    }

    [Test]
    public async Task Score_WhenEveryChannelIsSaturated_DoesNotOverflow()
    {
        // 400 x 300 pixels of values that maximize every intermediate sum, with a few darker pixels so that the image has contrast.
        const int width = 400;
        const int height = 300;
        const int screenWidth = width + 8;
        const int screenHeight = height + 8;
        var screen = new byte[screenWidth * screenHeight * 4];
        Array.Fill(screen, (byte)255);

        var templatePixels = new byte[width * height * 4];
        Array.Fill(templatePixels, (byte)255);
        for (var i = 0; i < 3; i++)
        {
            templatePixels[i] = 0;
            screen[((((4 * screenWidth) + 4) * 4) + i)] = 0;
        }

        var template = ImageTemplate.Create(templatePixels, width, height, 0.5, grayscale: false);
        await Assert.That(template).IsNotNull();

        using var surface = new SearchSurface(PixelBuffer.Wrap(screen, screenWidth, screenHeight), ownsPixels: false);

        var expected = Reference(screen, screenWidth, templatePixels, width, height, 4, 4);
        await Assert.That(TemplateMatcher.Score(surface, template!, 4, 4)).IsEqualTo(expected).Within(1e-9);
    }

    /// <summary>
    /// The score as it is defined: the correlation between the template and the window, over the three color channels, each centered on its own mean. Alpha is ignored.
    /// </summary>
    private static double Reference(byte[] screen, int screenWidth, byte[] template, int width, int height, int left, int top)
    {
        var area = (double)width * height;
        var meansWindow = new double[3];
        var meansTemplate = new double[3];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                for (var c = 0; c < 3; c++)
                {
                    meansWindow[c] += screen[((((top + y) * screenWidth) + left + x) * 4) + c];
                    meansTemplate[c] += template[(((y * width) + x) * 4) + c];
                }
            }
        }

        for (var c = 0; c < 3; c++)
        {
            meansWindow[c] /= area;
            meansTemplate[c] /= area;
        }

        double dot = 0, energyWindow = 0, energyTemplate = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                for (var c = 0; c < 3; c++)
                {
                    var w = screen[((((top + y) * screenWidth) + left + x) * 4) + c] - meansWindow[c];
                    var t = template[(((y * width) + x) * 4) + c] - meansTemplate[c];
                    dot += w * t;
                    energyWindow += w * w;
                    energyTemplate += t * t;
                }
            }
        }

        return energyWindow * area < 0.5 ? 0 : dot / Math.Sqrt(energyWindow * energyTemplate);
    }

    private static byte[] Crop(byte[] screen, int screenWidth, int left, int top, int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            Array.Copy(screen, (((top + y) * screenWidth) + left) * 4, pixels, y * width * 4, width * 4);
        }

        return pixels;
    }
}
