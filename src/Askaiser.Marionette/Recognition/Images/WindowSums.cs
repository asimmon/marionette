using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Askaiser.Marionette.Recognition.Images;

/// <summary>
/// Maintains the sum and the sum of squares of a plane over a window sliding across the screen, one row of positions at a time.
/// Moving the window down by one row only costs one addition and one subtraction per column.
/// </summary>
internal sealed class WindowSums : IDisposable
{
    private readonly int[] _columnSums;
    private readonly uint[] _columnSquares;
    private readonly long[] _sums;
    private readonly long[] _squares;
    private readonly int _width;
    private readonly int _positions;

    public WindowSums(int width, int positions)
    {
        this._width = width;
        this._positions = positions;
        this._columnSums = ArrayPool<int>.Shared.Rent(width);
        this._columnSquares = ArrayPool<uint>.Shared.Rent(width);
        this._sums = ArrayPool<long>.Shared.Rent(positions);
        this._squares = ArrayPool<long>.Shared.Rent(positions);
    }

    public ReadOnlySpan<long> Sums
    {
        get => this._sums.AsSpan(0, this._positions);
    }

    public ReadOnlySpan<long> Squares
    {
        get => this._squares.AsSpan(0, this._positions);
    }

    public void Start(short[] plane, int width, int top, int windowHeight)
    {
        this._columnSums.AsSpan(0, this._width).Clear();
        this._columnSquares.AsSpan(0, this._width).Clear();

        for (var y = top; y < top + windowHeight; y++)
        {
            this.Accumulate(plane.AsSpan(y * width, width), add: true);
        }
    }

    public void MoveDown(short[] plane, int width, int top, int windowHeight)
    {
        this.Accumulate(plane.AsSpan(top * width, width), add: false);
        this.Accumulate(plane.AsSpan((top + windowHeight) * width, width), add: true);
    }

    public void ComputeRow(int windowWidth)
    {
        var columnSums = this._columnSums.AsSpan(0, this._width);
        var columnSquares = this._columnSquares.AsSpan(0, this._width);
        var sums = this._sums.AsSpan(0, this._positions);
        var squares = this._squares.AsSpan(0, this._positions);

        long sum = 0, square = 0;
        for (var x = 0; x < windowWidth; x++)
        {
            sum += columnSums[x];
            square += columnSquares[x];
        }

        var last = this._positions - 1;
        for (var x = 0; x < last; x++)
        {
            sums[x] = sum;
            squares[x] = square;
            sum += columnSums[x + windowWidth] - columnSums[x];
            square += (long)columnSquares[x + windowWidth] - columnSquares[x];
        }

        sums[last] = sum;
        squares[last] = square;
    }

    public void Dispose()
    {
        ArrayPool<int>.Shared.Return(this._columnSums);
        ArrayPool<uint>.Shared.Return(this._columnSquares);
        ArrayPool<long>.Shared.Return(this._sums);
        ArrayPool<long>.Shared.Return(this._squares);
    }

    private void Accumulate(ReadOnlySpan<short> row, bool add)
    {
        var sums = this._columnSums.AsSpan(0, this._width);
        var squares = MemoryMarshal.Cast<uint, int>(this._columnSquares.AsSpan(0, this._width));
        var x = 0;

        if (Vector.IsHardwareAccelerated)
        {
            var half = Vector<int>.Count;
            for (; x <= row.Length - Vector<short>.Count; x += Vector<short>.Count)
            {
                Vector.Widen(new Vector<short>(row.Slice(x)), out var lo, out var hi);
                var sumLo = new Vector<int>(sums.Slice(x));
                var sumHi = new Vector<int>(sums.Slice(x + half));
                var squareLo = new Vector<int>(squares.Slice(x));
                var squareHi = new Vector<int>(squares.Slice(x + half));

                if (add)
                {
                    (sumLo + lo).CopyTo(sums.Slice(x));
                    (sumHi + hi).CopyTo(sums.Slice(x + half));
                    (squareLo + (lo * lo)).CopyTo(squares.Slice(x));
                    (squareHi + (hi * hi)).CopyTo(squares.Slice(x + half));
                }
                else
                {
                    (sumLo - lo).CopyTo(sums.Slice(x));
                    (sumHi - hi).CopyTo(sums.Slice(x + half));
                    (squareLo - (lo * lo)).CopyTo(squares.Slice(x));
                    (squareHi - (hi * hi)).CopyTo(squares.Slice(x + half));
                }
            }
        }

        for (; x < row.Length; x++)
        {
            int v = row[x];
            sums[x] += add ? v : -v;
            squares[x] += add ? v * v : -(v * v);
        }
    }
}
