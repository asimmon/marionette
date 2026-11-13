using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Platform;

namespace Askaiser.Marionette.Tests.Fakes;

/// <summary>
/// A desktop made of five monitors arranged around the primary one, including monitors with negative coordinates.
/// </summary>
internal sealed class FakeDisplayDevice : IDisplayDevice
{
    public static readonly DisplayInfo[] Displays =
    [
        new DisplayInfo(new Rectangle(1920, 0, 3840, 1080), IsPrimary: false, 1f, 1),
        new DisplayInfo(new Rectangle(0, 0, 1920, 1080), IsPrimary: true, 1f, 2),
        new DisplayInfo(new Rectangle(-1920, 0, 0, 1080), IsPrimary: false, 1f, 3),
        new DisplayInfo(new Rectangle(0, -1080, 1920, 0), IsPrimary: false, 1f, 4),
        new DisplayInfo(new Rectangle(0, 1080, 1920, 2160), IsPrimary: false, 1f, 5),
    ];

    private int _captureCount;

    public int CaptureCount => this._captureCount;

    /// <summary>
    /// Gets or sets a value indicating whether every capture differs from the previous one, like a screen showing an animation would.
    /// </summary>
    public bool ChangesOnEveryCapture { get; set; } = true;

    /// <summary>Gets or sets the image displayed by every monitor. When null, monitors display a solid color.</summary>
    public Image? Content { get; set; }

    public IReadOnlyList<DisplayInfo> GetDisplays() => Displays;

    public PixelBuffer Capture(DisplayInfo display)
    {
        var count = Interlocked.Increment(ref this._captureCount);
        var buffer = PixelBuffer.Rent(display.Bounds.Width, display.Bounds.Height);

        if (this.Content != null)
        {
            this.Content.Pixels.CopyTo(buffer.Span);
        }
        else
        {
            buffer.Span.Fill(0x40);
        }

        if (this.ChangesOnEveryCapture)
        {
            buffer.Span[0] = (byte)count;
            buffer.Span[1] = (byte)(count >> 8);
        }

        return buffer;
    }

    public void Dispose()
    {
    }
}
