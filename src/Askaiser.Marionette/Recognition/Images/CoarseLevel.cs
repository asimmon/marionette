using System;

namespace Askaiser.Marionette.Recognition.Images;

/// <summary>
/// One coarse test of an <see cref="ImageTemplate"/>: the block means of the centered template for a given block size.
/// </summary>
internal sealed class CoarseLevel
{
    public CoarseLevel(int blockSize, int kernelWidth, int kernelHeight, float[] kernel, double kernelSum, double normalizedKernelEnergy, double bound, int templateArea)
    {
        this.BlockSize = blockSize;
        this.KernelWidth = kernelWidth;
        this.KernelHeight = kernelHeight;
        this.Kernel = kernel;
        this.Bound = bound;

        // A window passes when (correlation - MeanFactor * windowSum)² >= Limit * windowEnergy, with a positive left-hand side.
        this.MeanFactor = kernelSum * blockSize * blockSize / templateArea;
        this.Limit = bound * bound * normalizedKernelEnergy;

        var radiusX = SelfSimilarityRadius(kernel, kernelWidth, kernelHeight, bound, horizontal: true);
        var radiusY = SelfSimilarityRadius(kernel, kernelWidth, kernelHeight, bound, horizontal: false);
        this.NeighborhoodArea = 4.0 * radiusX * radiusY * blockSize * blockSize;
    }

    /// <summary>Gets the side of the square blocks. 1 means the test runs at full resolution.</summary>
    public int BlockSize { get; }

    public int KernelWidth { get; }

    public int KernelHeight { get; }

    /// <summary>Gets the block means of the centered template plane, row by row.</summary>
    public float[] Kernel { get; }

    /// <summary>Gets the lowest normalized correlation a matching window can have at this level, or zero when the level cannot discard anything.</summary>
    public double Bound { get; }

    public double MeanFactor { get; }

    public double Limit { get; }

    /// <summary>
    /// Gets an estimate of the number of screen positions around an occurrence of the template that pass this level.
    /// It is derived from how far the kernel can be shifted before it stops resembling itself.
    /// </summary>
    public double NeighborhoodArea { get; }

    private static int SelfSimilarityRadius(float[] kernel, int width, int height, double bound, bool horizontal)
    {
        const int maxShift = 16;
        var limit = Math.Min(maxShift, (horizontal ? width : height) - 1);

        for (var shift = 1; shift <= limit; shift++)
        {
            double dot = 0, first = 0, second = 0;
            var columns = horizontal ? width - shift : width;
            var rows = horizontal ? height : height - shift;
            var offset = horizontal ? shift : shift * width;

            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < columns; x++)
                {
                    var a = kernel[(y * width) + x];
                    var b = kernel[(y * width) + x + offset];
                    dot += a * b;
                    first += a * a;
                    second += b * b;
                }
            }

            if (first <= 0 || second <= 0 || dot / Math.Sqrt(first * second) < bound)
            {
                return shift;
            }
        }

        return limit + 1;
    }
}
