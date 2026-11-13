using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Askaiser.Marionette.Imaging;

namespace Askaiser.Marionette.Recognition.Images;

/// <summary>
/// The part of a screenshot that images are searched in, along with the intermediate data derived from it.
/// That data only depends on the pixels, so it is computed on demand, once, and shared by every image searched in the same screenshot.
/// </summary>
internal sealed class SearchSurface : IDisposable
{
    private readonly PixelBuffer _pixels;
    private readonly bool _ownsPixels;
    private readonly ConcurrentDictionary<ColorPlaneKind, Lazy<short[]>> _planes = new();
    private readonly ConcurrentDictionary<(ColorPlaneKind Kind, int BlockSize), Lazy<BlockSums>> _blockSums = new();
    private int _disposed;

    public SearchSurface(PixelBuffer pixels, bool ownsPixels)
    {
        this._pixels = pixels;
        this._ownsPixels = ownsPixels;
    }

    public int Width
    {
        get => this._pixels.Width;
    }

    public int Height
    {
        get => this._pixels.Height;
    }

    public PixelBuffer Pixels
    {
        get => this._pixels;
    }

    /// <summary>
    /// Gets the pixels projected on a color plane, one 16-bit value per pixel.
    /// </summary>
    public short[] GetPlane(ColorPlaneKind kind)
    {
        return this._planes.GetOrAdd(kind, static (k, self) => new Lazy<short[]>(() => self.CreatePlane(k), LazyThreadSafetyMode.ExecutionAndPublication), this).Value;
    }

    /// <summary>
    /// Gets the sums of a color plane over every <paramref name="blockSize"/>-sized square of the surface.
    /// </summary>
    public BlockSums GetBlockSums(ColorPlaneKind kind, int blockSize)
    {
        return this._blockSums.GetOrAdd((kind, blockSize), static (key, self) => new Lazy<BlockSums>(() => BlockSums.Create(self.GetPlane(key.Kind), self.Width, self.Height, key.BlockSize), LazyThreadSafetyMode.ExecutionAndPublication), this).Value;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this._disposed, 1) != 0)
        {
            return;
        }

        foreach (var plane in this._planes.Values)
        {
            if (plane.IsValueCreated)
            {
                ArrayPool<short>.Shared.Return(plane.Value);
            }
        }

        foreach (var sums in this._blockSums.Values)
        {
            if (sums.IsValueCreated)
            {
                sums.Value.Dispose();
            }
        }

        if (this._ownsPixels)
        {
            this._pixels.Dispose();
        }
    }

    private short[] CreatePlane(ColorPlaneKind kind)
    {
        var width = this.Width;
        var height = this.Height;
        var plane = ArrayPool<short>.Shared.Rent(width * height);
        var pixels = this._pixels;

        const int rowsPerBatch = 64;
        var batches = (height + rowsPerBatch - 1) / rowsPerBatch;

        void ExtractBatch(int batch)
        {
            var top = batch * rowsPerBatch;
            var rows = Math.Min(rowsPerBatch, height - top);
            ColorPlane.Extract(pixels.Span.Slice(top * pixels.Stride, rows * pixels.Stride), plane.AsSpan(top * width, rows * width), kind);
        }

        if (batches > 2 && Environment.ProcessorCount > 1)
        {
            Parallel.For(0, batches, ExtractBatch);
        }
        else
        {
            for (var batch = 0; batch < batches; batch++)
            {
                ExtractBatch(batch);
            }
        }

        return plane;
    }
}

/// <summary>
/// For a block size <c>s</c>, the sum of a color plane over the <c>s×s</c> square starting at every pixel.
/// </summary>
/// <remarks>
/// A coarse search reads these sums with a stride of <c>s</c> pixels. To keep those reads contiguous in memory, and
/// therefore vectorizable, each row stores its values grouped by horizontal phase: first the sums at x = 0, s, 2s...,
/// then those at x = 1, s+1, 2s+1... and so on.
/// </remarks>
internal sealed class BlockSums : IDisposable
{
    private float[]? _values;

    private BlockSums(float[] values, int blockSize, int rowLength, int rowCount, int[] phaseOffsets)
    {
        this._values = values;
        this.BlockSize = blockSize;
        this.RowLength = rowLength;
        this.RowCount = rowCount;
        this.PhaseOffsets = phaseOffsets;
    }

