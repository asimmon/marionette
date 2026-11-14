using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Askaiser.Marionette.Imaging;

internal static class PixelOperations
{
    /// <summary>
    /// Sets the alpha channel of BGRA pixels to its maximum. Screen captures come with a meaningless alpha channel on most platforms.
    /// </summary>
    public static void MakeOpaque(Span<byte> bgra)
    {
        var pixels = MemoryMarshal.Cast<byte, uint>(bgra);
        var alpha = BitConverter.IsLittleEndian ? 0xFF000000u : 0x000000FFu;
        var i = 0;

        if (Vector.IsHardwareAccelerated)
        {
            var mask = new Vector<uint>(alpha);
            for (; i <= pixels.Length - Vector<uint>.Count; i += Vector<uint>.Count)
            {
                (new Vector<uint>(pixels.Slice(i)) | mask).CopyTo(pixels.Slice(i));
            }
        }

        for (; i < pixels.Length; i++)
        {
            pixels[i] |= alpha;
        }
    }

    /// <summary>
    /// Finds the smallest rectangle that contains every pixel that differs between two images of the same size.
    /// Returns false when the images are identical. <paramref name="right"/> and <paramref name="bottom"/> are exclusive.
    /// </summary>
    public static bool TryGetChangedBounds(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> current, int width, int height, out int left, out int top, out int right, out int bottom)
    {
        var stride = width * PixelBuffer.BytesPerPixel;
        var length = stride * height;

        previous = previous.Slice(0, length);
        current = current.Slice(0, length);

        var firstDifference = previous.CommonPrefixLength(current);
        if (firstDifference == length)
        {
            left = top = right = bottom = 0;
            return false;
        }

        top = firstDifference / stride;

        // The row at the top differs, which stops this loop at the latest.
        bottom = height;
        while (previous.Slice((bottom - 1) * stride, stride).SequenceEqual(current.Slice((bottom - 1) * stride, stride)))
        {
            bottom--;
        }

        left = width;
        right = 0;

        for (var y = top; y < bottom; y++)
        {
            var previousRow = previous.Slice(y * stride, stride);
            var currentRow = current.Slice(y * stride, stride);

            // Only what is outside of the bounds found so far can extend them, and most of the time nothing does:
            // comparing whole runs of pixels is much faster than looking at them one by one.
            var leftLength = left * PixelBuffer.BytesPerPixel;
            var prefix = previousRow.Slice(0, leftLength).CommonPrefixLength(currentRow.Slice(0, leftLength));
            if (prefix < leftLength)
            {
                left = prefix / PixelBuffer.BytesPerPixel;
            }

            var rightOffset = right * PixelBuffer.BytesPerPixel;
            if (!previousRow.Slice(rightOffset).SequenceEqual(currentRow.Slice(rightOffset)))
            {
                var previousPixels = MemoryMarshal.Cast<byte, uint>(previousRow);
                var currentPixels = MemoryMarshal.Cast<byte, uint>(currentRow);

                var x = width - 1;
                while (previousPixels[x] == currentPixels[x])
                {
                    x--;
                }

                right = x + 1;
            }
        }

        return true;
    }

