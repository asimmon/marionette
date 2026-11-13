using System;
using System.Collections.Generic;
using Askaiser.Marionette.Imaging;

namespace Askaiser.Marionette.Recognition.Images;

/// <summary>
/// An image to look for, analyzed once so that every search for it is as cheap as possible.
/// </summary>
/// <remarks>
/// <para>
/// The similarity between the template and a screen window is the zero-mean normalized cross-correlation (ZNCC),
/// the measure OpenCV calls <c>TM_CCOEFF_NORMED</c>. Evaluating it at every screen position is far too slow, so the search
/// first evaluates a much smaller correlation between block averages of the template and of the screen.
/// </para>
/// <para>
/// That first pass never discards a real match. Write the centered template as <c>T = K + R</c> where <c>K</c> is its
/// projection on block-constant images and <c>R</c> the remaining detail, and let <c>a² = |K|²/|T|²</c> be the share of
/// energy kept by the projection. For a window <c>I</c>, with <c>u</c> the cosine between <c>K</c> and <c>I</c>:
/// <c>ZNCC(T, I) ≤ a·u + √(1-a²)·√(1-u²)</c>. A window can therefore only reach the threshold <c>t</c> if
/// <c>u ≥ a·t - √(1-a²)·√(1-t²)</c>. Windows that pass this test are then scored exactly.
/// </para>
/// </remarks>
internal sealed class ImageTemplate
{
    /// <summary>
    /// The smallest value of the coarse correlation bound that is considered selective enough to be useful.
    /// </summary>
    private const double MinUsefulBound = 0.05;

    private const double PassRateFactor = 0.4;

    private const double MinPassRate = 1e-4;

    private static readonly int[] BlockSizes = { 64, 32, 16, 8, 4, 2, 1 };

    private ImageTemplate(int width, int height, double threshold, bool grayscale)
    {
        this.Width = width;
        this.Height = height;
        this.Threshold = threshold;
        this.IsGrayscale = grayscale;
        this.Plane = Array.Empty<short>();
        this.Bgra = Array.Empty<byte>();
    }

    public int Width { get; }

    public int Height { get; }

    public int Area
    {
        get => this.Width * this.Height;
    }

    public double Threshold { get; }

    public bool IsGrayscale { get; }

    /// <summary>Gets the color plane the coarse search runs on.</summary>
    public ColorPlaneKind PlaneKind { get; private set; }

    /// <summary>
    /// Gets the successive tests a screen position goes through before being scored exactly, from the coarsest
    /// and cheapest to the finest. The first one is evaluated at every position, the others only on what is left.
    /// </summary>
    public CoarseLevel[] Levels { get; private set; } = Array.Empty<CoarseLevel>();

    /// <summary>Gets a value indicating whether the first level can discard positions. When false, every position is scored exactly.</summary>
    public bool CanPrune
    {
        get => this.Levels.Length > 0 && this.Levels[0].Bound > 0;
    }

    /// <summary>Gets the template projected on <see cref="PlaneKind"/>.</summary>
    public short[] Plane { get; private set; }

    public long PlaneSum { get; private set; }

    /// <summary>Gets the energy (sum of squared deviations from the mean) of <see cref="Plane"/>.</summary>
    public double PlaneEnergy { get; private set; }

    /// <summary>Gets the template pixels with a zeroed alpha channel. Only used by color searches.</summary>
    public byte[] Bgra { get; private set; }

    public long SumBlue { get; private set; }

    public long SumGreen { get; private set; }

    public long SumRed { get; private set; }

    /// <summary>Gets the energy of the template: of the three color channels for a color search, of the plane for a grayscale search.</summary>
    public double Energy { get; private set; }

