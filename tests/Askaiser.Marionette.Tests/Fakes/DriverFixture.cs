using Askaiser.Marionette.Input;
using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Screen;

namespace Askaiser.Marionette.Tests.Fakes;

/// <summary>
/// A driver wired to fake devices, so that tests can observe what it does without a real desktop.
/// </summary>
internal sealed class DriverFixture : IDisposable
{
    public DriverFixture(DriverOptions? options = null, InputTimings? timings = null, ScreenRecorderSettings? recorderSettings = null)
    {
        this.Driver = new MarionetteDriver(
            options ?? new DriverOptions(),
            new DesktopPlatform(this.Display, this.Mouse, this.Keyboard),
            [this.Recognizer],
            this.FailureWriter,
            timings ?? InputTimings.Instant,
            recorderSettings ?? ScreenRecorderSettings.Default,
            TimeProvider.System);
    }

    public FakeDisplayDevice Display { get; } = new();

    public FakeMouseDevice Mouse { get; } = new();

    public FakeKeyboardDevice Keyboard { get; } = new();

    public FakeElementRecognizer Recognizer { get; } = new();

    public FakeFailureFileWriter FailureWriter { get; } = new();

    public MarionetteDriver Driver { get; }

    public void Dispose()
    {
        this.Driver.Dispose();
    }
}
