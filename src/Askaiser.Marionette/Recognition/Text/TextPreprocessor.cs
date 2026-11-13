using System;
using Askaiser.Marionette.Imaging;

namespace Askaiser.Marionette.Recognition.Text;

/// <summary>
/// Applies the transformations requested by <see cref="TextOptions"/> to an image before text recognition.
/// </summary>
internal static class TextPreprocessor
{
    /// <summary>
    /// Returns a transformed copy of the image, or null when the options do not require any transformation.
    /// </summary>
    public static PixelBuffer? Apply(PixelBuffer source, TextOptions options)
    {
        if (options == TextOptions.None)
        {
            return null;
        }

        var result = PixelBuffer.Rent(source.Width, source.Height);
        source.Span.CopyTo(result.Span);
        var pixels = result.Span;

        if ((options & TextOptions.Grayscale) != 0)
        {
            Span<int> histogram = stackalloc int[256];
            histogram.Clear();

            for (var i = 0; i < pixels.Length; i += 4)
            {
                var gray = (byte)(((pixels[i] * 29) + (pixels[i + 1] * 150) + (pixels[i + 2] * 77) + 128) >> 8);
                pixels[i] = pixels[i + 1] = pixels[i + 2] = gray;
                histogram[gray]++;
            }

            if ((options & TextOptions.BlackAndWhite) == TextOptions.BlackAndWhite)
            {
                var threshold = ComputeOtsuThreshold(histogram, pixels.Length / 4);
                for (var i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = pixels[i + 1] = pixels[i + 2] = pixels[i] > threshold ? (byte)255 : (byte)0;
                }
            }
        }

        if ((options & TextOptions.Negative) != 0)
        {
            for (var i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)~pixels[i];
                pixels[i + 1] = (byte)~pixels[i + 1];
                pixels[i + 2] = (byte)~pixels[i + 2];
            }
        }

        return result;
    }

    /// <summary>
    /// Finds the gray level that best separates dark pixels from bright ones, by maximizing the variance between both groups (Otsu's method).
    /// </summary>
    private static int ComputeOtsuThreshold(ReadOnlySpan<int> histogram, int pixelCount)
    {
        long total = 0;
        for (var level = 0; level < histogram.Length; level++)
        {
            total += (long)level * histogram[level];
        }

        long darkSum = 0, darkCount = 0;
        var bestVariance = -1.0;
        var bestThreshold = 127;

        for (var level = 0; level < histogram.Length; level++)
        {
            darkCount += histogram[level];
            if (darkCount == 0)
            {
                continue;
            }

            var brightCount = pixelCount - darkCount;
            if (brightCount == 0)
            {
                break;
            }

            darkSum += (long)level * histogram[level];
            var darkMean = (double)darkSum / darkCount;
            var brightMean = (double)(total - darkSum) / brightCount;
            var variance = (double)darkCount * brightCount * (darkMean - brightMean) * (darkMean - brightMean);

            if (variance > bestVariance)
            {
                bestVariance = variance;
                bestThreshold = level;
            }
        }

        return bestThreshold;
    }
}
