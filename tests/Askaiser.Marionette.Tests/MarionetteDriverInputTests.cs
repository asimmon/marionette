using TUnit.Assertions.Enums;
using Askaiser.Marionette.Tests.Fakes;

namespace Askaiser.Marionette.Tests;

// These tests measure durations and count attempts: running them alone keeps them independent of the load of the machine.
[NotInParallel]
public class MarionetteDriverInputTests
{
    [Test]
    [Arguments("SingleClick", new[] { "Move 60,150", "Press Left", "Release Left" })]
    [Arguments("DoubleClick", new[] { "Move 60,150", "Press Left", "Release Left", "Press Left", "Release Left" })]
    [Arguments("TripleClick", new[] { "Move 60,150", "Press Left", "Release Left", "Press Left", "Release Left", "Press Left", "Release Left" })]
    [Arguments("RightClick", new[] { "Move 60,150", "Press Right", "Release Right" })]
    [Arguments("DragFrom", new[] { "Move 60,150", "Press Left" })]
    [Arguments("DropTo", new[] { "Move 60,150", "Release Left" })]
    [Arguments("MoveTo", new[] { "Move 60,150" })]
    public async Task MouseActions_OnAnElement_ActAtItsCenter(string action, string[] expectedEvents, CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture(new DriverOptions { MouseSpeed = MouseSpeed.Immediate });
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, new Rectangle(50, 100, 70, 200));

        await (action switch
        {
            "SingleClick" => fixture.Driver.SingleClickAsync(needle, cancellationToken: cancellationToken),
            "DoubleClick" => fixture.Driver.DoubleClickAsync(needle, cancellationToken: cancellationToken),
            "TripleClick" => fixture.Driver.TripleClickAsync(needle, cancellationToken: cancellationToken),
            "RightClick" => fixture.Driver.RightClickAsync(needle, cancellationToken: cancellationToken),
            "DragFrom" => fixture.Driver.DragFromAsync(needle, cancellationToken: cancellationToken),
            "DropTo" => fixture.Driver.DropToAsync(needle, cancellationToken: cancellationToken),
            _ => fixture.Driver.MoveToAsync(needle, cancellationToken: cancellationToken),
        });

        await Assert.That(fixture.Mouse.Events).IsEquivalentTo(expectedEvents, CollectionOrdering.Matching);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    public async Task SingleClick_OnAnyMonitor_ClicksInDesktopCoordinates(int monitorIndex, CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture(new DriverOptions { MouseSpeed = MouseSpeed.Immediate });
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, new Rectangle(50, 100, 70, 200));

        var monitor = (await fixture.Driver.GetMonitorsAsync(cancellationToken))[monitorIndex];
        await fixture.Driver.SetCurrentMonitor(monitorIndex).SingleClickAsync(needle, cancellationToken: cancellationToken);

