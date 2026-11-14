using System;
using System.Threading;
using System.Threading.Tasks;
using Askaiser.Marionette.Platform;

namespace Askaiser.Marionette.Input;

/// <summary>
/// Turns high-level mouse gestures (click, double click, drag and drop...) into sequences of device events with human-like timings.
/// </summary>
/// <remarks>
/// Gestures can be canceled between two events, but a canceled click never leaves a button down.
/// </remarks>
internal sealed class MouseController
{
    private readonly IMouseDevice _device;
    private readonly InputTimings _timings;
    private readonly TimeProvider _timeProvider;

    public MouseController(IMouseDevice device, InputTimings timings, TimeProvider timeProvider)
    {
        this._device = device;
        this._timings = timings;
        this._timeProvider = timeProvider;
    }

    public Point GetPosition()
    {
        return this._device.GetPosition();
    }

    /// <summary>
    /// Moves the cursor to a position. Unless the speed is immediate, the cursor travels there progressively,
    /// which gives applications the hover events they expect before a click.
    /// </summary>
    public async Task MoveAsync(int x, int y, MouseSpeed speed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var duration = speed switch
        {
            MouseSpeed.Fast => this._timings.FastMove,
            MouseSpeed.Slow => this._timings.SlowMove,
            _ => TimeSpan.Zero,
        };

        if (duration > TimeSpan.Zero)
        {
            var start = this._device.GetPosition();
            if (start.X != x || start.Y != y)
            {
                var startTimestamp = this._timeProvider.GetTimestamp();
                while (true)
                {
                    var progress = this._timeProvider.GetElapsedTime(startTimestamp) / duration;
                    if (progress >= 1)
                    {
                        break;
                    }

                    // Smooth acceleration and deceleration.
                    var eased = progress * progress * (3 - (2 * progress));
                    this._device.MoveTo(
                        (int)Math.Round(start.X + ((x - start.X) * eased)),
                        (int)Math.Round(start.Y + ((y - start.Y) * eased)));

                    await Task.Delay(this._timings.MoveStep, this._timeProvider, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        this._device.MoveTo(x, y);
    }

    public Task SingleClickAsync(int x, int y, MouseSpeed speed, CancellationToken cancellationToken)
    {
        return this.ClickAsync(x, y, speed, MouseButton.Left, clicks: 1, cancellationToken);
    }

    public Task DoubleClickAsync(int x, int y, MouseSpeed speed, CancellationToken cancellationToken)
    {
        return this.ClickAsync(x, y, speed, MouseButton.Left, clicks: 2, cancellationToken);
    }

    public Task TripleClickAsync(int x, int y, MouseSpeed speed, CancellationToken cancellationToken)
    {
        return this.ClickAsync(x, y, speed, MouseButton.Left, clicks: 3, cancellationToken);
    }

    public Task RightClickAsync(int x, int y, MouseSpeed speed, CancellationToken cancellationToken)
    {
        return this.ClickAsync(x, y, speed, MouseButton.Right, clicks: 1, cancellationToken);
    }

    /// <summary>
    /// Presses the left button at a position and keeps it down.
    /// </summary>
    public async Task DragFromAsync(int x, int y, MouseSpeed speed, CancellationToken cancellationToken)
    {
        await this.MoveAsync(x, y, speed, cancellationToken).ConfigureAwait(false);
        await this.PauseAsync(this._timings.BeforeClick, cancellationToken).ConfigureAwait(false);
        this._device.Press(MouseButton.Left);
    }

    /// <summary>
    /// Moves to a position and releases the left button.
    /// </summary>
    public async Task DropToAsync(int x, int y, MouseSpeed speed, CancellationToken cancellationToken)
    {
        await this.MoveAsync(x, y, speed, cancellationToken).ConfigureAwait(false);
        await this.PauseAsync(this._timings.BeforeClick, cancellationToken).ConfigureAwait(false);
        this._device.Release(MouseButton.Left);
    }

    public async Task ScrollAsync(bool up, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this._device.Scroll(up ? 1 : -1);
        await this.PauseAsync(this._timings.AfterScroll, cancellationToken).ConfigureAwait(false);
    }

    private async Task ClickAsync(int x, int y, MouseSpeed speed, MouseButton button, int clicks, CancellationToken cancellationToken)
    {
        await this.MoveAsync(x, y, speed, cancellationToken).ConfigureAwait(false);
        await this.PauseAsync(this._timings.BeforeClick, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < clicks; i++)
        {
            if (i > 0)
            {
                await this.PauseAsync(this._timings.BetweenClicks, cancellationToken).ConfigureAwait(false);
            }

            this._device.Press(button);
            try
            {
                await this.PauseAsync(this._timings.ButtonHold, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // A button that went down always comes back up, even when the click is canceled in between.
                this._device.Release(button);
            }
        }
    }

    private Task PauseAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        if (duration > TimeSpan.Zero)
        {
            return Task.Delay(duration, this._timeProvider, cancellationToken);
        }

        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }
}