    /// <summary>
    /// Divides the size of a BGRA image by a whole number: every pixel of the result is the average of a square of
    /// <paramref name="factor"/> by <paramref name="factor"/> pixels. Pixels that do not fill a square, on the right and at the bottom, are dropped.
    /// </summary>
    public static void Reduce(ReadOnlySpan<byte> source, int width, int height, int factor, Span<byte> destination)
    {
        var reducedWidth = width / factor;
        var reducedHeight = height / factor;

        var sourcePixels = MemoryMarshal.Cast<byte, uint>(source);
        var destinationPixels = MemoryMarshal.Cast<byte, uint>(destination);

        if (factor == 2)
        {
            for (var y = 0; y < reducedHeight; y++)
            {
                ReduceRowByTwo(sourcePixels.Slice(y * 2 * width, width), sourcePixels.Slice(((y * 2) + 1) * width, width), destinationPixels.Slice(y * reducedWidth, reducedWidth));
            }

            return;
        }

        var area = (uint)(factor * factor);
        var rounding = area / 2;

        for (var y = 0; y < reducedHeight; y++)
        {
            var destinationRow = destinationPixels.Slice(y * reducedWidth, reducedWidth);

            for (var x = 0; x < reducedWidth; x++)
            {
                uint sum0 = 0, sum1 = 0, sum2 = 0, sum3 = 0;

                for (var blockY = 0; blockY < factor; blockY++)
                {
                    var block = sourcePixels.Slice((((y * factor) + blockY) * width) + (x * factor), factor);
                    foreach (var pixel in block)
                    {
                        sum0 += pixel & 0xFF;
                        sum1 += (pixel >> 8) & 0xFF;
                        sum2 += (pixel >> 16) & 0xFF;
                        sum3 += pixel >> 24;
                    }
                }

                destinationRow[x] = ((sum0 + rounding) / area) | (((sum1 + rounding) / area) << 8) | (((sum2 + rounding) / area) << 16) | (((sum3 + rounding) / area) << 24);
            }
        }
    }

    /// <summary>
    /// Averages two rows of pixels vertically, then pairs of pixels horizontally.
    /// </summary>
    /// <remarks>
    /// The four channels of a pixel are averaged at once in a 32-bit integer, without unpacking them: the average of two bytes
    /// is <c>(a &amp; b) + ((a ^ b) &gt;&gt; 1)</c>, which cannot overflow into the next channel once the bit shifted out of each channel is masked.
    /// That average rounds down. The second one rounds up, <c>(a | b) - ((a ^ b) &gt;&gt; 1)</c>, so that the result does not get darker.
    /// </remarks>
    private static void ReduceRowByTwo(ReadOnlySpan<uint> top, ReadOnlySpan<uint> bottom, Span<uint> destination)
    {
        const uint lowBits = 0x7F7F7F7Fu;
        var x = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            var mask = Vector128.Create(lowBits);
            var wideMask = Vector128.Create((ulong)lowBits);
            var lowHalf = Vector128.Create(0xFFFFFFFFul);

            for (; x <= destination.Length - Vector128<uint>.Count; x += Vector128<uint>.Count)
            {
                var top0 = Vector128.Create(top.Slice(x * 2));
                var top1 = Vector128.Create(top.Slice((x * 2) + Vector128<uint>.Count));
                var bottom0 = Vector128.Create(bottom.Slice(x * 2));
                var bottom1 = Vector128.Create(bottom.Slice((x * 2) + Vector128<uint>.Count));

                var vertical0 = ((top0 & bottom0) + (((top0 ^ bottom0) >>> 1) & mask)).AsUInt64();
                var vertical1 = ((top1 & bottom1) + (((top1 ^ bottom1) >>> 1) & mask)).AsUInt64();

                // Every 64-bit lane holds two neighbors: average its low half with its high half.
                var even0 = vertical0 & lowHalf;
                var odd0 = vertical0 >>> 32;
                var even1 = vertical1 & lowHalf;
                var odd1 = vertical1 >>> 32;

                var horizontal0 = (even0 | odd0) - (((even0 ^ odd0) >>> 1) & wideMask);
                var horizontal1 = (even1 | odd1) - (((even1 ^ odd1) >>> 1) & wideMask);

                Vector128.Narrow(horizontal0, horizontal1).CopyTo(destination.Slice(x));
            }
        }

        for (; x < destination.Length; x++)
        {
            var left = (top[x * 2] & bottom[x * 2]) + (((top[x * 2] ^ bottom[x * 2]) >>> 1) & lowBits);
            var right = (top[(x * 2) + 1] & bottom[(x * 2) + 1]) + (((top[(x * 2) + 1] ^ bottom[(x * 2) + 1]) >>> 1) & lowBits);
            destination[x] = (left | right) - (((left ^ right) >>> 1) & lowBits);
        }
    }
}
