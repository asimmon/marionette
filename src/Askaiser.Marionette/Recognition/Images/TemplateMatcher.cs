using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Recognition.Images;

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


    public static List<TemplateMatch> Find(SearchSurface surface, ImageTemplate template, TemplateSearchStatistics? statistics = null, CancellationToken cancellationToken = default)
    {
        var positionsX = surface.Width - template.Width + 1;
        var positionsY = surface.Height - template.Height + 1;
        var matches = new List<TemplateMatch>();

        if (positionsX <= 0 || positionsY <= 0)
        {
            return matches;
        }

        var plane = surface.GetPlane(template.PlaneKind);

        // A color search on a chroma plane also needs the brightness of each window to normalize its correlation.
        var lumaPlane = !template.IsGrayscale && template.PlaneKind != ColorPlaneKind.Sum ? surface.GetPlane(ColorPlaneKind.Sum) : null;

        var blockSums = new BlockSums[template.Levels.Length];
        for (var i = 0; i < blockSums.Length; i++)
        {
            blockSums[i] = surface.GetBlockSums(template.PlaneKind, template.Levels[i].BlockSize);
        }

        var work = (long)positionsX * positionsY * (template.CanPrune ? template.Levels[0].Kernel.Length : template.Area);
        var workers = work < ParallelWorkThreshold ? 1 : Math.Min(Environment.ProcessorCount, Math.Max(1, positionsY / 8));

        if (workers <= 1)
        {
            SearchRows(surface, template, plane, lumaPlane, blockSums, 0, positionsY, matches, statistics, cancellationToken);
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
                SearchRows(surface, template, plane, lumaPlane, blockSums, from, to, local, statistics, cancellationToken);

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
    /// Reduces all the positions above the threshold to one match per occurrence of the image.
    /// </summary>
    /// <remarks>
    /// Around an occurrence, neighboring positions also score above the threshold: together they form a group of adjacent
    /// positions, sometimes a long one when the image is made of lines, of which only the best one is kept. Then, of two
    /// matches whose rectangles overlap by more than half in both directions, only the best one is kept as well.
    /// </remarks>
    private static List<TemplateMatch> SuppressOverlaps(List<TemplateMatch> matches, int width, int height)
    {
        if (matches.Count <= 1)
        {
            return matches;
        }

        // Group adjacent positions with a disjoint-set forest.
        var indices = new Dictionary<(int X, int Y), int>(matches.Count);
        for (var i = 0; i < matches.Count; i++)
        {
            indices[(matches[i].X, matches[i].Y)] = i;
        }

        var parents = new int[matches.Count];
        for (var i = 0; i < parents.Length; i++)
        {
            parents[i] = i;
        }

        int Find(int index)
        {
            while (parents[index] != index)
            {
                parents[index] = parents[parents[index]];
                index = parents[index];
            }

            return index;
        }

        Span<(int X, int Y)> neighbors = [(-1, 0), (-1, -1), (0, -1), (1, -1)];
        for (var i = 0; i < matches.Count; i++)
        {
            foreach (var (dx, dy) in neighbors)
            {
                if (indices.TryGetValue((matches[i].X + dx, matches[i].Y + dy), out var neighbor))
                {
                    parents[Find(i)] = Find(neighbor);
                }
            }
        }

        var bestOfGroups = new Dictionary<int, TemplateMatch>();
        for (var i = 0; i < matches.Count; i++)
        {
            var group = Find(i);
            if (!bestOfGroups.TryGetValue(group, out var best) || IsBetter(matches[i], best))
            {
                bestOfGroups[group] = matches[i];
            }
        }

        var candidates = new List<TemplateMatch>(bestOfGroups.Values);
        candidates.Sort(static (a, b) => IsBetter(a, b) ? -1 : IsBetter(b, a) ? 1 : 0);

        var kept = new List<TemplateMatch>();
        var halfWidth = Math.Max(1, (width + 1) / 2);
        var halfHeight = Math.Max(1, (height + 1) / 2);

        foreach (var candidate in candidates)
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

    /// <summary>
    /// Orders matches by score. Position breaks ties so that results do not depend on how the search was split across threads.
    /// </summary>
    private static bool IsBetter(TemplateMatch a, TemplateMatch b)
    {
        if (a.Score != b.Score)
        {
            return a.Score > b.Score;
        }

        return a.Y != b.Y ? a.Y < b.Y : a.X < b.X;
    }

    private static unsafe void SearchRows(SearchSurface surface, ImageTemplate template, short[] plane, short[]? lumaPlane, BlockSums[] blockSums, int fromY, int toY, List<TemplateMatch> matches, TemplateSearchStatistics? statistics, CancellationToken cancellationToken)
    {
        var width = surface.Width;
        var templateWidth = template.Width;
        var templateHeight = template.Height;
        var inverseArea = 1.0 / template.Area;
        var flatEnergy = 0.5 * inverseArea;
        var positionsX = width - templateWidth + 1;

        var levels = template.Levels;
        var first = levels[0];
        var firstSums = blockSums[0];
        var blockSize = first.BlockSize;
        var canPrune = template.CanPrune;
        var firstLimit = first.Limit * CoarseSlack * CoarseSlack;
        var firstMeanFactor = (float)first.MeanFactor;
        var planeWeight = template.IsGrayscale ? 1.0 : 1.0 / ColorPlane.SquaredNorm(template.PlaneKind);
        var lumaWeight = 1.0 / ColorPlane.SquaredNorm(ColorPlaneKind.Sum);

        using var planeWindow = new WindowSums(width, positionsX);
        using var lumaWindow = lumaPlane != null ? new WindowSums(width, positionsX) : null;
        var accumulatorArray = ArrayPool<float>.Shared.Rent(positionsX + Vector<float>.Count);
        var candidates = new List<int>();
        var passed = new long[levels.Length + 1];

        try
        {
            planeWindow.Start(plane, width, fromY, templateHeight);
            lumaWindow?.Start(lumaPlane!, width, fromY, templateHeight);

            for (var y = fromY; y < toY; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 1. Sum and sum of squares of the plane over the window at every position of this row.
                planeWindow.ComputeRow(templateWidth);
                lumaWindow?.ComputeRow(templateWidth);

                var sums = planeWindow.Sums;
                var squares = planeWindow.Squares;
                ReadOnlySpan<long> lumaSums = default;
                ReadOnlySpan<long> lumaSquares = default;
                if (lumaWindow != null)
                {
                    lumaSums = lumaWindow.Sums;
                    lumaSquares = lumaWindow.Squares;
                }

                // Energy of the window, the quantity correlations are normalized with. Zero means the window has no contrast at all.
                double EnergyAt(int x, ReadOnlySpan<long> sums, ReadOnlySpan<long> squares, ReadOnlySpan<long> lumaSums, ReadOnlySpan<long> lumaSquares)
                {
                    var sum = (double)sums[x];
                    var energy = (squares[x] - (sum * sum * inverseArea)) * planeWeight;
                    if (!lumaSums.IsEmpty)
                    {
                        var lumaSum = (double)lumaSums[x];
                        energy += (lumaSquares[x] - (lumaSum * lumaSum * inverseArea)) * lumaWeight;
                    }

                    return energy;
                }

                // 2. First coarse test at every position, one horizontal phase at a time.
                candidates.Clear();
                if (canPrune)
                {
                    fixed (float* blockValues = firstSums.Values)
                    fixed (float* kernel = first.Kernel)
                    fixed (float* accumulator = accumulatorArray)
                    {
                        for (var phase = 0; phase < blockSize && phase < positionsX; phase++)
                        {
                            var count = ((positionsX - 1 - phase) / blockSize) + 1;
                            var rowStart = blockValues + ((long)y * firstSums.RowLength) + firstSums.PhaseOffsets[phase];
                            Correlate(rowStart, (long)blockSize * firstSums.RowLength, kernel, first.KernelWidth, first.KernelHeight, accumulator, count);

                            for (int index = 0, x = phase; index < count; index++, x += blockSize)
                            {
                                var value = accumulator[index] - (firstMeanFactor * sums[x]);
                                if (value > 0)
                                {
                                    var energy = EnergyAt(x, sums, squares, lumaSums, lumaSquares);
                                    if (energy >= flatEnergy && (double)value * value >= firstLimit * energy)
                                    {
                                        candidates.Add(x);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (var x = 0; x < positionsX; x++)
                    {
                        if (EnergyAt(x, sums, squares, lumaSums, lumaSquares) >= flatEnergy)
                        {
                            candidates.Add(x);
                        }
                    }
                }

                passed[0] += candidates.Count;

                // 3. Finer coarse tests, then the exact score, for the few positions that are left.
                foreach (var x in candidates)
                {
                    var windowSum = sums[x];
                    var energy = EnergyAt(x, sums, squares, lumaSums, lumaSquares);
                    var rejected = false;

                    for (var i = 1; i < levels.Length; i++)
                    {
                        var level = levels[i];
                        var value = CorrelateAt(blockSums[i], level, x, y) - (level.MeanFactor * windowSum);
                        if (value <= 0 || value * value < level.Limit * CoarseSlack * CoarseSlack * energy)
                        {
                            rejected = true;
                            break;
                        }

                        passed[i]++;
                    }

                    if (rejected)
                    {
                        continue;
                    }

                    var score = template.IsGrayscale
                        ? ScoreGrayscale(plane, width, template, x, y, windowSum, squares[x] - ((double)windowSum * windowSum * inverseArea))
                        : ScoreColor(surface.Pixels.Span, surface.Pixels.Stride, template, x, y);

                    if (score >= template.Threshold - ScoreTolerance)
                    {
                        matches.Add(new TemplateMatch(x, y, score));
                        passed[levels.Length]++;
                    }
                }

                if (y + 1 < toY)
                {
                    planeWindow.MoveDown(plane, width, y, templateHeight);
                    lumaWindow?.MoveDown(lumaPlane!, width, y, templateHeight);
                }
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(accumulatorArray);
        }

        statistics?.Add((long)positionsX * (toY - fromY), passed);
    }

    /// <summary>
    /// Computes the dot product between the kernel of a level and the block sums under it, at a single position.
    /// </summary>
    private static double CorrelateAt(BlockSums sums, CoarseLevel level, int x, int y)
    {
        var blockSize = level.BlockSize;
        var kernelWidth = level.KernelWidth;
        var values = sums.Values.AsSpan();
        var offset = sums.PhaseOffsets[x % blockSize] + (x / blockSize);
        var total = 0.0;

        for (var ky = 0; ky < level.KernelHeight; ky++)
        {
            var row = values.Slice(((y + (ky * blockSize)) * sums.RowLength) + offset, kernelWidth);
            var kernel = level.Kernel.AsSpan(ky * kernelWidth, kernelWidth);
            var i = 0;

            if (Vector.IsHardwareAccelerated && kernelWidth >= Vector<float>.Count)
            {
                var acc = Vector<float>.Zero;
                for (; i <= kernelWidth - Vector<float>.Count; i += Vector<float>.Count)
                {
                    acc = MultiplyAdd(new Vector<float>(kernel.Slice(i)), new Vector<float>(row.Slice(i)), acc);
                }

                total += Vector.Sum(acc);
            }

            for (; i < kernelWidth; i++)
            {
                total += kernel[i] * row[i];
            }
        }

        return total;
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
        return Vector.FusedMultiplyAdd(a, b, addend);
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
