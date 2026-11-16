using Askaiser.Marionette.Platform.MacOS;

namespace Askaiser.Marionette.Tests;

/// <summary>
/// macOS is the only platform where the bounds of a monitor in pixels are not the bounds the operating system reports:
/// they are computed from the layout of the desktop in points, where a monitor of a higher pixel density takes more
/// pixels than points. These tests describe that layout, which no macOS machine is needed to compute.
/// </summary>
public class MacDisplayLayoutTests
{
    /// <summary>A 1512x982 points built-in Retina screen, captured as 3024x1964 pixels.</summary>
    private static MacDisplay Retina(uint id, double left, double top, bool isMain = false)
    {
        return new MacDisplay(id, isMain, new PointsRectangle(left, top, 1512, 982), Scale: 2);
    }

    /// <summary>A 1920x1080 external screen at standard pixel density, captured as 1920x1080 pixels.</summary>
    private static MacDisplay Standard(uint id, double left, double top, bool isMain = false)
    {
        return new MacDisplay(id, isMain, new PointsRectangle(left, top, 1920, 1080), Scale: 1);
    }

    public static IEnumerable<string> Arrangements()
    {
        yield return "single retina";
        yield return "single standard";
        yield return "standard right of retina";
        yield return "standard left of retina";
        yield return "standard above retina";
        yield return "standard below retina";
        yield return "retina right of standard";
        yield return "retina below standard";
        yield return "standard on both sides of retina";
        yield return "five standards around the main one";
        yield return "three retinas in a row";
        yield return "scaled mode next to a retina";
        yield return "standard above a retina and a standard";
        yield return "standard overlapping two monitors from above";
    }

    private static MacDisplay[] GetArrangement(string arrangement) => arrangement switch
    {
        "single retina" => [Retina(1, 0, 0, isMain: true)],
        "single standard" => [Standard(1, 0, 0, isMain: true)],
        "standard right of retina" => [Retina(1, 0, 0, isMain: true), Standard(2, 1512, 0)],
        "standard left of retina" => [Retina(1, 0, 0, isMain: true), Standard(2, -1920, 0)],
        "standard above retina" => [Retina(1, 0, 0, isMain: true), Standard(2, 0, -1080)],
        "standard below retina" => [Retina(1, 0, 0, isMain: true), Standard(2, 0, 982)],
        "retina right of standard" => [Standard(1, 0, 0, isMain: true), Retina(2, 1920, 0)],
        "retina below standard" => [Standard(1, 0, 0, isMain: true), Retina(2, 0, 1080)],
        "standard on both sides of retina" => [Retina(1, 0, 0, isMain: true), Standard(2, 1512, 0), Standard(3, -1920, 0)],
        "five standards around the main one" =>
        [
            Standard(1, 0, 0, isMain: true),
            Standard(2, 1920, 0),
            Standard(3, -1920, 0),
            Standard(4, 0, -1080),
            Standard(5, 0, 1080),
        ],
        "three retinas in a row" =>
        [
            Retina(1, 0, 0, isMain: true),
            Retina(2, 1512, 0),
            Retina(3, 3024, 0),
        ],
        // A screen in a scaled mode shows 1.75 pixels per point, which is not a whole number.
        "scaled mode next to a retina" =>
        [
            Retina(1, 0, 0, isMain: true),
            new MacDisplay(2, IsMain: false, new PointsRectangle(1512, 0, 1728, 1116), Scale: 1.75),
        ],
        // The monitor above spans both the Retina monitor and the standard one, which have different scale factors.
        "standard above a retina and a standard" =>
        [
            Retina(1, 0, 0, isMain: true),
            Standard(2, 1512, 0),
            new MacDisplay(3, IsMain: false, new PointsRectangle(0, -1080, 1920, 1080), Scale: 1),
        ],
        "standard overlapping two monitors from above" =>
        [
            Retina(1, 0, 0, isMain: true),
            Standard(2, 1512, 0),
            Standard(3, 500, -1080),
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(arrangement), arrangement, "Unknown arrangement"),
    };