    /// <summary>
    /// Analyzes a BGRA image.
    /// </summary>
    /// <returns>The template, or null when the image is a single solid color and cannot be searched for.</returns>
    public static ImageTemplate? Create(ReadOnlySpan<byte> bgra, int width, int height, double threshold, bool grayscale)
    {
        var template = new ImageTemplate(width, height, threshold, grayscale);
        var area = width * height;

        if (grayscale)
        {
            var plane = new short[area];
            ColorPlane.Extract(bgra, plane, ColorPlaneKind.Sum);
            var energy = ComputeEnergy(plane, out var sum);
            if (energy <= 0)
            {
                return null;
            }

            template.Energy = energy;
            template.SelectCoarseSearch(new[] { plane }, new[] { sum }, new[] { energy }, new[] { ColorPlaneKind.Sum }, normDivisor: false);
            return template;
        }

        var pixels = bgra.Slice(0, area * PixelBuffer.BytesPerPixel).ToArray();
        long sumB = 0, sumG = 0, sumR = 0, sumSquares = 0;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            int b = pixels[i], g = pixels[i + 1], r = pixels[i + 2];
            pixels[i + 3] = 0;
            sumB += b;
            sumG += g;
            sumR += r;
            sumSquares += (b * b) + (g * g) + (r * r);
        }

        var colorEnergy = sumSquares - ((((double)sumB * sumB) + ((double)sumG * sumG) + ((double)sumR * sumR)) / area);
        if (colorEnergy <= 0.5 / area)
        {
            return null;
        }

        template.Bgra = pixels;
        template.SumBlue = sumB;
        template.SumGreen = sumG;
        template.SumRed = sumR;
        template.Energy = colorEnergy;

        var planes = new short[ColorPlane.KindCount][];
        var sums = new long[ColorPlane.KindCount];
        var energies = new double[ColorPlane.KindCount];
        var kinds = new ColorPlaneKind[ColorPlane.KindCount];

        for (var k = 0; k < ColorPlane.KindCount; k++)
        {
            kinds[k] = (ColorPlaneKind)k;
            planes[k] = new short[area];
            ColorPlane.Extract(bgra, planes[k], kinds[k]);
            energies[k] = ComputeEnergy(planes[k], out sums[k]);
        }

