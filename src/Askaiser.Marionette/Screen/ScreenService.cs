using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Askaiser.Marionette.Platform;

namespace Askaiser.Marionette.Screen;

/// <summary>
/// Lists monitors and captures them, reusing recent captures.
/// </summary>
internal sealed class ScreenService : IDisposable
{
    /// <summary>The monitors of a desktop rarely change: their list is only read again after this long.</summary>
    private static readonly TimeSpan DisplayListLifetime = TimeSpan.FromSeconds(1);

    private readonly IDisplayDevice _device;
    private readonly TimeSpan _frameLifetime;
#pragma warning disable CA2213 // Disposable fields should be disposed: callers may still be waiting for the semaphore, which holds nothing to release as its wait handle is never used
    private readonly SemaphoreSlim _captureGate = new(1, 1);
#pragma warning restore CA2213
    private readonly Lock _stateGate = new();

    private Monitor[] _monitors = [];
    private long _monitorsTimestamp;
    private ScreenFrame? _currentFrame;
    private long _currentFrameTimestamp;
    private long _currentFrameInputVersion;
    private long _inputVersion;
    private bool _disposed;

    public ScreenService(IDisplayDevice device, TimeSpan frameLifetime)
    {
        this._device = device;
        this._frameLifetime = frameLifetime;
    }

    /// <summary>
    /// Returns the monitors of the desktop: the primary one first, then the others from left to right and top to bottom.
    /// </summary>
    public async Task<MonitorDescription[]> GetMonitorsAsync(CancellationToken cancellationToken)
    {
        var monitors = await this.GetMonitorsInternalAsync(cancellationToken).ConfigureAwait(false);
        return Array.ConvertAll(monitors, static monitor => monitor.Description);
    }

    public async Task<MonitorDescription> GetMonitorAsync(int index, CancellationToken cancellationToken)
    {
        return (await this.GetMonitorInternalAsync(index, cancellationToken).ConfigureAwait(false)).Description;
    }

    /// <summary>
    /// Returns what a monitor currently displays. The caller must release the frame when done with it.
    /// </summary>
    public async Task<ScreenFrame> AcquireFrameAsync(int monitorIndex, CancellationToken cancellationToken)
    {
        var monitor = await this.GetMonitorInternalAsync(monitorIndex, cancellationToken).ConfigureAwait(false);

        if (this.TryReuseCurrentFrame(monitor.Display) is { } recent)
        {
            return recent;
        }

        // Searches that start at the same time share a single capture.
        await this._captureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (this.TryReuseCurrentFrame(monitor.Display) is { } concurrent)
            {
                return concurrent;
            }

            long inputVersion;
            lock (this._stateGate)
            {
                ObjectDisposedException.ThrowIf(this._disposed, this);
                inputVersion = this._inputVersion;
            }

            var pixels = await Task.Run(() => this._device.Capture(monitor.Display), cancellationToken).ConfigureAwait(false);

            lock (this._stateGate)
            {
                if (this._disposed)
                {
                    pixels.Dispose();
                    throw new ObjectDisposedException(nameof(ScreenService));
                }

                var previous = this._currentFrame;
                var isUnchanged = previous != null
                    && previous.Display == monitor.Display
                    && previous.Pixels.Width == pixels.Width
                    && previous.Pixels.Height == pixels.Height
                    && previous.Pixels.Span.SequenceEqual(pixels.Span);

                // Input that was simulated during the capture may not be visible in it: such a capture is not reused by searches.
                this._currentFrameTimestamp = Stopwatch.GetTimestamp();
                this._currentFrameInputVersion = inputVersion;

                if (isUnchanged && previous!.TryAddReference())
                {
                    // Nothing changed on the screen: keep the previous frame and what was already computed from it.
                    pixels.Dispose();
                    return previous;
                }

                var frame = new ScreenFrame(pixels, monitor.Display);
                _ = frame.TryAddReference();

                this._currentFrame = frame;
                previous?.Release();
                return frame;
            }
        }
        finally
        {
            this._captureGate.Release();
        }
    }

    /// <summary>
    /// Makes the next search capture the screen again, whatever the age of the last capture.
    /// Called after simulated input, which is expected to change what the screen displays.
    /// </summary>
    public void Invalidate()
    {
        lock (this._stateGate)
        {
            this._inputVersion++;
        }
    }

    public void Dispose()
    {
        lock (this._stateGate)
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;
        }

        // Let the capture in progress finish: the display device is released right after this service.
        // Callers that were waiting for their turn then find the service disposed.
        this._captureGate.Wait();
        try
        {
            lock (this._stateGate)
            {
                this._currentFrame?.Release();
                this._currentFrame = null;
            }
        }
        finally
        {
            this._captureGate.Release();
        }
    }

    private ScreenFrame? TryReuseCurrentFrame(DisplayInfo display)
    {
        if (this._frameLifetime <= TimeSpan.Zero)
        {
            return null;
        }

        lock (this._stateGate)
        {
            var frame = this._currentFrame;
            if (frame != null
                && this._currentFrameInputVersion == this._inputVersion
                && frame.Display == display
                && Stopwatch.GetElapsedTime(this._currentFrameTimestamp) <= this._frameLifetime
                && frame.TryAddReference())
            {
                return frame;
            }
        }

        return null;
    }

    private async Task<Monitor> GetMonitorInternalAsync(int index, CancellationToken cancellationToken)
    {
        var monitors = await this.GetMonitorsInternalAsync(cancellationToken).ConfigureAwait(false);
        return index >= 0 && index < monitors.Length
            ? monitors[index]
            : throw new InvalidOperationException(Messages.MonitorService_Throw_MonitorNotFound.FormatInvariant(index));
    }

    private ValueTask<Monitor[]> GetMonitorsInternalAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (this._stateGate)
        {
            if (this._monitors.Length > 0 && Stopwatch.GetElapsedTime(this._monitorsTimestamp) <= DisplayListLifetime)
            {
                return new ValueTask<Monitor[]>(this._monitors);
            }
        }

        return new ValueTask<Monitor[]>(this.ReadMonitorsAsync(cancellationToken));
    }

    private async Task<Monitor[]> ReadMonitorsAsync(CancellationToken cancellationToken)
    {
        var displays = await Task.Run(this._device.GetDisplays, cancellationToken).ConfigureAwait(false);

        var monitors = displays
            .OrderByDescending(static display => display.IsPrimary)
            .ThenBy(static display => display.Bounds.Left)
            .ThenBy(static display => display.Bounds.Top)
            .Select(static (display, index) => new Monitor(new MonitorDescription(index, display.Bounds.Left, display.Bounds.Top, display.Bounds.Right, display.Bounds.Bottom), display))
            .ToArray();

        lock (this._stateGate)
        {
            this._monitors = monitors;
            this._monitorsTimestamp = Stopwatch.GetTimestamp();
        }

        return monitors;
    }

    private sealed record Monitor(MonitorDescription Description, DisplayInfo Display);
}
