using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Screen;
using Askaiser.Marionette.Tests.Fakes;

namespace Askaiser.Marionette.Tests;

/// <summary>
/// These tests give frames to a recorder at chosen times, then play the video it writes like a web browser would.
/// </summary>
public class ScreenRecorderTests
{
    private static readonly DisplayInfo SmallDisplay = new(new Rectangle(0, 0, 64, 48), IsPrimary: true, 1f, 1);
    private static readonly DisplayInfo TinyDisplay = new(new Rectangle(64, 0, 96, 24), IsPrimary: false, 1f, 2);
    private static readonly TimeSpan Hold = TimeSpan.FromMilliseconds(1500);

    [Test]
    public async Task Video_ShowsEveryChange_WithoutAnyLoss()
    {
        using var context = new RecorderContext(TimeSpan.FromSeconds(10));

        var first = Picture.Noise(64, 48, seed: 1);
        var second = first.With(new Rectangle(10, 5, 30, 20), 0xFF2040C0);
        var third = second.With(new Rectangle(0, 0, 1, 1), 0xFF00FF00).With(new Rectangle(63, 47, 64, 48), 0xFFFF0000);

        context.Record(first, SmallDisplay);
        context.Time.Advance(200);
        context.Record(second, SmallDisplay);
        context.Time.Advance(300);
        context.Record(third, SmallDisplay);
        context.Time.Advance(100);

        var video = context.Encode();

        await Assert.That((video.Width, video.Height)).IsEqualTo((64, 48));
        await Assert.That(video.Frames.Count).IsEqualTo(3);

        await AssertShowsAsync(video.Frames[0], first);
        await AssertShowsAsync(video.Frames[1], second);
        await AssertShowsAsync(video.Frames[2], third);

        await Assert.That(video.Frames[0].Delay).IsEqualTo(TimeSpan.FromMilliseconds(200));
        await Assert.That(video.Frames[1].Delay).IsEqualTo(TimeSpan.FromMilliseconds(300));
        await Assert.That(video.Frames[2].Delay).IsEqualTo(TimeSpan.FromMilliseconds(100) + Hold);

        // Programs that do not play animations display what the screen looked like in the end.
        await Assert.That(video.DefaultImage.Pixels.SequenceEqual(third.ToOpaqueImage().Pixels)).IsTrue();
    }

    [Test]
    public async Task Video_WhenTheScreenDoesNotChange_IsASinglePicture()
    {
        using var context = new RecorderContext(TimeSpan.FromSeconds(10));
        var picture = Picture.Noise(64, 48, seed: 2);

        var frame = context.CreateFrame(picture, SmallDisplay);
        context.Recorder.Record(frame);
        context.Time.Advance(100);
        context.Recorder.Record(frame);
        context.Time.Advance(100);

        // A new capture that shows the same thing.
        context.Record(picture, SmallDisplay);
        context.Time.Advance(100);
        frame.Release();

        var video = context.Encode();

        var single = await Assert.That(video.Frames).HasSingleItem();
        await AssertShowsAsync(single!, picture);
        await Assert.That(single!.Delay).IsEqualTo(TimeSpan.FromMilliseconds(300) + Hold);
    }

