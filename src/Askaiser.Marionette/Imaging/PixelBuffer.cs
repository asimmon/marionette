using System;
using System.Buffers;
using System.Threading;

namespace Askaiser.Marionette.Imaging;

/// <summary>
/// A 32-bit BGRA pixel buffer, stored top-down without row padding. This is the native layout of screen captures
/// on Windows, X11 and macOS, which means a captured frame never needs to be converted before being analyzed.
/// Buffers are rented from a shared pool because screen-sized arrays would otherwise be allocated on the large object heap at every capture.
/// </summary>
internal sealed class PixelBuffer : IDisposable
{
    public const int BytesPerPixel = 4;

    private readonly bool _pooled;
    private byte[]? _array;

    private PixelBuffer(byte[] array, int width, int height, bool pooled)
    {
        this._array = array;
        this._pooled = pooled;
        this.Width = width;
        this.Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public int Stride
    {
        get => this.Width * BytesPerPixel;
    }

    public int ByteLength
    {
        get => this.Width * this.Height * BytesPerPixel;
    }

    public Span<byte> Span
    {
        get => this.Array.AsSpan(0, this.ByteLength);
    }

    public Memory<byte> Memory
    {
        get => this.Array.AsMemory(0, this.ByteLength);
    }

    /// <summary>
    /// Gets the underlying array, which can be larger than <see cref="ByteLength"/> when the buffer is pooled.
    /// </summary>
    public byte[] Array
    {
        get => this._array ?? throw new ObjectDisposedException(nameof(PixelBuffer));
    }

    public static PixelBuffer Rent(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var array = ArrayPool<byte>.Shared.Rent(checked(width * height * BytesPerPixel));
        return new PixelBuffer(array, width, height, pooled: true);
    }

    /// <summary>
    /// Wraps an array that is owned by the caller. Disposing the returned buffer does not affect the array.
    /// </summary>
    public static PixelBuffer Wrap(byte[] bgra, int width, int height)
    {
        if (bgra.Length < checked(width * height * BytesPerPixel))
        {
            throw new ArgumentException(Messages.Image_Throw_InvalidPixelLength.FormatInvariant(width * height * BytesPerPixel, width, height, bgra.Length), nameof(bgra));
        }

        return new PixelBuffer(bgra, width, height, pooled: false);
    }

    public Span<byte> GetRow(int y)
    {
        return this.Array.AsSpan(y * this.Stride, this.Stride);
    }

    /// <summary>
    /// Copies a region of this buffer to a new pooled buffer.
    /// </summary>
    public PixelBuffer Crop(int left, int top, int width, int height)
    {
        var destination = Rent(width, height);
        this.CopyRegionTo(left, top, width, height, destination.Span);
        return destination;
    }

    public void CopyRegionTo(int left, int top, int width, int height, Span<byte> destination)
    {
        var rowLength = width * BytesPerPixel;
        var source = this.Span;

        for (var y = 0; y < height; y++)
        {
            source.Slice(((top + y) * this.Stride) + (left * BytesPerPixel), rowLength).CopyTo(destination.Slice(y * rowLength, rowLength));
        }
    }

    public void Dispose()
    {
        var array = Interlocked.Exchange(ref this._array, null);
        if (array != null && this._pooled)
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}
