using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Recognition.Images;

internal readonly record struct TemplateMatch(int X, int Y, double Score);

/// <summary>
/// Finds every occurrence of an <see cref="ImageTemplate"/> in a <see cref="SearchSurface"/>.
/// The result is the same as scoring every position with a normalized cross-correlation, only much faster. See <see cref="ImageTemplate"/> for the reasoning.
/// </summary>
internal static class TemplateMatcher
{
    /// <summary>Tolerance on the final score, to absorb floating-point rounding when a threshold of exactly 1 is requested.</summary>
    private const double ScoreTolerance = 1e-6;

    /// <summary>Relative slack applied to the coarse test, which is computed in single precision.</summary>
    private const float CoarseSlack = 0.995f;

    /// <summary>Below this many multiply-adds, splitting the search across threads costs more than it saves.</summary>
    private const long ParallelWorkThreshold = 4_000_000;

    public static List<TemplateMatch> Find(SearchSurface surface, ImageTemplate template, CancellationToken cancellationToken = default)
    {
        var positionsX = surface.Width - template.Width + 1;
        var positionsY = surface.Height - template.Height + 1;
        var matches = new List<TemplateMatch>();

        if (positionsX <= 0 || positionsY <= 0)
        {
            return matches;
        }

        var plane = surface.GetPlane(template.PlaneKind);
        var blockSums = surface.GetBlockSums(template.PlaneKind, template.BlockSize);

        var work = (long)positionsX * positionsY * template.Kernel.Length;
        var workers = work < ParallelWorkThreshold ? 1 : Math.Min(Environment.ProcessorCount, Math.Max(1, positionsY / 8));

        if (workers <= 1)
        {
            SearchRows(surface, template, plane, blockSums, 0, positionsY, matches, cancellationToken);
        }
        else
        {
            // Bands are several times more numerous than workers so that threads stay busy when matches are unevenly distributed.
            var bandCount = Math.Min(positionsY, workers * 4);
            var gate = new object();

            Parallel.For(0, bandCount, new ParallelOptions { MaxDegreeOfParallelism = workers }, band =>
            {
                var from = (int)((long)positionsY * band / bandCount);
                var to = (int)((long)positionsY * (band + 1) / bandCount);
                var local = new List<TemplateMatch>();
                SearchRows(surface, template, plane, blockSums, from, to, local, cancellationToken);

                if (local.Count > 0)
                {
                    lock (gate)
                    {
                        matches.AddRange(local);
                    }
                }
            });
        }

        cancellationToken.ThrowIfCancellationRequested();
        return SuppressOverlaps(matches, template.Width, template.Height);
    }

    /// <summary>
    /// Scores a single position exactly. This is the reference definition of a match and is what the search verifies candidates with.
    /// </summary>
    public static double Score(SearchSurface surface, ImageTemplate template, int x, int y)
    {
        return template.IsGrayscale
            ? ScoreGrayscale(surface.GetPlane(ColorPlaneKind.Sum), surface.Width, template, x, y)
            : ScoreColor(surface.Pixels.Span, surface.Pixels.Stride, template, x, y);
    }

