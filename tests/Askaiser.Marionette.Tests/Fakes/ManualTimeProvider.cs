namespace Askaiser.Marionette.Tests.Fakes;

/// <summary>
/// A clock that only moves when a test says so. Its timestamps are in milliseconds.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private long _milliseconds;

    public override long TimestampFrequency => 1000;

    public override long GetTimestamp() => Interlocked.Read(ref this._milliseconds);

    public void Advance(int milliseconds) => Interlocked.Add(ref this._milliseconds, milliseconds);
}