        template.SelectCoarseSearch(planes, sums, energies, kinds, normDivisor: true);
        return template;
    }

    private static double ComputeEnergy(ReadOnlySpan<short> plane, out long sum)
    {
        long s = 0, sq = 0;
        foreach (var v in plane)
        {
            s += v;
            sq += v * v;
        }

        sum = s;
        var energy = sq - ((double)s * s / plane.Length);
        return energy <= 0.5 / plane.Length ? 0 : energy;
    }

    /// <summary>
    /// Picks the color plane and the block sizes of the coarse tests that make a search cheapest, according to a simple
    /// model of its cost. Whatever is picked only affects speed: every combination returns the same matches.
    /// </summary>
    private void SelectCoarseSearch(short[][] planes, long[] sums, double[] energies, ColorPlaneKind[] kinds, bool normDivisor)
    {
        var bestCost = double.MaxValue;

        for (var p = 0; p < planes.Length; p++)
        {
            if (energies[p] <= 0)
            {
                continue;
            }

            var norm = normDivisor ? ColorPlane.SquaredNorm(kinds[p]) : 1;
            var candidates = new List<CoarseLevel>();
            foreach (var blockSize in BlockSizes)
            {
                if (this.Width / blockSize > 0 && this.Height / blockSize > 0)
                {
                    candidates.Add(CreateLevel(planes[p], sums[p], this.Width, blockSize, norm, this.Energy, this.Threshold));
                }
            }

            // There are at most seven block sizes: every ordered subset of them can be evaluated.
            for (var subset = 1; subset < 1 << candidates.Count; subset++)
            {
                var cost = this.EstimateCost(candidates, subset);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    this.PlaneKind = kinds[p];
                    this.Plane = planes[p];
                    this.PlaneSum = sums[p];
                    this.PlaneEnergy = energies[p];

                    var levels = new List<CoarseLevel>();
                    for (var i = 0; i < candidates.Count; i++)
                    {
                        if ((subset & (1 << i)) != 0)
                        {
                            levels.Add(candidates[i]);
                        }
                    }

                    this.Levels = levels.ToArray();
                }
            }
        }
    }

    /// <summary>
    /// Estimates the number of vector operations a search spends on a screen of typical size with a cascade of levels.
    /// </summary>
    private double EstimateCost(List<CoarseLevel> candidates, int subset)
    {
        // Assumptions of the model: a full HD screen, on which the template appears once.
        const double positions = 1920 * 1080;
        const double occurrences = 1;

        // Cost of scoring one position exactly, and fixed cost of evaluating one more level at one position.
        var exactCost = this.Area / 4.0;
        const double levelOverhead = 8;

        var cost = 0.0;
        var reaching = positions;
        var isFirst = true;

        for (var i = 0; i < candidates.Count; i++)
        {
            if ((subset & (1 << i)) == 0)
            {
                continue;
            }

            var level = candidates[i];
            if (level.Bound <= 0)
            {
                // A level that cannot discard anything is only acceptable alone, as the brute-force fallback.
                return subset == 1 << i ? positions * exactCost : double.MaxValue;
            }

            // The first level is vectorized across positions, the next ones are evaluated one position at a time.
            cost += reaching * ((level.Kernel.Length / 8.0) + (isFirst ? 0 : levelOverhead));

            // Positions that pass a level are of two kinds. Unrelated windows pass by chance: their correlation is roughly centered
            // on zero with a standard deviation of 1/sqrt(values), and real screens being far from random (text correlates with
            // text), a conservative factor is applied. Windows next to an occurrence pass because, seen through blocks, a slightly
            // shifted template still looks like itself.
            var z = PassRateFactor * level.Bound * Math.Sqrt(level.Kernel.Length);
            var passing = (positions * Math.Max(MinPassRate, Math.Exp(-0.5 * z * z))) + (occurrences * level.NeighborhoodArea);
            if (passing >= reaching)
            {
                return double.MaxValue;
            }

            reaching = passing;
            isFirst = false;
        }

        return cost + (reaching * exactCost);
    }

    private static CoarseLevel CreateLevel(short[] plane, long planeSum, int width, int blockSize, int norm, double energy, double threshold)
    {
        var kernelWidth = width / blockSize;
        var kernelHeight = plane.Length / width / blockSize;
        var kernel = BuildKernel(plane, planeSum, width, blockSize, kernelWidth, kernelHeight, out var kernelSum, out var kernelEnergy);

        // Share of the template energy carried by the kernel.
        var alpha2 = Math.Clamp(kernelEnergy / (norm * energy), 0, 1);
        var bound = (Math.Sqrt(alpha2) * threshold) - Math.Sqrt((1 - alpha2) * (1 - (threshold * threshold)));

        return new CoarseLevel(blockSize, kernelWidth, kernelHeight, kernel, kernelSum, kernelEnergy * norm, bound < MinUsefulBound ? 0 : bound, plane.Length);
    }

    private static float[] BuildKernel(ReadOnlySpan<short> plane, long planeSum, int width, int blockSize, int kernelWidth, int kernelHeight, out double kernelSum, out double kernelEnergy)
    {
        var kernel = new float[kernelWidth * kernelHeight];
        var mean = (double)planeSum / plane.Length;
        var blockArea = blockSize * blockSize;
        double sum = 0, squares = 0;

        for (var ky = 0; ky < kernelHeight; ky++)
        {
            for (var kx = 0; kx < kernelWidth; kx++)
            {
                long blockSum = 0;
                for (var y = 0; y < blockSize; y++)
                {
                    var row = plane.Slice((((ky * blockSize) + y) * width) + (kx * blockSize), blockSize);
                    foreach (var v in row)
                    {
                        blockSum += v;
                    }
                }

                // The kernel is stored in single precision: recompute its statistics from the rounded values
                // so that the bound stays valid for the kernel that is actually used.
                var value = (float)(((double)blockSum / blockArea) - mean);
                kernel[(ky * kernelWidth) + kx] = value;
                sum += value;
                squares += (double)value * value;
            }
        }

        kernelSum = sum;
        kernelEnergy = squares * blockArea;
        return kernel;
    }
}