    [Test]
    [MethodDataSource(nameof(Arrangements))]
    public async Task PixelBounds_NeverOverlap(string arrangement)
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement(arrangement));

        for (var i = 0; i < geometries.Length; i++)
        {
            for (var j = i + 1; j < geometries.Length; j++)
            {
                var overlap = geometries[i].Pixels.Intersect(geometries[j].Pixels);
                await Assert.That(overlap).IsNull();
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(Arrangements))]
    public async Task PixelBounds_HaveTheSizeOfACaptureOfTheirMonitor(string arrangement)
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement(arrangement));

        foreach (var geometry in geometries)
        {
            await Assert.That(geometry.Pixels.Width).IsEqualTo((int)Math.Round(geometry.Points.Width * geometry.Scale));
            await Assert.That(geometry.Pixels.Height).IsEqualTo((int)Math.Round(geometry.Points.Height * geometry.Scale));
        }
    }

    [Test]
    [MethodDataSource(nameof(Arrangements))]
    public async Task MainMonitor_IsAtTheOriginOfThePixelCoordinates(string arrangement)
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement(arrangement));

        foreach (var geometry in geometries)
        {
            if (geometry.IsMain)
            {
                await Assert.That(geometry.Pixels.TopLeft).IsEqualTo(new Point(0, 0));
            }
        }
    }

    /// <summary>
    /// What the mouse needs: a position in pixels is converted to the points of the monitor it is on, and back to the
    /// same position in pixels. Monitors whose pixel bounds overlapped would convert positions of one of them with the
    /// other, which moves the mouse to another monitor.
    /// </summary>
    [Test]
    [MethodDataSource(nameof(Arrangements))]
    public async Task PixelsAndPoints_AreExactInverses(string arrangement)
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement(arrangement));

        foreach (var geometry in geometries)
        {
            foreach (var pixel in GetSamplePixels(geometry.Pixels))
            {
                var (pointX, pointY) = MacDisplayLayout.FindFromPixels(geometries, pixel.X, pixel.Y).ToPoints(pixel.X, pixel.Y);
                var roundTrip = MacDisplayLayout.FindFromPoints(geometries, pointX, pointY).ToPixels(pointX, pointY);

                await Assert.That(roundTrip).IsEqualTo(pixel);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(Arrangements))]
    public async Task EveryPixelOfAMonitor_IsConvertedToAPointOnThatMonitor(string arrangement)
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement(arrangement));

        foreach (var geometry in geometries)
        {
            foreach (var pixel in GetSamplePixels(geometry.Pixels))
            {
                var found = MacDisplayLayout.FindFromPixels(geometries, pixel.X, pixel.Y);
                await Assert.That(found.Id).IsEqualTo(geometry.Id);

                var (pointX, pointY) = found.ToPoints(pixel.X, pixel.Y);
                await Assert.That(geometry.Points.Contains(pointX, pointY)).IsTrue();
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(Arrangements))]
    public async Task EveryPointOfAMonitor_IsConvertedToAPixelOnThatMonitor(string arrangement)
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement(arrangement));

        foreach (var geometry in geometries)
        {
            var points = geometry.Points;
            double[] xs = [points.Left, points.Left + 1, points.Left + (points.Width / 2), points.Right - 1];
            double[] ys = [points.Top, points.Top + 1, points.Top + (points.Height / 2), points.Bottom - 1];

            foreach (var x in xs)
            {
                foreach (var y in ys)
                {
                    var found = MacDisplayLayout.FindFromPoints(geometries, x, y);
                    await Assert.That(found.Id).IsEqualTo(geometry.Id);

                    var pixel = found.ToPixels(x, y);
                    await Assert.That(Contains(geometry.Pixels, pixel)).IsTrue();
                }
            }
        }
    }

    [Test]
    public async Task StandardRightOfRetina_TilesTheDesktopWithoutOverlapOrGap()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("standard right of retina"));

        await Assert.That(geometries[0].Pixels).IsEqualTo(new Rectangle(0, 0, 3024, 1964));
        await Assert.That(geometries[1].Pixels).IsEqualTo(new Rectangle(3024, 0, 4944, 1080));
    }

    [Test]
    public async Task StandardLeftOfRetina_TilesTheDesktopWithoutOverlapOrGap()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("standard left of retina"));

        await Assert.That(geometries[0].Pixels).IsEqualTo(new Rectangle(0, 0, 3024, 1964));
        await Assert.That(geometries[1].Pixels).IsEqualTo(new Rectangle(-1920, 0, 0, 1080));
    }

    [Test]
    public async Task StandardAboveRetina_TilesTheDesktopWithoutOverlapOrGap()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("standard above retina"));

        await Assert.That(geometries[0].Pixels).IsEqualTo(new Rectangle(0, 0, 3024, 1964));
        await Assert.That(geometries[1].Pixels).IsEqualTo(new Rectangle(0, -1080, 1920, 0));
    }

    [Test]
    public async Task StandardBelowRetina_TilesTheDesktopWithoutOverlapOrGap()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("standard below retina"));

        await Assert.That(geometries[0].Pixels).IsEqualTo(new Rectangle(0, 0, 3024, 1964));
        await Assert.That(geometries[1].Pixels).IsEqualTo(new Rectangle(0, 1964, 1920, 3044));
    }

    [Test]
    public async Task RetinaRightOfStandard_TilesTheDesktopWithoutOverlapOrGap()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("retina right of standard"));

        await Assert.That(geometries[0].Pixels).IsEqualTo(new Rectangle(0, 0, 1920, 1080));
        await Assert.That(geometries[1].Pixels).IsEqualTo(new Rectangle(1920, 0, 4944, 1964));
    }

    [Test]
    public async Task StandardMonitors_KeepTheirBoundsInPoints()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("five standards around the main one"));

        await Assert.That(geometries[0].Pixels).IsEqualTo(new Rectangle(0, 0, 1920, 1080));
        await Assert.That(geometries[1].Pixels).IsEqualTo(new Rectangle(1920, 0, 3840, 1080));
        await Assert.That(geometries[2].Pixels).IsEqualTo(new Rectangle(-1920, 0, 0, 1080));
        await Assert.That(geometries[3].Pixels).IsEqualTo(new Rectangle(0, -1080, 1920, 0));
        await Assert.That(geometries[4].Pixels).IsEqualTo(new Rectangle(0, 1080, 1920, 2160));
    }

    [Test]
    public async Task RetinaMonitors_DoubleTheirBoundsInPoints()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("three retinas in a row"));

        await Assert.That(geometries[0].Pixels).IsEqualTo(new Rectangle(0, 0, 3024, 1964));
        await Assert.That(geometries[1].Pixels).IsEqualTo(new Rectangle(3024, 0, 6048, 1964));
        await Assert.That(geometries[2].Pixels).IsEqualTo(new Rectangle(6048, 0, 9072, 1964));
    }

    /// <summary>
    /// The monitor above spans a Retina monitor and a standard one, so the three of them cannot tile the desktop:
    /// the layout still places each of them where its arrangement in points says, with the size of its own captures.
    /// </summary>
    [Test]
    public async Task MonitorsThatCannotTileTheDesktop_KeepTheirArrangement()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("standard above a retina and a standard"));

        await Assert.That(geometries[0].Pixels).IsEqualTo(new Rectangle(0, 0, 3024, 1964));
        await Assert.That(geometries[1].Pixels).IsEqualTo(new Rectangle(3024, 0, 4944, 1080));
        await Assert.That(geometries[2].Pixels).IsEqualTo(new Rectangle(0, -1080, 1920, 0));
    }

    /// <summary>
    /// The bug this layout fixes: the center of the external monitor used to be converted to a position in points on
    /// the built-in monitor, as the pixel bounds of both monitors overlapped.
    /// </summary>
    [Test]
    public async Task CenterOfTheSecondMonitor_IsConvertedToItsOwnCenterInPoints()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("standard right of retina"));
        var external = geometries[1];

        var (centerX, centerY) = external.Pixels.Center;
        var (pointX, pointY) = MacDisplayLayout.FindFromPixels(geometries, centerX, centerY).ToPoints(centerX, centerY);

        await Assert.That(pointX).IsEqualTo(2472d);
        await Assert.That(pointY).IsEqualTo(540d);
        await Assert.That(external.Points.Contains(pointX, pointY)).IsTrue();
        await Assert.That(geometries[0].Points.Contains(pointX, pointY)).IsFalse();
    }

    /// <summary>
    /// The cursor is reported in points, including when it is on a monitor that is not the main one.
    /// </summary>
    [Test]
    public async Task CursorOnTheSecondMonitor_IsConvertedToPixelsOnThatMonitor()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("standard right of retina"));
        var external = geometries[1];

        var pixel = MacDisplayLayout.FindFromPoints(geometries, 1512 + 960, 540).ToPixels(1512 + 960, 540);

        await Assert.That(pixel).IsEqualTo(new Point(3024 + 960, 540));
        await Assert.That(Contains(external.Pixels, pixel)).IsTrue();
    }

    [Test]
    public async Task PositionOutsideEveryMonitor_IsConvertedWithTheClosestOne()
    {
        var geometries = MacDisplayLayout.Compute(GetArrangement("standard right of retina"));

        // Below the external monitor, which is shorter than the Retina one.
        var belowExternal = MacDisplayLayout.FindFromPixels(geometries, 4000, 1500);
        await Assert.That(belowExternal.Id).IsEqualTo(2u);

        // On the left of the Retina monitor, where no monitor is.
        var leftOfRetina = MacDisplayLayout.FindFromPixels(geometries, -500, 100);
        await Assert.That(leftOfRetina.Id).IsEqualTo(1u);
    }

    [Test]
    public async Task NoDisplay_ReturnsNoGeometry()
    {
        await Assert.That(MacDisplayLayout.Compute([])).IsEmpty();
    }

    private static bool Contains(Rectangle pixels, Point point)
    {
        return point.X >= pixels.Left && point.X < pixels.Right && point.Y >= pixels.Top && point.Y < pixels.Bottom;
    }

    private static IEnumerable<Point> GetSamplePixels(Rectangle pixels)
    {
        int[] xs = [pixels.Left, pixels.Left + 1, pixels.Left + (pixels.Width / 2), pixels.Right - 1];
        int[] ys = [pixels.Top, pixels.Top + 1, pixels.Top + (pixels.Height / 2), pixels.Bottom - 1];

        foreach (var x in xs)
        {
            foreach (var y in ys)
            {
                yield return new Point(x, y);
            }
        }
    }
}