        await Assert.That(fixture.Mouse.Events[0]).IsEqualTo($"Move {monitor.Left + 60},{monitor.Top + 150}");
    }

    [Test]
    public async Task SingleClick_WhenElementIsNotVisible_DoesNotTouchTheMouse(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle);

        await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.SingleClickAsync(needle, cancellationToken: cancellationToken));

        await Assert.That(fixture.Mouse.Events).IsEmpty();
    }

    [Test]
    public async Task SingleClickAny_ClicksTheFirstVisibleElement(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture(new DriverOptions { MouseSpeed = MouseSpeed.Immediate });
        var hidden = new FakeElement("hidden");
        var visible = new FakeElement("visible");
        fixture.Recognizer.Setup(hidden);
        fixture.Recognizer.Setup(visible, new Rectangle(0, 0, 20, 10));

        await fixture.Driver.SingleClickAnyAsync([hidden, visible], cancellationToken: cancellationToken);

        await Assert.That(fixture.Mouse.Events).IsEquivalentTo(["Move 10,5", "Press Left", "Release Left"], CollectionOrdering.Matching);
    }

    [Test]
    [Arguments(MouseSpeed.Fast)]
    [Arguments(MouseSpeed.Slow)]
    public async Task MoveTo_WhenSpeedIsNotImmediate_TravelsToTheDestination(MouseSpeed speed, CancellationToken cancellationToken)
    {
        // Real timings are needed to observe the cursor travelling.
        var timings = new Input.InputTimings { FastMove = TimeSpan.FromMilliseconds(60), SlowMove = TimeSpan.FromMilliseconds(120), MoveStep = TimeSpan.FromMilliseconds(5) };
        using var fixture = new DriverFixture(new DriverOptions { MouseSpeed = speed }, timings);
        var (driver, mouse) = (fixture.Driver, fixture.Mouse);

        await driver.MoveToAsync(400, 300, cancellationToken);

        await Assert.That(mouse.Events.Count > 2).IsTrue().Because("The cursor should go through intermediate positions");
        await Assert.That(mouse.Events[^1]).IsEqualTo("Move 400,300");
        await Assert.That(driver.GetMousePositionAsync()).IsEqualTo(new Point(400, 300));
    }

    [Test]
    public async Task Scroll_SendsOneEventPerTick(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();

        await fixture.Driver.ScrollDownAsync(3, cancellationToken);
        await fixture.Driver.ScrollUpAsync(cancellationToken: cancellationToken);

        await Assert.That(fixture.Mouse.Events).IsEquivalentTo(["Scroll -1", "Scroll -1", "Scroll -1", "Scroll 1"], CollectionOrdering.Matching);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Driver.ScrollDownAsync(0, cancellationToken));
    }

    [Test]
    public async Task ScrollDownUntilVisible_ScrollsUntilTheElementShowsUp(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle, attempt => attempt >= 3 ? [new Rectangle(0, 0, 10, 10)] : []);

        await fixture.Driver.ScrollDownUntilVisibleAsync(needle, TimeSpan.FromSeconds(10), scrollTicks: 2, cancellationToken: cancellationToken);

        await Assert.That(fixture.Mouse.Events).IsEquivalentTo(["Scroll -1", "Scroll -1", "Scroll -1", "Scroll -1"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ScrollUpUntilVisible_WhenElementNeverShowsUp_Throws(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        var needle = new FakeElement("needle");
        fixture.Recognizer.Setup(needle);

        await Assert.ThrowsAsync<ElementNotFoundException>(() => fixture.Driver.ScrollUpUntilVisibleAsync(needle, TimeSpan.FromMilliseconds(100), cancellationToken: cancellationToken));
        await Assert.That(fixture.Mouse.Events.All(mouseEvent => mouseEvent == "Scroll 1")).IsTrue();
    }

    [Test]
    public async Task TypeText_TypesCharacters_AndPressesKeysForLineBreaksAndTabulations(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();

        await fixture.Driver.TypeTextAsync("user\tsecret\r\nsecond line\nthird", cancellationToken: cancellationToken);

        string[] expected = ["Type user", "Press Tab", "Release Tab", "Type secret", "Press Enter", "Release Enter", "Type second line", "Press Enter", "Release Enter", "Type third"];
        await Assert.That(fixture.Keyboard.Events).IsEquivalentTo(expected, CollectionOrdering.Matching);
    }

    [Test]
    public async Task TypeText_WhenEmpty_DoesNothing(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();

        await fixture.Driver.TypeTextAsync(string.Empty, cancellationToken: cancellationToken);

        await Assert.That(fixture.Keyboard.Events).IsEmpty();
    }

    [Test]
    public async Task KeyPress_HoldsModifiersWhilePressingTheOtherKeys(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();

        await fixture.Driver.KeyPressAsync([VirtualKeyCode.C, VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.V], cancellationToken: cancellationToken);

        string[] expected = ["Press Control", "Press Shift", "Press C", "Release C", "Press V", "Release V", "Release Shift", "Release Control"];
        await Assert.That(fixture.Keyboard.Events).IsEquivalentTo(expected, CollectionOrdering.Matching);
    }

    [Test]
    public async Task KeyPress_WithASingleKey_PressesAndReleasesIt(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();

        await fixture.Driver.KeyPressAsync(VirtualKeyCode.Escape, cancellationToken: cancellationToken);

        await Assert.That(fixture.Keyboard.Events).IsEquivalentTo(["Press Escape", "Release Escape"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task KeyDownAndKeyUp_SendSeparateEvents(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();

        await fixture.Driver.KeyDownAsync([VirtualKeyCode.LeftShift, VirtualKeyCode.A], cancellationToken: cancellationToken);
        await fixture.Driver.KeyUpAsync(VirtualKeyCode.A, cancellationToken: cancellationToken);
        await fixture.Driver.KeyUpAsync(VirtualKeyCode.LeftShift, cancellationToken: cancellationToken);

        await Assert.That(fixture.Keyboard.Events).IsEquivalentTo(["Press LeftShift", "Press A", "Release A", "Release LeftShift"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task KeyPress_WhenNoKeys_Throws(CancellationToken cancellationToken)
    {
        using var fixture = new DriverFixture();
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Driver.KeyPressAsync([], cancellationToken: cancellationToken));
    }

    [Test]
    [Arguments(VirtualKeyCode.Enter, 0x0D)]
    [Arguments(VirtualKeyCode.Escape, 0x1B)]
    [Arguments(VirtualKeyCode.A, 0x41)]
    [Arguments(VirtualKeyCode.LeftWindows, 0x5B)]
    [Arguments(VirtualKeyCode.LeftCommand, 0x5B)]
    [Arguments(VirtualKeyCode.F1, 0x70)]
    [Arguments(VirtualKeyCode.SingleQuote, 0xDE)]
    public async Task VirtualKeyCode_KeepsTheValuesOfPreviousVersions(VirtualKeyCode key, int expectedValue)
    {
        await Assert.That((int)key).IsEqualTo(expectedValue);
    }
}
