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