    /// <summary>
    /// Keeps the best match of every group of overlapping matches. Two matches belong to the same group when
    /// their rectangles overlap by more than half in both directions, which is what happens around the exact location of an occurrence.
    /// </summary>
    private static List<TemplateMatch> SuppressOverlaps(List<TemplateMatch> matches, int width, int height)
    {
        if (matches.Count <= 1)
        {
            return matches;
        }

        // Position is a tie-breaker so that results do not depend on how the search was split across threads.
        matches.Sort(static (a, b) =>
        {
            var byScore = b.Score.CompareTo(a.Score);
            if (byScore != 0)
            {
                return byScore;
            }

            var byY = a.Y.CompareTo(b.Y);
            return byY != 0 ? byY : a.X.CompareTo(b.X);
        });

        var kept = new List<TemplateMatch>();
        var halfWidth = Math.Max(1, (width + 1) / 2);
        var halfHeight = Math.Max(1, (height + 1) / 2);

        foreach (var candidate in matches)
        {
            var overlaps = false;
            foreach (var other in kept)
            {
                if (Math.Abs(candidate.X - other.X) < halfWidth && Math.Abs(candidate.Y - other.Y) < halfHeight)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                kept.Add(candidate);
            }
        }

        kept.Sort(static (a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
        return kept;
    }

    private static unsafe void SearchRows(SearchSurface surface, ImageTemplate template, short[] plane, BlockSums blockSums, int fromY, int toY, List<TemplateMatch> matches, CancellationToken cancellationToken)
    {
        var width = surface.Width;
        var templateWidth = template.Width;
        var templateHeight = template.Height;
        var area = (double)template.Area;
        var positionsX = width - templateWidth + 1;
        var blockSize = template.BlockSize;

        // Position x of phase p is stored at phasePositionOffsets[p] + (x - p) / blockSize.
        var phasePositionOffsets = new int[blockSize + 1];
        for (var phase = 0; phase < blockSize; phase++)
        {
            var count = phase < positionsX ? ((positionsX - 1 - phase) / blockSize) + 1 : 0;
            phasePositionOffsets[phase + 1] = phasePositionOffsets[phase] + count;
        }

        var useBound = template.CoarseBound > 0;
        var boundSquared = useBound ? (float)(template.CoarseBound * template.CoarseBound * template.KernelEnergy) * CoarseSlack * CoarseSlack : 0f;
        var meanFactor = template.KernelSum * blockSize * blockSize / area;

        var pool = ArrayPool<int>.Shared;
        var columnSumsArray = pool.Rent(width);
        var columnSquaresArray = ArrayPool<uint>.Shared.Rent(width);
        var windowSumsArray = ArrayPool<long>.Shared.Rent(positionsX);
        var windowSquaresArray = ArrayPool<long>.Shared.Rent(positionsX);
        var correctionsArray = ArrayPool<float>.Shared.Rent(positionsX);
        var limitsArray = ArrayPool<float>.Shared.Rent(positionsX);
        var accumulatorArray = ArrayPool<float>.Shared.Rent(positionsX + Vector<float>.Count);
        var candidates = new List<int>();

        try
        {
            var columnSums = columnSumsArray.AsSpan(0, width);
            var columnSquares = columnSquaresArray.AsSpan(0, width);
            var windowSums = windowSumsArray.AsSpan(0, positionsX);
            var windowSquares = windowSquaresArray.AsSpan(0, positionsX);
            var corrections = correctionsArray.AsSpan(0, positionsX);
            var limits = limitsArray.AsSpan(0, positionsX);

            columnSums.Clear();
            columnSquares.Clear();
            for (var y = fromY; y < fromY + templateHeight; y++)
            {
                AddRow(plane.AsSpan(y * width, width), columnSums, columnSquares);
            }

            for (var y = fromY; y < toY; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Sum and sum of squares of the plane over the window at every position of this row.
                long sum = 0, squares = 0;
                for (var x = 0; x < templateWidth; x++)
                {
                    sum += columnSums[x];
                    squares += columnSquares[x];
                }

                var position = 0;
                for (var index = 0; position < positionsX; index++)
                {
                    for (var phase = 0; phase < blockSize && position < positionsX; phase++, position++)
                    {
                        windowSums[position] = sum;
                        windowSquares[position] = squares;

                        var energy = squares - ((double)sum * sum / area);
                        var slot = phasePositionOffsets[phase] + index;

                        // A window without any contrast cannot be correlated with anything: an infinite limit rejects it.
                        limits[slot] = energy * area < 0.5 ? float.PositiveInfinity : (float)(boundSquared * energy);
                        corrections[slot] = (float)(meanFactor * sum);

                        var next = position + templateWidth;
                        if (next < width)
                        {
                            sum += columnSums[next] - columnSums[position];
                            squares += (long)columnSquares[next] - columnSquares[position];
                        }
                    }
                }

                // 2. Coarse correlation of every position, one horizontal phase at a time.
                candidates.Clear();
                fixed (float* blockValues = blockSums.Values)
                fixed (float* kernel = template.Kernel)
                fixed (float* accumulator = accumulatorArray)
                fixed (float* correctionsPtr = corrections)
                fixed (float* limitsPtr = limits)
                {
                    for (var phase = 0; phase < blockSize; phase++)
                    {
                        var count = phasePositionOffsets[phase + 1] - phasePositionOffsets[phase];
                        if (count == 0)
                        {
                            continue;
                        }

                        var rowStart = blockValues + ((long)y * blockSums.RowLength) + blockSums.PhaseOffsets[phase];
                        Correlate(rowStart, (long)blockSize * blockSums.RowLength, kernel, template.KernelWidth, template.KernelHeight, accumulator, count);

                        var slotOffset = phasePositionOffsets[phase];
                        CollectCandidates(accumulator, correctionsPtr + slotOffset, limitsPtr + slotOffset, count, useBound, phase, blockSize, candidates);
                    }
                }

                // 3. Exact score of the few positions that passed.
                foreach (var x in candidates)
                {
                    var planeEnergy = windowSquares[x] - ((double)windowSums[x] * windowSums[x] / area);
                    var score = template.IsGrayscale
                        ? ScoreGrayscale(plane, width, template, x, y, windowSums[x], planeEnergy)
                        : ScoreColor(surface.Pixels.Span, surface.Pixels.Stride, template, x, y);

                    if (score >= template.Threshold - ScoreTolerance)
                    {
                        matches.Add(new TemplateMatch(x, y, score));
                    }
                }

                if (y + 1 < toY)
                {
                    SubtractRow(plane.AsSpan(y * width, width), columnSums, columnSquares);
                    AddRow(plane.AsSpan((y + templateHeight) * width, width), columnSums, columnSquares);
                }
            }
        }
        finally
        {
            pool.Return(columnSumsArray);
            ArrayPool<uint>.Shared.Return(columnSquaresArray);
            ArrayPool<long>.Shared.Return(windowSumsArray);
            ArrayPool<long>.Shared.Return(windowSquaresArray);
            ArrayPool<float>.Shared.Return(correctionsArray);
            ArrayPool<float>.Shared.Return(limitsArray);
            ArrayPool<float>.Shared.Return(accumulatorArray);
        }
    }

    /// <summary>
    /// Computes, for <paramref name="count"/> consecutive positions, the dot product between the kernel and the block sums under it.
    /// Positions are processed several vectors at a time with the accumulators kept in registers, so the inner loop is one load and one multiply-add per kernel value.
    /// </summary>
    private static unsafe void Correlate(float* rowStart, long rowStride, float* kernel, int kernelWidth, int kernelHeight, float* destination, int count)
    {
        var vectorSize = Vector<float>.Count;
        var index = 0;

        if (Vector.IsHardwareAccelerated && count >= vectorSize)
        {
            var blockOfFour = vectorSize * 4;
            for (; index <= count - blockOfFour; index += blockOfFour)
            {
                Vector<float> a0 = Vector<float>.Zero, a1 = Vector<float>.Zero, a2 = Vector<float>.Zero, a3 = Vector<float>.Zero;
                var k = kernel;
                var row = rowStart + index;

                for (var ky = 0; ky < kernelHeight; ky++, row += rowStride)
                {
                    for (var kx = 0; kx < kernelWidth; kx++)
                    {
                        var weight = new Vector<float>(*k++);
                        var source = row + kx;
                        a0 = MultiplyAdd(weight, Unsafe.ReadUnaligned<Vector<float>>(source), a0);
                        a1 = MultiplyAdd(weight, Unsafe.ReadUnaligned<Vector<float>>(source + vectorSize), a1);
                        a2 = MultiplyAdd(weight, Unsafe.ReadUnaligned<Vector<float>>(source + (2 * vectorSize)), a2);
                        a3 = MultiplyAdd(weight, Unsafe.ReadUnaligned<Vector<float>>(source + (3 * vectorSize)), a3);
                    }
                }

                Unsafe.WriteUnaligned(destination + index, a0);
                Unsafe.WriteUnaligned(destination + index + vectorSize, a1);
                Unsafe.WriteUnaligned(destination + index + (2 * vectorSize), a2);
                Unsafe.WriteUnaligned(destination + index + (3 * vectorSize), a3);
            }

            while (index < count)
            {
                // The last vector overlaps the previous one rather than reading past the end of the row.
                var start = Math.Min(index, count - vectorSize);
                var a = Vector<float>.Zero;
                var k = kernel;
                var row = rowStart + start;

                for (var ky = 0; ky < kernelHeight; ky++, row += rowStride)
                {
                    for (var kx = 0; kx < kernelWidth; kx++)
                    {
                        a = MultiplyAdd(new Vector<float>(*k++), Unsafe.ReadUnaligned<Vector<float>>(row + kx), a);
                    }
                }

                Unsafe.WriteUnaligned(destination + start, a);
                index = start + vectorSize;
            }

            return;
        }

        for (; index < count; index++)
        {
            var total = 0f;
            var k = kernel;
            var row = rowStart + index;

            for (var ky = 0; ky < kernelHeight; ky++, row += rowStride)
            {
                for (var kx = 0; kx < kernelWidth; kx++)
                {
                    total += *k++ * row[kx];
                }
            }

            destination[index] = total;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<float> MultiplyAdd(Vector<float> a, Vector<float> b, Vector<float> addend)
    {
#if NET9_0_OR_GREATER
        return Vector.FusedMultiplyAdd(a, b, addend);
#else
        return (a * b) + addend;
#endif
    }

    /// <summary>
    /// Keeps the positions whose coarse correlation is high enough for a match to be possible.
    /// </summary>
    private static unsafe void CollectCandidates(float* correlations, float* corrections, float* limits, int count, bool useBound, int phase, int blockSize, List<int> candidates)
    {
        if (!useBound)
        {
            // The coarse search cannot discard anything for this template: every window with some contrast is verified.
            for (var i = 0; i < count; i++)
            {
                if (!float.IsPositiveInfinity(limits[i]))
                {
                    candidates.Add(phase + (i * blockSize));
                }
            }

            return;
        }

        var index = 0;

        if (Vector.IsHardwareAccelerated)
        {
            var vectorSize = Vector<float>.Count;
            for (; index <= count - vectorSize; index += vectorSize)
            {
                var value = Unsafe.ReadUnaligned<Vector<float>>(correlations + index) - Unsafe.ReadUnaligned<Vector<float>>(corrections + index);
                var limit = Unsafe.ReadUnaligned<Vector<float>>(limits + index);
                var passes = Vector.GreaterThan(value, Vector<float>.Zero) & Vector.GreaterThanOrEqual(value * value, limit);

                if (passes == Vector<int>.Zero)
                {
                    continue;
                }

                for (var lane = 0; lane < vectorSize; lane++)
                {
                    if (passes[lane] != 0)
                    {
                        candidates.Add(phase + ((index + lane) * blockSize));
                    }
                }
            }
        }

        for (; index < count; index++)
        {
            var value = correlations[index] - corrections[index];
            if (value > 0 && value * value >= limits[index])
            {
                candidates.Add(phase + (index * blockSize));
            }
        }
    }

    private static void AddRow(ReadOnlySpan<short> row, Span<int> columnSums, Span<uint> columnSquares)
    {
        var x = 0;

        if (Vector.IsHardwareAccelerated)
        {
            var sums = MemoryMarshal.Cast<int, int>(columnSums);
            var squares = MemoryMarshal.Cast<uint, int>(columnSquares);
            var step = Vector<short>.Count;
            var half = Vector<int>.Count;

            for (; x <= row.Length - step; x += step)
            {
                Vector.Widen(new Vector<short>(row.Slice(x)), out var lo, out var hi);
                (new Vector<int>(sums.Slice(x)) + lo).CopyTo(sums.Slice(x));
                (new Vector<int>(sums.Slice(x + half)) + hi).CopyTo(sums.Slice(x + half));
                (new Vector<int>(squares.Slice(x)) + (lo * lo)).CopyTo(squares.Slice(x));
                (new Vector<int>(squares.Slice(x + half)) + (hi * hi)).CopyTo(squares.Slice(x + half));
            }
        }

        for (; x < row.Length; x++)
        {
            int v = row[x];
            columnSums[x] += v;
            columnSquares[x] += (uint)(v * v);
        }
    }

    private static void SubtractRow(ReadOnlySpan<short> row, Span<int> columnSums, Span<uint> columnSquares)
    {
        var x = 0;

        if (Vector.IsHardwareAccelerated)
        {
            var squares = MemoryMarshal.Cast<uint, int>(columnSquares);
            var step = Vector<short>.Count;
            var half = Vector<int>.Count;

            for (; x <= row.Length - step; x += step)
            {
                Vector.Widen(new Vector<short>(row.Slice(x)), out var lo, out var hi);
                (new Vector<int>(columnSums.Slice(x)) - lo).CopyTo(columnSums.Slice(x));
                (new Vector<int>(columnSums.Slice(x + half)) - hi).CopyTo(columnSums.Slice(x + half));
                (new Vector<int>(squares.Slice(x)) - (lo * lo)).CopyTo(squares.Slice(x));
                (new Vector<int>(squares.Slice(x + half)) - (hi * hi)).CopyTo(squares.Slice(x + half));
            }
        }

        for (; x < row.Length; x++)
        {
            int v = row[x];
            columnSums[x] -= v;
            columnSquares[x] -= (uint)(v * v);
        }
    }

    private static double ScoreGrayscale(short[] plane, int width, ImageTemplate template, int x, int y)
    {
        long sum = 0, squares = 0;
        for (var row = 0; row < template.Height; row++)
        {
            foreach (var v in plane.AsSpan(((y + row) * width) + x, template.Width))
            {
                sum += v;
                squares += v * v;
            }
        }

        return ScoreGrayscale(plane, width, template, x, y, sum, squares - ((double)sum * sum / template.Area));
    }

    private static double ScoreGrayscale(short[] plane, int width, ImageTemplate template, int x, int y, long windowSum, double windowEnergy)
    {
        if (windowEnergy * template.Area < 0.5)
        {
            return 0;
        }

        long dot = 0;
        for (var row = 0; row < template.Height; row++)
        {
            dot += Dot(template.Plane.AsSpan(row * template.Width, template.Width), plane.AsSpan(((y + row) * width) + x, template.Width));
        }

        var numerator = dot - ((double)template.PlaneSum * windowSum / template.Area);
        return numerator / Math.Sqrt(template.PlaneEnergy * windowEnergy);
    }

    private static long Dot(ReadOnlySpan<short> a, ReadOnlySpan<short> b)
    {
        long total = 0;
        var i = 0;

        if (Vector.IsHardwareAccelerated && a.Length >= Vector<short>.Count)
        {
            // Products are at most 765², so each 32-bit lane can hold thousands of them before it is flushed.
            var acc = Vector<int>.Zero;
            var pending = 0;

            for (; i <= a.Length - Vector<short>.Count; i += Vector<short>.Count)
            {
                Vector.Widen(new Vector<short>(a.Slice(i)), out var alo, out var ahi);
                Vector.Widen(new Vector<short>(b.Slice(i)), out var blo, out var bhi);
                acc += (alo * blo) + (ahi * bhi);

                if (++pending == 1024)
                {
                    Vector.Widen(acc, out var lo, out var hi);
                    total += Vector.Sum(lo) + Vector.Sum(hi);
                    acc = Vector<int>.Zero;
                    pending = 0;
                }
            }

            Vector.Widen(acc, out var restLo, out var restHi);
            total += Vector.Sum(restLo) + Vector.Sum(restHi);
        }

        for (; i < a.Length; i++)
        {
            total += a[i] * b[i];
        }

        return total;
    }

    /// <summary>
    /// Exact normalized cross-correlation over the three color channels, each centered on its own mean.
    /// </summary>
    private static double ScoreColor(ReadOnlySpan<byte> pixels, int stride, ImageTemplate template, int x, int y)
    {
        var rowLength = template.Width * 4;
        var templatePixels = template.Bgra.AsSpan();
        long dot = 0, sumB = 0, sumG = 0, sumR = 0, squares = 0;

        for (var row = 0; row < template.Height; row++)
        {
            var window = pixels.Slice(((y + row) * stride) + (x * 4), rowLength);
            var reference = templatePixels.Slice(row * rowLength, rowLength);
            AccumulateColorRow(window, reference, ref dot, ref sumB, ref sumG, ref sumR, ref squares);
        }

        var area = (double)template.Area;
        var windowEnergy = squares - ((((double)sumB * sumB) + ((double)sumG * sumG) + ((double)sumR * sumR)) / area);
        if (windowEnergy * area < 0.5)
        {
            return 0;
        }

        var numerator = dot - ((((double)template.SumBlue * sumB) + ((double)template.SumGreen * sumG) + ((double)template.SumRed * sumR)) / area);
        return numerator / Math.Sqrt(template.Energy * windowEnergy);
    }

    private static void AccumulateColorRow(ReadOnlySpan<byte> window, ReadOnlySpan<byte> reference, ref long dot, ref long sumB, ref long sumG, ref long sumR, ref long squares)
    {
        var i = 0;

        if (Vector.IsHardwareAccelerated && Vector<byte>.Count >= 16 && window.Length >= Vector<byte>.Count)
        {
            // Lanes of the 32-bit accumulators cycle through B, G, R, A. The alpha lane is ignored:
            // the template alpha is zero, and window alpha is excluded from the sums below.
            var dots = Vector<uint>.Zero;
            var sums = Vector<uint>.Zero;
            var squared = Vector<uint>.Zero;
            var pending = 0;

            for (; i <= window.Length - Vector<byte>.Count; i += Vector<byte>.Count)
            {
                Vector.Widen(new Vector<byte>(window.Slice(i)), out var wlo, out var whi);
                Vector.Widen(new Vector<byte>(reference.Slice(i)), out var rlo, out var rhi);
                Vector.Widen(wlo, out var w0, out var w1);
                Vector.Widen(whi, out var w2, out var w3);
                Vector.Widen(rlo, out var r0, out var r1);
                Vector.Widen(rhi, out var r2, out var r3);

                dots += (w0 * r0) + (w1 * r1) + (w2 * r2) + (w3 * r3);
                sums += w0 + w1 + w2 + w3;
                squared += (w0 * w0) + (w1 * w1) + (w2 * w2) + (w3 * w3);

                if (++pending == 4096)
                {
                    Flush(dots, sums, squared, ref dot, ref sumB, ref sumG, ref sumR, ref squares);
                    dots = sums = squared = Vector<uint>.Zero;
                    pending = 0;
                }
            }

            Flush(dots, sums, squared, ref dot, ref sumB, ref sumG, ref sumR, ref squares);
        }

        for (; i < window.Length; i += 4)
        {
            int b = window[i], g = window[i + 1], r = window[i + 2];
            dot += (b * reference[i]) + (g * reference[i + 1]) + (r * reference[i + 2]);
            sumB += b;
            sumG += g;
            sumR += r;
            squares += (b * b) + (g * g) + (r * r);
        }
    }

    private static void Flush(Vector<uint> dots, Vector<uint> sums, Vector<uint> squared, ref long dot, ref long sumB, ref long sumG, ref long sumR, ref long squares)
    {
        for (var lane = 0; lane < Vector<uint>.Count; lane++)
        {
            switch (lane & 3)
            {
                case 0:
                    sumB += sums[lane];
                    dot += dots[lane];
                    squares += squared[lane];
                    break;
                case 1:
                    sumG += sums[lane];
                    dot += dots[lane];
                    squares += squared[lane];
                    break;
                case 2:
                    sumR += sums[lane];
                    dot += dots[lane];
                    squares += squared[lane];
                    break;
                default:
                    break;
            }
        }
    }
}
