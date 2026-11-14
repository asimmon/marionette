using System.Runtime.InteropServices;
using Askaiser.Marionette.Imaging;

namespace Askaiser.Marionette.Tests;

public class PixelOperationsTests
{
    [Test]
    public async Task TryGetChangedBounds_WhenImagesAreIdentical_ReturnsFalse()
    {
        var pixels = CreateNoise(37, 23, seed: 1);

        var changed = PixelOperations.TryGetChangedBounds(MemoryMarshal.AsBytes(pixels.AsSpan()), MemoryMarshal.AsBytes(pixels.AsSpan()), 37, 23, out _, out _, out _, out _);

        await Assert.That(changed).IsFalse();
    }

    [Test]
    [Arguments(0, 0, 1, 1)]
    [Arguments(36, 22, 37, 23)]
    [Arguments(0, 22, 1, 23)]
    [Arguments(36, 0, 37, 1)]
    [Arguments(5, 7, 20, 9)]
    [Arguments(0, 0, 37, 23)]
    [Arguments(17, 3, 18, 21)]
    [Arguments(3, 11, 35, 12)]
    public async Task TryGetChangedBounds_ReturnsTheSmallestRectangleThatContainsEveryChange(int left, int top, int right, int bottom)
    {
        // The width is not a multiple of the number of pixels compared at once, and changes are at the corners of the rectangle only.
        const int width = 37;
        const int height = 23;

        var previous = CreateNoise(width, height, seed: 2);
        var current = (uint[])previous.Clone();
        current[(top * width) + left] ^= 0x00010000;
        current[((bottom - 1) * width) + right - 1] ^= 0x00000100;
        current[(top * width) + right - 1] ^= 0x01000000;
        current[((bottom - 1) * width) + left] ^= 0x00000001;

        var changed = PixelOperations.TryGetChangedBounds(
            MemoryMarshal.AsBytes(previous.AsSpan()), MemoryMarshal.AsBytes(current.AsSpan()), width, height, out var actualLeft, out var actualTop, out var actualRight, out var actualBottom);

        await Assert.That(changed).IsTrue();
        await Assert.That((actualLeft, actualTop, actualRight, actualBottom)).IsEqualTo((left, top, right, bottom));
    }

    [Test]
    public async Task TryGetChangedBounds_WithScatteredChanges_MatchesAPixelByPixelComparison()
    {
        const int width = 61;
        const int height = 40;
        var random = new Random(3);

        for (var iteration = 0; iteration < 200; iteration++)
        {
            var previous = CreateNoise(width, height, seed: iteration);
            var current = (uint[])previous.Clone();

            for (var change = random.Next(1, 6); change > 0; change--)
            {
                var index = random.Next(current.Length);
                current[index] = ~current[index];
            }

            // Inverting the same pixel twice restores it.
            if (current.AsSpan().SequenceEqual(previous))
            {
                continue;
            }

            var (expectedLeft, expectedTop, expectedRight, expectedBottom) = GetBounds(previous, current, width, height);

            PixelOperations.TryGetChangedBounds(MemoryMarshal.AsBytes(previous.AsSpan()), MemoryMarshal.AsBytes(current.AsSpan()), width, height, out var left, out var top, out var right, out var bottom);

            await Assert.That((left, top, right, bottom)).IsEqualTo((expectedLeft, expectedTop, expectedRight, expectedBottom));
        }
    }

    [Test]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    public async Task Reduce_AveragesSquaresOfPixels(int factor)
    {
        // Sizes that are not multiples of the factor: the pixels that do not fill a square are dropped.
        const int width = 45;
        const int height = 31;

        var source = CreateNoise(width, height, seed: factor);
        var reducedWidth = width / factor;
        var reducedHeight = height / factor;
        var reduced = new uint[reducedWidth * reducedHeight];

        PixelOperations.Reduce(MemoryMarshal.AsBytes(source.AsSpan()), width, height, factor, MemoryMarshal.AsBytes(reduced.AsSpan()));

        var largestError = 0d;
        for (var y = 0; y < reducedHeight; y++)
        {
            for (var x = 0; x < reducedWidth; x++)
            {
                for (var channel = 0; channel < 4; channel++)
                {
                    var sum = 0;
                    for (var blockY = 0; blockY < factor; blockY++)
                    {
                        for (var blockX = 0; blockX < factor; blockX++)
                        {
                            sum += (int)(source[(((y * factor) + blockY) * width) + (x * factor) + blockX] >> (channel * 8)) & 0xFF;
                        }
                    }

                    var actual = (int)(reduced[(y * reducedWidth) + x] >> (channel * 8)) & 0xFF;
                    largestError = Math.Max(largestError, Math.Abs(actual - (sum / (double)(factor * factor))));
                }
            }
        }

        // The fast path for a factor of two averages twice, and rounds each time.
        await Assert.That(largestError).IsLessThanOrEqualTo(1);
    }

    [Test]
    public async Task Reduce_KeepsSolidColors()
    {
        var source = new uint[64 * 16];
        Array.Fill(source, 0xFF8040C1);
        var reduced = new uint[32 * 8];

        PixelOperations.Reduce(MemoryMarshal.AsBytes(source.AsSpan()), 64, 16, 2, MemoryMarshal.AsBytes(reduced.AsSpan()));

        await Assert.That(reduced.All(static pixel => pixel == 0xFF8040C1)).IsTrue();
    }

    [Test]
    public async Task AnimatedPng_DrawsWhatItCompressed()
    {
        const int width = 53;
        const int height = 29;

        var previous = CreateNoise(width, height, seed: 10);
        var current = (uint[])previous.Clone();

        // Half of the pixels of a region change, the others must be left as they are.
        var random = new Random(11);
        for (var y = 4; y < 20; y++)
        {
            for (var x = 7; x < 50; x++)
            {
                if (random.Next(2) == 0)
                {
                    current[(y * width) + x] = 0xFF000000 | (uint)random.Next(0x1000000);
                }
            }
        }

        // What a player displays: the previous image, in the RGBA layout of PNG.
        var canvas = new byte[width * height * 4];
        AnimatedPng.ConvertToRgba(MemoryMarshal.AsBytes(previous.AsSpan()), canvas);

        using var compressed = new PooledMemoryStream();
        AnimatedPng.CompressRegion(MemoryMarshal.AsBytes(current.AsSpan()), MemoryMarshal.AsBytes(previous.AsSpan()), width * 4, 7, 4, 43, 16, compressed);
        AnimatedPng.Apply(compressed.WrittenSpan, canvas, width * 4, 7, 4, 43, 16);

        var expected = new byte[width * height * 4];
        AnimatedPng.ConvertToRgba(MemoryMarshal.AsBytes(current.AsSpan()), expected);

        await Assert.That(canvas.AsSpan().SequenceEqual(expected)).IsTrue();
    }

    private static (int Left, int Top, int Right, int Bottom) GetBounds(uint[] previous, uint[] current, int width, int height)
    {
        int left = width, top = height, right = 0, bottom = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (previous[(y * width) + x] != current[(y * width) + x])
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x + 1);
                    bottom = Math.Max(bottom, y + 1);
                }
            }
        }

        return (left, top, right, bottom);
    }

    private static uint[] CreateNoise(int width, int height, int seed)
    {
        var random = new Random(seed);
        var pixels = new uint[width * height];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = 0xFF000000 | (uint)random.Next(0x1000000);
        }

        return pixels;
    }
}
