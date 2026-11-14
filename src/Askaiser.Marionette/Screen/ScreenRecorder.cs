using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Recognition;

namespace Askaiser.Marionette.Screen;

/// <summary>
/// Remembers what the current monitor displayed during the last few seconds, and turns it into a video when asked to.
/// </summary>
/// <remarks>
/// <para>
/// The recorder never stores whole images. It keeps the image the video starts with, and for every later capture, the compressed
/// rectangle of what changed since the previous one, see <see cref="AnimatedPng"/>. A screen that does not change costs a capture
/// and a comparison, which the screen service already does, and no memory. Changes that leave the recording window are drawn
/// on the starting image, so that it always is what the screen displayed at the beginning of the window.
/// </para>
/// <para>
/// Captures are shared with searches: the recorder asks the screen service for frames like a search does. At its full size,
/// a monitor is compared and compressed straight from the frame, without copying it.
/// </para>
/// </remarks>
internal sealed class ScreenRecorder : IDisposable
{
    private readonly ScreenService _screen;
    private readonly TimeSpan _duration;
    private readonly ScreenRecorderSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly CancellationTokenSource _stop = new();
    private readonly Lock _gate = new();
    private readonly Queue<Change> _changes = new();
    private readonly PooledMemoryStream _scratch = new();

    private int _monitorIndex;

    /// <summary>What the screen displayed at the beginning of the recording window, in the RGBA layout of PNG.</summary>
    private PixelBuffer? _start;
    private long _startTimestamp;

    /// <summary>The last recorded frame. Its pixels are what the next capture is compared with, unless the monitor is recorded at a reduced size.</summary>
    private ScreenFrame? _lastFrame;

    /// <summary>The last recorded image when the monitor is recorded at a reduced size, and a buffer of the same size for the next one.</summary>
    private PixelBuffer? _reduced;
    private PixelBuffer? _spare;

    private int _width;
    private int _height;
    private long _bufferedBytes;
    private bool _disposed;

