using System;
using System.Buffers;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Recognition.Images;

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
                ConvertToSingle(plane.AsSpan(y * width, width), values.AsSpan(y * rowLength, rowLength));
            }

            return;
        }

        var pool = ArrayPool<int>.Shared;
        var columnsArray = pool.Rent(width);
        var firstArray = pool.Rent(width);
        var secondArray = pool.Rent(width);

        try
        {
            // Column sums over the block height, updated incrementally from one row to the next.
            var columnSums = columnsArray.AsSpan(0, width);
            columnSums.Clear();
            for (var y = top; y < top + blockSize; y++)
            {
                AddRow(plane.AsSpan(y * width, width), columnSums, sign: 1);
            }

            for (var y = top; y < top + rows; y++)
            {
                // Horizontal sums of blockSize columns, obtained by doubling: sums of 2, then 4, then 8...
                ReadOnlySpan<int> current = columnSums;
                var target = firstArray;
                var length = width;

                for (var step = 1; step < blockSize; step *= 2)
                {
                    length -= step;
                    AddShifted(current, step, target.AsSpan(0, length));
                    current = target.AsSpan(0, length);
                    target = ReferenceEquals(target, firstArray) ? secondArray : firstArray;
                }

                var destination = values.AsSpan(y * rowLength, rowLength);
                for (var phase = 0; phase < blockSize; phase++)
                {
                    var count = phaseOffsets[phase + 1] - phaseOffsets[phase];
                    var slots = destination.Slice(phaseOffsets[phase], count);
                    for (int index = 0, x = phase; index < slots.Length; index++, x += blockSize)
                    {
                        slots[index] = current[x];
                    }
                }

                if (y + 1 < top + rows)
                {
                    AddRow(plane.AsSpan(y * width, width), columnSums, sign: -1);
                    AddRow(plane.AsSpan((y + blockSize) * width, width), columnSums, sign: 1);
                }
            }
        }
        finally
        {
            pool.Return(columnsArray);
            pool.Return(firstArray);
            pool.Return(secondArray);
        }
    }

    private static void ConvertToSingle(ReadOnlySpan<short> source, Span<float> destination)
    {
        var x = 0;

        if (Vector.IsHardwareAccelerated)
        {
            var half = Vector<int>.Count;
            for (; x <= source.Length - Vector<short>.Count; x += Vector<short>.Count)
            {
                Vector.Widen(new Vector<short>(source.Slice(x)), out var lo, out var hi);
                Vector.ConvertToSingle(lo).CopyTo(destination.Slice(x));
                Vector.ConvertToSingle(hi).CopyTo(destination.Slice(x + half));
            }
        }

        for (; x < source.Length; x++)
        {
            destination[x] = source[x];
        }
    }

    private static void AddRow(ReadOnlySpan<short> row, Span<int> columnSums, int sign)
    {
        var x = 0;

        if (Vector.IsHardwareAccelerated)
        {
            var half = Vector<int>.Count;
            for (; x <= row.Length - Vector<short>.Count; x += Vector<short>.Count)
            {
                Vector.Widen(new Vector<short>(row.Slice(x)), out var lo, out var hi);
                var first = new Vector<int>(columnSums.Slice(x));
                var second = new Vector<int>(columnSums.Slice(x + half));
                (sign > 0 ? first + lo : first - lo).CopyTo(columnSums.Slice(x));
                (sign > 0 ? second + hi : second - hi).CopyTo(columnSums.Slice(x + half));
            }
        }

        for (; x < row.Length; x++)
        {
            columnSums[x] += sign * row[x];
        }
    }

    /// <summary>
    /// Computes <c>destination[x] = source[x] + source[x + shift]</c>.
    /// </summary>
    private static void AddShifted(ReadOnlySpan<int> source, int shift, Span<int> destination)
    {
        var x = 0;

        if (Vector.IsHardwareAccelerated)
        {
            for (; x <= destination.Length - Vector<int>.Count; x += Vector<int>.Count)
            {
                (new Vector<int>(source.Slice(x)) + new Vector<int>(source.Slice(x + shift))).CopyTo(destination.Slice(x));
            }
        }

        for (; x < destination.Length; x++)
        {
            destination[x] = source[x] + source[x + shift];
        }
    }
}
