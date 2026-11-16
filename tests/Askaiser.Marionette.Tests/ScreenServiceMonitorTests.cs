using TUnit.Assertions.Enums;
using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Screen;

namespace Askaiser.Marionette.Tests;

/// <summary>
/// These tests describe desktops the way each operating system does, then check which monitor is reported as the primary one.
/// The primary monitor is what the operating system says it is: it cannot be deduced from the position of the monitors, because
/// X11 lets the user mark any output as primary, wherever it sits, and lets them mark none at all.
/// </summary>
public class ScreenServiceMonitorTests
{
    /// <summary>A desktop as Windows and macOS describe it: the primary monitor is the one at the origin, with a second monitor on its right.</summary>
    [Test]
    public async Task GetMonitors_WhenThePrimaryMonitorIsAtTheOrigin_ReportsItFirstAndAsThePrimaryOne(CancellationToken cancellationToken)
    {
        var monitors = await GetMonitorsAsync(cancellationToken,
            new DisplayInfo(new Rectangle(1920, 0, 3840, 1080), IsPrimary: false, 1f, 1),
            new DisplayInfo(new Rectangle(0, 0, 1920, 1080), IsPrimary: true, 1f, 2));

        await Assert.That(monitors.Select(static monitor => monitor.TopLeft)).IsEquivalentTo([new Point(0, 0), new Point(1920, 0)], CollectionOrdering.Matching);
        await Assert.That(monitors.Select(static monitor => monitor.IsPrimary)).IsEquivalentTo([true, false], CollectionOrdering.Matching);
    }

    /// <summary>
    /// A desktop as X11 describes it: the user marked the right monitor as primary with <c>xrandr --primary</c>, while the left
    /// one sits at the origin of the screen. Deducing the primary monitor from its position would name the wrong one here.
    /// </summary>
    [Test]
    public async Task GetMonitors_WhenThePrimaryMonitorIsNotAtTheOrigin_StillReportsItFirstAndAsThePrimaryOne(CancellationToken cancellationToken)
    {
        var monitors = await GetMonitorsAsync(cancellationToken,
            new DisplayInfo(new Rectangle(0, 0, 1920, 1080), IsPrimary: false, 1f, 1),
            new DisplayInfo(new Rectangle(1920, 0, 3840, 1080), IsPrimary: true, 1f, 2));

        await Assert.That(monitors.Select(static monitor => monitor.TopLeft)).IsEquivalentTo([new Point(1920, 0), new Point(0, 0)], CollectionOrdering.Matching);
        await Assert.That(monitors.Select(static monitor => monitor.IsPrimary)).IsEquivalentTo([true, false], CollectionOrdering.Matching);
    }

    /// <summary>
    /// A desktop as X11 describes it when monitors of different heights are aligned by their center: no monitor sits at the
    /// origin of the screen at all. Deducing the primary monitor from its position would name none of them here.
    /// </summary>
    [Test]
    public async Task GetMonitors_WhenNoMonitorIsAtTheOrigin_ReportsThePrimaryOneTheOperatingSystemNames(CancellationToken cancellationToken)
    {
        var monitors = await GetMonitorsAsync(cancellationToken,
            new DisplayInfo(new Rectangle(0, 180, 1920, 1260), IsPrimary: false, 1f, 1),
            new DisplayInfo(new Rectangle(1920, 0, 4480, 1440), IsPrimary: true, 1f, 2));

        await Assert.That(monitors.Select(static monitor => monitor.TopLeft)).IsEquivalentTo([new Point(1920, 0), new Point(0, 180)], CollectionOrdering.Matching);
        await Assert.That(monitors.Select(static monitor => monitor.IsPrimary)).IsEquivalentTo([true, false], CollectionOrdering.Matching);
    }

    /// <summary>
    /// A desktop as X11 describes it after <c>xrandr --noprimary</c>: no output is marked as primary. One monitor is still
    /// reported as the primary one, and it is the one closest to the origin of the desktop.
    /// </summary>
    [Test]
    public async Task GetMonitors_WhenNoMonitorIsMarkedAsPrimary_ReportsTheOneClosestToTheOrigin(CancellationToken cancellationToken)
    {
        var monitors = await GetMonitorsAsync(cancellationToken,
            new DisplayInfo(new Rectangle(1920, 0, 3840, 1080), IsPrimary: false, 1f, 1),
            new DisplayInfo(new Rectangle(0, 0, 1920, 1080), IsPrimary: false, 1f, 2));

        await Assert.That(monitors.Select(static monitor => monitor.TopLeft)).IsEquivalentTo([new Point(0, 0), new Point(1920, 0)], CollectionOrdering.Matching);
        await Assert.That(monitors.Select(static monitor => monitor.IsPrimary)).IsEquivalentTo([true, false], CollectionOrdering.Matching);
    }

    /// <summary>
    /// A desktop whose second monitor mirrors the first one: both cover the same pixels, and both may claim to be the main one.
    /// Only one monitor is reported as the primary one.
    /// </summary>
    [Test]
    public async Task GetMonitors_WhenAMonitorMirrorsThePrimaryOne_ReportsASinglePrimaryMonitor(CancellationToken cancellationToken)
    {
        var monitors = await GetMonitorsAsync(cancellationToken,
            new DisplayInfo(new Rectangle(0, 0, 1920, 1080), IsPrimary: true, 1f, 1),
            new DisplayInfo(new Rectangle(0, 0, 1920, 1080), IsPrimary: true, 1f, 2));

        await Assert.That(monitors.Select(static monitor => monitor.IsPrimary)).IsEquivalentTo([true, false], CollectionOrdering.Matching);
    }

    /// <summary>A desktop made of a single monitor, which is the primary one however the operating system positions it.</summary>
    [Test]
    public async Task GetMonitors_WhenTheDesktopHasASingleMonitor_ReportsItAsThePrimaryOne(CancellationToken cancellationToken)
    {
        var monitors = await GetMonitorsAsync(cancellationToken, new DisplayInfo(new Rectangle(100, 200, 2020, 1280), IsPrimary: false, 1f, 1));

        await Assert.That(monitors.Length).IsEqualTo(1);
        await Assert.That(monitors[0].IsPrimary).IsTrue();
        await Assert.That(monitors[0].TopLeft).IsEqualTo(new Point(100, 200));
    }

    private static async Task<MonitorDescription[]> GetMonitorsAsync(CancellationToken cancellationToken, params DisplayInfo[] displays)
    {
        using var device = new StubDisplayDevice(displays);
        using var screen = new ScreenService(device, TimeSpan.FromSeconds(1));
        return await screen.GetMonitorsAsync(cancellationToken);
    }

    /// <summary>A display device that describes a desktop without ever capturing it.</summary>
    private sealed class StubDisplayDevice(DisplayInfo[] displays) : IDisplayDevice
    {
        public IReadOnlyList<DisplayInfo> GetDisplays() => displays;

        public PixelBuffer Capture(DisplayInfo display) => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
