using System;
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

    private static readonly int[] BlockSizes = { 64, 32, 16, 8, 4, 2, 1 };

    private ImageTemplate(int width, int height, double threshold, bool grayscale)
    {
        this.Width = width;
        this.Height = height;
        this.Threshold = threshold;
        this.IsGrayscale = grayscale;
        this.Plane = Array.Empty<short>();
        this.Kernel = Array.Empty<float>();
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

    /// <summary>Gets the side of the square blocks averaged by the coarse search. 1 means the coarse search runs at full resolution.</summary>
    public int BlockSize { get; private set; }

    public int KernelWidth { get; private set; }

    public int KernelHeight { get; private set; }

    /// <summary>Gets the block means of the centered template plane, row by row.</summary>
    public float[] Kernel { get; private set; }

    /// <summary>Gets the sum of the kernel values. It is zero when blocks cover the whole template.</summary>
    public double KernelSum { get; private set; }

    /// <summary>Gets the energy of the kernel expanded to full resolution, in plane units.</summary>
    public double KernelEnergy { get; private set; }

    /// <summary>Gets the lowest coarse correlation a matching window can have. When it is not positive, the coarse search cannot discard anything.</summary>
    public double CoarseBound { get; private set; }

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
    /// Picks the color plane and block size that make the search cheapest, using a simple model of its cost:
    /// one multiplication per kernel value at every screen position, plus a full verification of the windows that pass.
    /// </summary>
    private void SelectCoarseSearch(short[][] planes, long[] sums, double[] energies, ColorPlaneKind[] kinds, bool normDivisor)
    {
        var bestCost = double.MaxValue;
        var bestBound = double.MinValue;
        var found = false;

        for (var p = 0; p < planes.Length; p++)
        {
            if (energies[p] <= 0)
            {
                continue;
            }

            var norm = normDivisor ? ColorPlane.SquaredNorm(kinds[p]) : 1;

            foreach (var blockSize in BlockSizes)
            {
                var kernelWidth = this.Width / blockSize;
                var kernelHeight = this.Height / blockSize;
                if (kernelWidth == 0 || kernelHeight == 0)
                {
                    continue;
                }

                var kernel = BuildKernel(planes[p], sums[p], this.Width, blockSize, kernelWidth, kernelHeight, out var kernelSum, out var kernelEnergy);

                // Share of the template energy carried by the kernel.
                var alpha2 = Math.Clamp(kernelEnergy / (norm * this.Energy), 0, 1);
                var alpha = Math.Sqrt(alpha2);
                var bound = (alpha * this.Threshold) - Math.Sqrt((1 - alpha2) * (1 - (this.Threshold * this.Threshold)));

                var taps = kernel.Length;
                double cost;
                if (bound >= MinUsefulBound)
                {
                    // Under a rough Gaussian model the coarse correlation of an unrelated window has a standard deviation of 1/sqrt(taps).
                    // Real screens are far from random (text correlates with text), hence the conservative factor.
                    var z = 0.5 * bound * Math.Sqrt(taps);
                    var passRate = Math.Max(1e-4, Math.Exp(-0.5 * z * z));
                    cost = (taps / 8.0) + (passRate * this.Area);
                }
                else
                {
                    cost = (taps / 8.0) + this.Area;
                }

                if (cost < bestCost)
                {
                    found = true;
                    bestCost = cost;
                    bestBound = bound;
                    this.PlaneKind = kinds[p];
                    this.Plane = planes[p];
                    this.PlaneSum = sums[p];
                    this.PlaneEnergy = energies[p];
                    this.BlockSize = blockSize;
                    this.KernelWidth = kernelWidth;
                    this.KernelHeight = kernelHeight;
                    this.Kernel = kernel;
                    this.KernelSum = kernelSum;
                    this.KernelEnergy = kernelEnergy;
                }
            }
        }

        if (!found)
        {
            throw new InvalidOperationException("No coarse search configuration could be selected.");
        }

        this.CoarseBound = bestBound;
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
