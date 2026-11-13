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
    private readonly SemaphoreSlim _captureGate = new(1, 1);
    private readonly Lock _stateGate = new();

    private Monitor[] _monitors = [];
    private long _monitorsTimestamp;
    private ScreenFrame? _currentFrame;
    private long _currentFrameTimestamp;
    private bool _disposed;

    public ScreenService(IDisplayDevice device, TimeSpan frameLifetime)
    {
        this._device = device;
        this._frameLifetime = frameLifetime;
    }

    /// <summary>
    /// Returns the monitors of the desktop: the primary one first, then the others from left to right and top to bottom.
    /// </summary>
    public async Task<MonitorDescription[]> GetMonitorsAsync()
    {
        var monitors = await this.GetMonitorsInternalAsync().ConfigureAwait(false);
        return Array.ConvertAll(monitors, static monitor => monitor.Description);
    }

    public async Task<MonitorDescription> GetMonitorAsync(int index)
    {
        return (await this.GetMonitorInternalAsync(index).ConfigureAwait(false)).Description;
    }

    /// <summary>
    /// Returns what a monitor currently displays. The caller must release the frame when done with it.
    /// </summary>
    public async Task<ScreenFrame> AcquireFrameAsync(int monitorIndex, CancellationToken cancellationToken)
    {
        var monitor = await this.GetMonitorInternalAsync(monitorIndex).ConfigureAwait(false);

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

            var pixels = await Task.Run(() => this._device.Capture(monitor.Display), cancellationToken).ConfigureAwait(false);

            lock (this._stateGate)
            {
                ObjectDisposedException.ThrowIf(this._disposed, this);

                var previous = this._currentFrame;
                var isUnchanged = previous != null
                    && previous.Display == monitor.Display
                    && previous.Pixels.Width == pixels.Width
                    && previous.Pixels.Height == pixels.Height
                    && previous.Pixels.Span.SequenceEqual(pixels.Span);

                if (isUnchanged && previous!.TryAddReference())
                {
                    // Nothing changed on the screen: keep the previous frame and what was already computed from it.
                    pixels.Dispose();
                    this._currentFrameTimestamp = Stopwatch.GetTimestamp();
                    return previous;
                }

                var frame = new ScreenFrame(pixels, monitor.Display);
                _ = frame.TryAddReference();

                this._currentFrame = frame;
                this._currentFrameTimestamp = Stopwatch.GetTimestamp();
                previous?.Release();
                return frame;
            }
        }
        finally
        {
            this._captureGate.Release();
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
            this._currentFrame?.Release();
            this._currentFrame = null;
        }

        this._captureGate.Dispose();
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
            if (frame != null && frame.Display == display && Stopwatch.GetElapsedTime(this._currentFrameTimestamp) <= this._frameLifetime && frame.TryAddReference())
            {
                return frame;
            }
        }

        return null;
    }

    private async Task<Monitor> GetMonitorInternalAsync(int index)
    {
        var monitors = await this.GetMonitorsInternalAsync().ConfigureAwait(false);
        return index >= 0 && index < monitors.Length
            ? monitors[index]
            : throw new InvalidOperationException(Messages.MonitorService_Throw_MonitorNotFound.FormatInvariant(index));
    }

    private async Task<Monitor[]> GetMonitorsInternalAsync()
    {
        lock (this._stateGate)
        {
            if (this._monitors.Length > 0 && Stopwatch.GetElapsedTime(this._monitorsTimestamp) <= DisplayListLifetime)
            {
                return this._monitors;
            }
        }

        var displays = await Task.Run(this._device.GetDisplays).ConfigureAwait(false);

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