    [Test]
    public async Task Video_OnlyCoversTheLastMoments()
    {
        using var context = new RecorderContext(TimeSpan.FromSeconds(2));

        var pictures = new Picture[5];
        pictures[0] = Picture.Noise(64, 48, seed: 3);
        for (var i = 1; i < pictures.Length; i++)
        {
            pictures[i] = pictures[i - 1].With(new Rectangle(i * 8, i * 4, (i * 8) + 12, (i * 4) + 9), 0xFF000000 | (uint)(i * 0x304050));
        }

        // One picture per second, from 0 to 4 seconds. The video is made half a second later and covers 2 seconds.
        foreach (var picture in pictures)
        {
            context.Record(picture, SmallDisplay);
            context.Time.Advance(1000);
        }

        context.Time.Advance(-500);
        var video = context.Encode();

        // It starts 2.5 seconds after the first picture, when the third one was on screen for half a second more.
        await Assert.That(video.Frames.Count).IsEqualTo(3);
        await AssertShowsAsync(video.Frames[0], pictures[2]);
        await AssertShowsAsync(video.Frames[1], pictures[3]);
        await AssertShowsAsync(video.Frames[2], pictures[4]);

        await Assert.That(video.Frames[0].Delay).IsEqualTo(TimeSpan.FromMilliseconds(500));
        await Assert.That(video.Frames[1].Delay).IsEqualTo(TimeSpan.FromMilliseconds(1000));
        await Assert.That(video.Frames[2].Delay).IsEqualTo(TimeSpan.FromMilliseconds(500) + Hold);
        await Assert.That(video.Duration - Hold).IsEqualTo(TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task Video_CanBeMadeSeveralTimes()
    {
        using var context = new RecorderContext(TimeSpan.FromSeconds(1));
        var first = Picture.Noise(64, 48, seed: 4);
        var second = first.With(new Rectangle(5, 5, 20, 20), 0xFF123456);

        context.Record(first, SmallDisplay);
        context.Time.Advance(400);
        context.Record(second, SmallDisplay);
        context.Time.Advance(400);

        var early = context.Encode();
        await Assert.That(early.Frames.Count).IsEqualTo(2);

        // Later on, the first picture is too old to be part of the video.
        context.Time.Advance(700);
        var late = context.Encode();

        var single = await Assert.That(late.Frames).HasSingleItem();
        await AssertShowsAsync(single!, second);
        await Assert.That(single!.Delay).IsEqualTo(TimeSpan.FromMilliseconds(1000) + Hold);
    }

    [Test]
    public async Task Video_WhenTheMonitorChanges_ShowsOneMonitorAfterTheOther()
    {
        using var context = new RecorderContext(TimeSpan.FromSeconds(10));

        var small = Picture.Noise(64, 48, seed: 5);
        var tiny = Picture.Noise(32, 24, seed: 6);
        var tinyChanged = tiny.With(new Rectangle(4, 4, 10, 10), 0xFFFFFFFF);
        var smallAgain = small.With(new Rectangle(40, 30, 60, 40), 0xFF000000);

        context.Record(small, SmallDisplay);
        context.Time.Advance(500);
        context.Record(tiny, TinyDisplay);
        context.Time.Advance(500);
        context.Record(tinyChanged, TinyDisplay);
        context.Time.Advance(500);
        context.Record(smallAgain, SmallDisplay);
        context.Time.Advance(500);

        var video = context.Encode();

        // The video has the size of the largest monitor. An invisible frame erases a monitor before the next one is displayed.
        await Assert.That((video.Width, video.Height)).IsEqualTo((64, 48));
        await Assert.That(video.Frames.Count).IsEqualTo(6);

        await AssertShowsAsync(video.Frames[0], small);
        await AssertShowsAsync(video.Frames[1], small);
        await AssertShowsAsync(video.Frames[2], tiny);
        await AssertShowsAsync(video.Frames[3], tinyChanged);
        await AssertShowsAsync(video.Frames[4], tinyChanged);
        await AssertShowsAsync(video.Frames[5], smallAgain);

        await Assert.That(video.Frames[0].Delay + video.Frames[1].Delay).IsEqualTo(TimeSpan.FromMilliseconds(500));
        await Assert.That(video.Frames[2].Delay).IsEqualTo(TimeSpan.FromMilliseconds(500));
        await Assert.That(video.Frames[3].Delay + video.Frames[4].Delay).IsEqualTo(TimeSpan.FromMilliseconds(500));
        await Assert.That(video.Duration - Hold).IsEqualTo(TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task Video_WhenAMonitorLeftTheLastMoments_StartsWithTheNextOne()
    {
        using var context = new RecorderContext(TimeSpan.FromSeconds(1));

        var small = Picture.Noise(64, 48, seed: 7);
        var tiny = Picture.Noise(32, 24, seed: 8);
        var tinyChanged = tiny.With(new Rectangle(0, 0, 32, 3), 0xFF808080);

        context.Record(small, SmallDisplay);
        context.Time.Advance(400);
        context.Record(tiny, TinyDisplay);
        context.Time.Advance(1500);
        context.Record(tinyChanged, TinyDisplay);
        context.Time.Advance(200);

        var video = context.Encode();

        await Assert.That((video.Width, video.Height)).IsEqualTo((32, 24));
        await Assert.That(video.Frames.Count).IsEqualTo(2);
        await AssertShowsAsync(video.Frames[0], tiny);
        await AssertShowsAsync(video.Frames[1], tinyChanged);
        await Assert.That(video.Frames[0].Delay).IsEqualTo(TimeSpan.FromMilliseconds(800));
    }

    [Test]
    [Arguments(2)]
    [Arguments(3)]
    public async Task Video_OfALargeMonitor_IsReduced(int factor)
    {
        // Monitors with more pixels than the limit are recorded at the largest size that fits.
        var settings = ScreenRecorderSettings.Default with { MaxPixels = 64 * 48 / (factor * factor) };
        using var context = new RecorderContext(TimeSpan.FromSeconds(10), settings);

        var first = Picture.Solid(64, 48, 0xFF204060);
        var second = first.With(new Rectangle(0, 0, 6 * factor, 4 * factor), 0xFFE0C0A0);

        context.Record(first, SmallDisplay);
        context.Time.Advance(100);
        context.Record(second, SmallDisplay);
        context.Time.Advance(100);

        var video = context.Encode();

        await Assert.That((video.Width, video.Height)).IsEqualTo((64 / factor, 48 / factor));
        await Assert.That(video.Frames.Count).IsEqualTo(2);

        var expectedFirst = Picture.Solid(64 / factor, 48 / factor, 0xFF204060);
        await AssertShowsAsync(video.Frames[0], expectedFirst);
        await AssertShowsAsync(video.Frames[1], expectedFirst.With(new Rectangle(0, 0, 6, 4), 0xFFE0C0A0));
    }

    [Test]
    public async Task Video_WhenMemoryIsLimited_DropsItsBeginning()
    {
        // Pictures made of noise do not compress: none of them fits in the memory the recorder is allowed to use.
        var settings = ScreenRecorderSettings.Default with { MaxBufferedBytes = 1024 };
        using var context = new RecorderContext(TimeSpan.FromSeconds(10), settings);

        var last = Picture.Noise(64, 48, seed: 0);
        for (var seed = 1; seed <= 5; seed++)
        {
            context.Record(last, SmallDisplay);
            context.Time.Advance(100);
            last = Picture.Noise(64, 48, seed);
        }

        context.Record(last, SmallDisplay);
        context.Time.Advance(100);

        var video = context.Encode();

        var single = await Assert.That(video.Frames).HasSingleItem();
        await AssertShowsAsync(single!, last);
        await Assert.That(single!.Delay).IsEqualTo(TimeSpan.FromMilliseconds(100) + Hold);
    }

    [Test]
    public async Task Video_IgnoresFramesThatAreOlderThanTheLastOne()
    {
        using var context = new RecorderContext(TimeSpan.FromSeconds(10));
        var older = Picture.Noise(64, 48, seed: 9);
        var newer = older.With(new Rectangle(1, 1, 5, 5), 0xFF000000);

        var olderFrame = context.CreateFrame(older, SmallDisplay);
        context.Record(newer, SmallDisplay);
        context.Time.Advance(100);
        context.Recorder.Record(olderFrame);
        olderFrame.Release();

        var video = context.Encode();

        var single = await Assert.That(video.Frames).HasSingleItem();
        await AssertShowsAsync(single!, newer);
    }

    [Test]
    public async Task Video_WhenNothingWasRecorded_IsNotMade()
    {
        using var context = new RecorderContext(TimeSpan.FromSeconds(10));
        using var stream = new MemoryStream();

        await Assert.That(context.Recorder.TryEncode(stream)).IsFalse();
        await Assert.That(stream.Length).IsEqualTo(0);

        context.Record(Picture.Solid(64, 48, 0xFF000000), SmallDisplay);
        context.Recorder.Dispose();

        await Assert.That(context.Recorder.TryEncode(stream)).IsFalse();
    }

    /// <summary>
    /// Checks that a frame of a video displays a picture in its top left corner, and nothing else around it.
    /// </summary>
    private static async Task AssertShowsAsync(AnimatedPngFrame frame, Picture expected)
    {
        var actual = frame.Picture;
        var mismatch = string.Empty;

        for (var y = 0; y < actual.Height && mismatch.Length == 0; y++)
        {
            for (var x = 0; x < actual.Width; x++)
            {
                var pixel = BitConverter.ToUInt32(actual.Pixels.Slice(((y * actual.Width) + x) * 4, 4));
                var expectedPixel = x < expected.Width && y < expected.Height ? expected.Pixels[(y * expected.Width) + x] | 0xFF000000 : 0u;

                if (pixel != expectedPixel)
                {
                    mismatch = $"The pixel ({x},{y}) is {pixel:X8} instead of {expectedPixel:X8}.";
                    break;
                }
            }
        }

        await Assert.That(mismatch).IsEmpty();
    }

    /// <summary>
    /// A screen content made by a test: BGRA pixels as 32-bit integers.
    /// </summary>
    private sealed record Picture(int Width, int Height, uint[] Pixels)
    {
        public static Picture Solid(int width, int height, uint color)
        {
            var pixels = new uint[width * height];
            Array.Fill(pixels, color);
            return new Picture(width, height, pixels);
        }

        public static Picture Noise(int width, int height, int seed)
        {
            var random = new Random(seed);
            var pixels = new uint[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = 0xFF000000 | (uint)random.Next(0x1000000);
            }

            return new Picture(width, height, pixels);
        }

        public Picture With(Rectangle region, uint color)
        {
            var pixels = (uint[])this.Pixels.Clone();
            for (var y = region.Top; y < region.Bottom; y++)
            {
                pixels.AsSpan((y * this.Width) + region.Left, region.Width).Fill(color);
            }

            return this with { Pixels = pixels };
        }

        public Image ToOpaqueImage()
        {
            return Image.FromBgra32(this.Width, this.Height, System.Runtime.InteropServices.MemoryMarshal.AsBytes(this.Pixels.AsSpan()));
        }
    }

    private sealed class RecorderContext : IDisposable
    {
        private readonly FakeDisplayDevice _device = new();
        private readonly ScreenService _screen;
        private long _sequence;

        public RecorderContext(TimeSpan duration, ScreenRecorderSettings? settings = null)
        {
            this._screen = new ScreenService(this._device, TimeSpan.Zero);
            this.Recorder = new ScreenRecorder(this._screen, duration, settings ?? ScreenRecorderSettings.Default, this.Time);
        }

        public ManualTimeProvider Time { get; } = new();

        public ScreenRecorder Recorder { get; }

        public ScreenFrame CreateFrame(Picture picture, DisplayInfo display)
        {
            var pixels = PixelBuffer.Rent(picture.Width, picture.Height);
            System.Runtime.InteropServices.MemoryMarshal.AsBytes(picture.Pixels.AsSpan()).CopyTo(pixels.Span);
            return new ScreenFrame(pixels, display, ++this._sequence);
        }

        public void Record(Picture picture, DisplayInfo display)
        {
            var frame = this.CreateFrame(picture, display);
            this.Recorder.Record(frame);
            frame.Release();
        }

        public AnimatedPngReader Encode()
        {
            using var stream = new MemoryStream();
            if (!this.Recorder.TryEncode(stream))
            {
                throw new InvalidOperationException("Nothing was recorded.");
            }

            return AnimatedPngReader.Read(stream.ToArray());
        }

        public void Dispose()
        {
            this.Recorder.Dispose();
            this._screen.Dispose();
            this._device.Dispose();
        }
    }
}