    public int BlockSize { get; }

    /// <summary>Gets the number of values per row, which is the number of horizontal positions a block fits at.</summary>
    public int RowLength { get; }

    public int RowCount { get; }

    /// <summary>Gets the index, within a row, of the first value of each horizontal phase.</summary>
    public int[] PhaseOffsets { get; }

    public float[] Values
    {
        get => this._values ?? throw new ObjectDisposedException(nameof(BlockSums));
    }

    public static BlockSums Create(short[] plane, int width, int height, int blockSize)
    {
        var rowLength = width - blockSize + 1;
        var rowCount = height - blockSize + 1;
        if (rowLength <= 0 || rowCount <= 0)
        {
            return new BlockSums(Array.Empty<float>(), blockSize, 0, 0, new int[blockSize + 1]);
        }

        var phaseOffsets = new int[blockSize + 1];
        for (var phase = 0; phase < blockSize; phase++)
        {
            var count = phase < rowLength ? ((rowLength - 1 - phase) / blockSize) + 1 : 0;
            phaseOffsets[phase + 1] = phaseOffsets[phase] + count;
        }

        var values = ArrayPool<float>.Shared.Rent(rowLength * rowCount);

        const int rowsPerBatch = 64;
        var batches = (rowCount + rowsPerBatch - 1) / rowsPerBatch;

        void ComputeBatch(int batch)
        {
            var top = batch * rowsPerBatch;
            ComputeRows(plane, width, blockSize, phaseOffsets, values, rowLength, top, Math.Min(rowsPerBatch, rowCount - top));
        }

        if (batches > 2 && Environment.ProcessorCount > 1)
        {
            Parallel.For(0, batches, ComputeBatch);
        }
        else
        {
            for (var batch = 0; batch < batches; batch++)
            {
                ComputeBatch(batch);
            }
        }

        return new BlockSums(values, blockSize, rowLength, rowCount, phaseOffsets);
    }

    public void Dispose()
    {
        var values = Interlocked.Exchange(ref this._values, null);
        if (values is { Length: > 0 })
        {
            ArrayPool<float>.Shared.Return(values);
        }
    }

    private static void ComputeRows(short[] plane, int width, int blockSize, int[] phaseOffsets, float[] values, int rowLength, int top, int rows)
    {
        if (blockSize == 1)
        {
            for (var y = top; y < top + rows; y++)
            {
                var source = plane.AsSpan(y * width, width);
                var destination = values.AsSpan(y * rowLength, rowLength);
                for (var x = 0; x < source.Length; x++)
                {
                    destination[x] = source[x];
                }
            }

            return;
        }

        // Column sums over the block height, updated incrementally from one row to the next.
        var columns = ArrayPool<int>.Shared.Rent(width);
        try
        {
            var columnSums = columns.AsSpan(0, width);
            columnSums.Clear();

            for (var y = top; y < top + blockSize; y++)
            {
                var row = plane.AsSpan(y * width, width);
                for (var x = 0; x < width; x++)
                {
                    columnSums[x] += row[x];
                }
            }

            for (var y = top; y < top + rows; y++)
            {
                var destination = values.AsSpan(y * rowLength, rowLength);

                var running = 0;
                for (var x = 0; x < blockSize; x++)
                {
                    running += columnSums[x];
                }

                // Sliding horizontal sum, written to the position of its phase.
                var position = 0;
                for (var index = 0; position < rowLength; index++)
                {
                    for (var phase = 0; phase < blockSize && position < rowLength; phase++, position++)
                    {
                        destination[phaseOffsets[phase] + index] = running;
                        if (position + blockSize < width)
                        {
                            running += columnSums[position + blockSize] - columnSums[position];
                        }
                    }
                }

                if (y + 1 < top + rows)
                {
                    var leaving = plane.AsSpan(y * width, width);
                    var entering = plane.AsSpan((y + blockSize) * width, width);
                    for (var x = 0; x < width; x++)
                    {
                        columnSums[x] += entering[x] - leaving[x];
                    }
                }
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(columns);
        }
    }
}
