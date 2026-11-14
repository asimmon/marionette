using System;
using System.Buffers;
using System.IO;

namespace Askaiser.Marionette.Imaging;

/// <summary>
/// A write-only stream that stores what it receives in an array rented from the shared pool, and that can be reused.
/// Compressed images are megabytes long: this keeps them out of the large object heap.
/// </summary>
internal sealed class PooledMemoryStream : Stream
{
    private const int MinimumCapacity = 4096;

    private byte[] _buffer = [];
    private int _length;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => this._length;

    public override long Position
    {
        get => this._length;
        set => throw new NotSupportedException();
    }

    public ReadOnlySpan<byte> WrittenSpan => this._buffer.AsSpan(0, this._length);

    public ReadOnlyMemory<byte> WrittenMemory => this._buffer.AsMemory(0, this._length);

    /// <summary>
    /// Forgets what was written, and keeps the memory for what comes next.
    /// </summary>
    public void Reset()
    {
        this._length = 0;
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        this.EnsureCapacity(checked(this._length + buffer.Length));
        buffer.CopyTo(this._buffer.AsSpan(this._length));
        this._length += buffer.Length;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        this.Write(buffer.AsSpan(offset, count));
    }

    public override void WriteByte(byte value)
    {
        this.EnsureCapacity(checked(this._length + 1));
        this._buffer[this._length++] = value;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && this._buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(this._buffer);
            this._buffer = [];
            this._length = 0;
        }

        base.Dispose(disposing);
    }

    private void EnsureCapacity(int capacity)
    {
        if (capacity <= this._buffer.Length)
        {
            return;
        }

        var larger = ArrayPool<byte>.Shared.Rent(Math.Max(capacity, Math.Max(MinimumCapacity, this._buffer.Length * 2)));
        this._buffer.AsSpan(0, this._length).CopyTo(larger);

        if (this._buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(this._buffer);
        }

        this._buffer = larger;
    }
}
