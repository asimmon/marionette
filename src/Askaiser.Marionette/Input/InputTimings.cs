using System;

namespace Askaiser.Marionette.Input;

/// <summary>
/// The pauses inserted between simulated input events. Applications tend to ignore input that is faster than what a person could do:
/// a click on a button that the cursor reached within the same millisecond is often missed.
/// </summary>
internal sealed record InputTimings
{
    public static InputTimings Default { get; } = new InputTimings();

    /// <summary>Gets timings without any pause, for tests that only verify the sequence of events.</summary>
    public static InputTimings Instant { get; } = new InputTimings
    {
        BeforeClick = TimeSpan.Zero,
        ButtonHold = TimeSpan.Zero,
        BetweenClicks = TimeSpan.Zero,
        AfterScroll = TimeSpan.Zero,
        FastMove = TimeSpan.Zero,
        SlowMove = TimeSpan.Zero,
        ModifierSettle = TimeSpan.Zero,
    };

    /// <summary>Gets the pause between the arrival of the cursor and a button event.</summary>
    public TimeSpan BeforeClick { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Gets how long a button stays down during a click.</summary>
    public TimeSpan ButtonHold { get; init; } = TimeSpan.FromMilliseconds(30);

    /// <summary>Gets the pause between the clicks of a double or triple click.</summary>
    public TimeSpan BetweenClicks { get; init; } = TimeSpan.FromMilliseconds(70);

    /// <summary>Gets the pause after a notch of the mouse wheel.</summary>
    public TimeSpan AfterScroll { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Gets the duration of a cursor move at <see cref="MouseSpeed.Fast"/> speed.</summary>
    public TimeSpan FastMove { get; init; } = TimeSpan.FromMilliseconds(200);

    /// <summary>Gets the duration of a cursor move at <see cref="MouseSpeed.Slow"/> speed.</summary>
    public TimeSpan SlowMove { get; init; } = TimeSpan.FromMilliseconds(700);

    /// <summary>Gets the pause after a modifier key went down or before it comes back up: some applications, macOS ones in particular, miss shortcuts that are faster than a person.</summary>
    public TimeSpan ModifierSettle { get; init; } = TimeSpan.FromMilliseconds(30);

    /// <summary>Gets the pause between two positions of the cursor during a move.</summary>
    public TimeSpan MoveStep { get; init; } = TimeSpan.FromMilliseconds(8);
}
