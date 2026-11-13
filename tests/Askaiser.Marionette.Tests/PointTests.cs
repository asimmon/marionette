namespace Askaiser.Marionette.Tests;

public class PointTests
{
    [Test]
    public async Task Add()
    {
        var p = new Point(1, 2);
        await Assert.That(p + (1, 3)).IsEqualTo(new Point(2, 5));
        await Assert.That(p + new Point(1, 3)).IsEqualTo(new Point(2, 5));
    }

    [Test]
    public async Task Substract()
    {
        var p = new Point(1, 2);
        await Assert.That(p - (1, 3)).IsEqualTo(new Point(0, -1));
        await Assert.That(p - new Point(1, 3)).IsEqualTo(new Point(0, -1));
    }
}