    public ScreenRecorder(ScreenService screen, TimeSpan duration, ScreenRecorderSettings settings, TimeProvider timeProvider)
    {
        this._screen = screen;
        this._duration = duration;
        this._settings = settings;
        this._timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets or sets the monitor that is recorded. A video shows the monitors that were recorded one after the other.
    /// </summary>
    public int MonitorIndex
    {
        get => Volatile.Read(ref this._monitorIndex);
        set => Volatile.Write(ref this._monitorIndex, value);
    }

    /// <summary>
    /// Starts capturing the screen at regular intervals, until the recorder is disposed.
    /// </summary>
    public void Start()
    {
        var cancellationToken = this._stop.Token;
        _ = Task.Run(() => this.RunAsync(cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Adds a frame to the recording, unless it is the last recorded one or an older one.
    /// </summary>
    public void Record(ScreenFrame frame)
    {
        lock (this._gate)
        {
            if (this._disposed || ReferenceEquals(frame, this._lastFrame) || (this._lastFrame != null && frame.Sequence <= this._lastFrame.Sequence))
            {
                return;
            }

            if (!frame.TryAddReference())
            {
                return;
            }

            var timestamp = this._timeProvider.GetTimestamp();
            var source = frame.Pixels;
            var factor = this.GetReductionFactor(source.Width, source.Height);
            var width = source.Width / factor;
            var height = source.Height / factor;

            PixelBuffer? reduced = null;
            if (factor > 1)
            {
                reduced = this.TakeSpare(width, height);
                PixelOperations.Reduce(source.Span, source.Width, source.Height, factor, reduced.Span);
            }

            var pixels = reduced != null ? reduced.Span : source.Span;
            var stride = width * PixelBuffer.BytesPerPixel;

            if (this._start == null)
            {
                this._start = PixelBuffer.Rent(width, height);
                this._startTimestamp = timestamp;
                AnimatedPng.ConvertToRgba(pixels.Slice(0, stride * height), this._start.Span);
            }
            else if (width != this._width || height != this._height)
            {
                // Another monitor, of another size: there is nothing to compare its image with.
                this.AddChange(timestamp, pixels, default, width, height, 0, 0, width, height);
            }
            else if (PixelOperations.TryGetChangedBounds(this.GetLastPixels(), pixels, width, height, out var left, out var top, out var right, out var bottom))
            {
                this.AddChange(timestamp, pixels, this.GetLastPixels(), width, height, left, top, right - left, bottom - top);
            }

            this._lastFrame?.Release();
            this._lastFrame = frame;
            this._width = width;
            this._height = height;

            // The previous reduced image is where the next one will be written.
            this._spare?.Dispose();
            this._spare = this._reduced;
            this._reduced = reduced;

            this.Trim(timestamp);
        }
    }

    /// <summary>
    /// Saves a video of what was recorded, ending with the frame given as an argument.
    /// </summary>
    public async Task SaveAsync(ScreenFrame lastFrame, string path, IFailureFileWriter writer)
    {
        using var video = new PooledMemoryStream();

        // Encoding takes tens of milliseconds: it must not run on the thread of the caller, which may be the one of a user interface.
        var hasVideo = await Task.Run(
            () =>
            {
                this.Record(lastFrame);
                return this.TryEncode(video);
            },
            CancellationToken.None).ConfigureAwait(false);

        if (hasVideo)
        {
            await writer.SaveAsync(path, video.WrittenMemory, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes a video of the recording window, which ends now. Returns false when nothing was recorded.
    /// </summary>
    public bool TryEncode(Stream destination)
    {
        lock (this._gate)
        {
            if (this._disposed || this._start == null)
            {
                return false;
            }

            var now = this._timeProvider.GetTimestamp();
            this.Trim(now);

            // The video is as large as the largest monitor it shows. Smaller ones are in its top left corner, surrounded by transparency.
            var videoWidth = Math.Max(this._start.Width, this._width);
            var videoHeight = Math.Max(this._start.Height, this._height);
            var frameCount = 1 + this._changes.Count;

            var previousWidth = this._start.Width;
            var previousHeight = this._start.Height;
            foreach (var change in this._changes)
            {
                videoWidth = Math.Max(videoWidth, change.ImageWidth);
                videoHeight = Math.Max(videoHeight, change.ImageHeight);

                if (change.ImageWidth != previousWidth || change.ImageHeight != previousHeight)
                {
                    frameCount++;
                    previousWidth = change.ImageWidth;
                    previousHeight = change.ImageHeight;
                }
            }

            AnimatedPng.WriteHeader(destination, videoWidth, videoHeight, frameCount);

            // Programs that do not play animations display the last image: what the screen looked like at the time of the failure.
            this._scratch.Reset();
            AnimatedPng.CompressPadded(this.GetLastPixels(), this._width, this._height, videoWidth, videoHeight, this._scratch);
            AnimatedPng.WriteDefaultImage(destination, this._scratch.WrittenSpan);

            this._scratch.Reset();
            AnimatedPng.CompressRgba(this._start.Span, this._start.Width, this._start.Height, this._scratch);

            // A frame is written once the next one is known, as it is displayed until then.
            var sequence = 0;
            var pending = new Change(Math.Max(this._startTimestamp, now - this.ToTimestampTicks(this._duration)), 0, 0, this._start.Width, this._start.Height, this._start.Width, this._start.Height, [], 0);
            var pendingPixels = this._scratch.WrittenSpan;

            foreach (var change in this._changes)
            {
                var delay = this._timeProvider.GetElapsedTime(pending.Timestamp, change.Timestamp);
                var isOtherSize = change.ImageWidth != pending.ImageWidth || change.ImageHeight != pending.ImageHeight;

                if (isOtherSize)
                {
                    delay -= AnimatedPng.MinimumDelay;
                }

                AnimatedPng.WriteFrame(destination, ref sequence, pending.Left, pending.Top, pending.Width, pending.Height, delay, clearAfter: false, pendingPixels);

                if (isOtherSize)
                {
                    // The monitor changed for one of another size. An invisible frame makes room for it: once displayed, it erases the previous monitor.
                    this._scratch.Reset();
                    AnimatedPng.CompressTransparent(pending.ImageWidth, pending.ImageHeight, this._scratch);
                    AnimatedPng.WriteFrame(destination, ref sequence, 0, 0, pending.ImageWidth, pending.ImageHeight, AnimatedPng.MinimumDelay, clearAfter: true, this._scratch.WrittenSpan);
                }

                pending = change;
                pendingPixels = change.Data.AsSpan(0, change.Length);
            }

            var lastDelay = this._timeProvider.GetElapsedTime(pending.Timestamp, now) + this._settings.FinalImageHold;
            AnimatedPng.WriteFrame(destination, ref sequence, pending.Left, pending.Top, pending.Width, pending.Height, lastDelay, clearAfter: false, pendingPixels);
            AnimatedPng.WriteEnd(destination);

            return true;
        }
    }

    public void Dispose()
    {
        lock (this._gate)
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;
        }

        this._stop.Cancel();

        lock (this._gate)
        {
            while (this._changes.TryDequeue(out var change))
            {
                ArrayPool<byte>.Shared.Return(change.Data);
            }

            this._bufferedBytes = 0;
            this._lastFrame?.Release();
            this._lastFrame = null;
            this._start?.Dispose();
            this._start = null;
            this._reduced?.Dispose();
            this._reduced = null;
            this._spare?.Dispose();
            this._spare = null;
            this._scratch.Dispose();
        }

        this._stop.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        // A frame captured for a search less than half an interval ago is recent enough for the video.
        var maxAge = this._settings.Interval / 2;

        while (!cancellationToken.IsCancellationRequested)
        {
            var startTimestamp = this._timeProvider.GetTimestamp();

            try
            {
                var frame = await this._screen.AcquireFrameAsync(this.MonitorIndex, maxAge, cancellationToken).ConfigureAwait(false);
                try
                {
                    this.Record(frame);
                }
                finally
                {
                    frame.Release();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
#pragma warning disable CA1031 // Do not catch general exception types: whatever prevents a capture, the recording goes on
            catch (Exception)
#pragma warning restore CA1031
            {
                // The screen cannot be captured right now: the session is locked, the monitor is gone, a permission is missing...
                // Searches report these problems. A video is only an help to understand a failure and must never be the cause of one.
            }

            var elapsed = this._timeProvider.GetElapsedTime(startTimestamp);
            var rest = elapsed * ((1 / this._settings.MaxProcessorShare) - 1);
            var delay = this._settings.Interval - elapsed;

            try
            {
                await Task.Delay(delay > rest ? delay : rest, this._timeProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private int GetReductionFactor(int width, int height)
    {
        var factor = 1;
        while ((long)(width / factor) * (height / factor) > this._settings.MaxPixels)
        {
            factor++;
        }

        return factor;
    }

    private PixelBuffer TakeSpare(int width, int height)
    {
        var spare = this._spare;
        this._spare = null;

        if (spare != null && spare.Width == width && spare.Height == height)
        {
            return spare;
        }

        spare?.Dispose();
        return PixelBuffer.Rent(width, height);
    }

    private ReadOnlySpan<byte> GetLastPixels()
    {
        return this._reduced != null ? this._reduced.Span : this._lastFrame!.Pixels.Span;
    }

    private void AddChange(long timestamp, ReadOnlySpan<byte> pixels, ReadOnlySpan<byte> previousPixels, int imageWidth, int imageHeight, int left, int top, int width, int height)
    {
        this._scratch.Reset();
        AnimatedPng.CompressRegion(pixels, previousPixels, imageWidth * PixelBuffer.BytesPerPixel, left, top, width, height, this._scratch);

        var compressed = this._scratch.WrittenSpan;
        var data = ArrayPool<byte>.Shared.Rent(compressed.Length);
        compressed.CopyTo(data);

        this._changes.Enqueue(new Change(timestamp, left, top, width, height, imageWidth, imageHeight, data, compressed.Length));
        this._bufferedBytes += data.Length;
    }

    /// <summary>
    /// Draws the changes that left the recording window on the image the video starts with.
    /// </summary>
    private void Trim(long now)
    {
        while (this._changes.TryPeek(out var oldest) && (this._timeProvider.GetElapsedTime(oldest.Timestamp, now) >= this._duration || this._bufferedBytes > this._settings.MaxBufferedBytes))
        {
            _ = this._changes.Dequeue();

            if (oldest.ImageWidth != this._start!.Width || oldest.ImageHeight != this._start.Height)
            {
                // The change is the whole image of another monitor.
                this._start.Dispose();
                this._start = PixelBuffer.Rent(oldest.ImageWidth, oldest.ImageHeight);
            }

            AnimatedPng.Apply(oldest.Data.AsSpan(0, oldest.Length), this._start.Span, this._start.Stride, oldest.Left, oldest.Top, oldest.Width, oldest.Height);
            this._startTimestamp = oldest.Timestamp;

            this._bufferedBytes -= oldest.Data.Length;
            ArrayPool<byte>.Shared.Return(oldest.Data);
        }
    }

    private long ToTimestampTicks(TimeSpan duration)
    {
        return (long)(duration.TotalSeconds * this._timeProvider.TimestampFrequency);
    }

    /// <summary>
    /// What changed on the screen at a point in time: a rectangle of an image, compressed.
    /// </summary>
    private readonly record struct Change(long Timestamp, int Left, int Top, int Width, int Height, int ImageWidth, int ImageHeight, byte[] Data, int Length);
}
