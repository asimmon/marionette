namespace Askaiser.Marionette.Tests;

public class RectangleTests
{
    [Test]
    [Arguments(1, 0, 0, 0)]
    [Arguments(0, 1, 0, 0)]
    [Arguments(0, 0, -1, 0)]
    [Arguments(0, 0, 0, -1)]
    public async Task WhenArgumentOutOfRangeException_Throws(int left, int top, int right, int bottom)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new Rectangle(left, top, right, bottom));
    }

    [Test]
    public async Task WidthAndHeight_Work()
    {
        var rect = new Rectangle(50, 20, 70, 90);
        await Assert.That(rect.Width).IsEqualTo(20);
        await Assert.That(rect.Height).IsEqualTo(70);
    }

    [Test]
    public async Task Deconstruct_Work()
    {
        var (left, top, right, bottom) = new Rectangle(50, 20, 70, 90);

        await Assert.That(left).IsEqualTo(50);
        await Assert.That(top).IsEqualTo(20);
        await Assert.That(right).IsEqualTo(70);
        await Assert.That(bottom).IsEqualTo(90);
    }

    [Test]
    [Arguments(10, 10, 10, 10, 10, 10)]
    [Arguments(10, 10, 20, 20, 15, 15)]
    [Arguments(10, 10, 15, 16, 12, 13)]
    public async Task Center_Works(int left, int top, int right, int bottom, int expectedCenterX, int expectedCenterY)
    {
        var rect = new Rectangle(left, top, right, bottom);
        var (centerX, centerY) = rect.Center;

        await Assert.That(centerX).IsEqualTo(expectedCenterX);
        await Assert.That(centerY).IsEqualTo(expectedCenterY);
    }

    [Test]
    public async Task Corners()
    {
        var rect = new Rectangle(75, 63, 478, 362);
        await Assert.That(rect.TopLeft).IsEqualTo(new Point(75, 63));
        await Assert.That(rect.TopRight).IsEqualTo(new Point(478, 63));
        await Assert.That(rect.BottomLeft).IsEqualTo(new Point(75, 362));
        await Assert.That(rect.BottomRight).IsEqualTo(new Point(478, 362));
    }

    [Test]
    public async Task ConstructorWithPoints()
    {
        var r1 = new Rectangle(75, 63, 478, 362);
        var r2 = new Rectangle(r1.TopLeft, r1.BottomRight);
        await Assert.That(r2).IsEqualTo(r1);
    }

    [Test]
    public async Task FromLeft()
    {
        var rect = new Rectangle(100, 200, 300, 400);
        await Assert.That(rect.FromLeft(50)).IsEqualTo(new Rectangle(100, 200, 150, 400));
    }

    [Test]
    public async Task FromRight()
    {
        var rect = new Rectangle(100, 200, 300, 400);
        await Assert.That(rect.FromRight(50)).IsEqualTo(new Rectangle(250, 200, 300, 400));
    }

    [Test]
    public async Task FromTop()
    {
        var rect = new Rectangle(100, 200, 300, 400);
        await Assert.That(rect.FromTop(50)).IsEqualTo(new Rectangle(100, 200, 300, 250));
    }

    [Test]
    public async Task FromBottom()
    {
        var rect = new Rectangle(100, 200, 300, 400);
        await Assert.That(rect.FromBottom(50)).IsEqualTo(new Rectangle(100, 350, 300, 400));
    }

    [Test]
    public async Task FromTopLeft()
    {
        var rect = new Rectangle(1, 1, 100, 100);
        await Assert.That(rect.FromTopLeft(49, 29)).IsEqualTo(new Rectangle(1, 1, 50, 30));
    }

    [Test]
    public async Task FromTopRight()
    {
        var rect = new Rectangle(1, 1, 100, 100);
        await Assert.That(rect.FromTopRight(50, 29)).IsEqualTo(new Rectangle(50, 1, 100, 30));
    }

    [Test]
    public async Task FromBottomLeft()
    {
        var rect = new Rectangle(1, 1, 100, 100);
        await Assert.That(rect.FromBottomLeft(49, 30)).IsEqualTo(new Rectangle(1, 70, 50, 100));
    }

    [Test]
    public async Task FromBottomRight()
    {
        var rect = new Rectangle(1, 1, 100, 100);
        await Assert.That(rect.FromBottomRight(50, 30)).IsEqualTo(new Rectangle(50, 70, 100, 100));
    }

    [Test]
    public async Task FromCenter()
    {
        var rect = new Rectangle(0, 0, 100, 100);
        await Assert.That(rect.FromCenter(60, 40)).IsEqualTo(new Rectangle(20, 30, 80, 70));
    }

    [Test]
    public async Task Multiply()
    {
        var rect = new Rectangle(5, 5, 20, 20);
        await Assert.That(rect * (2, 3)).IsEqualTo(new Rectangle(10, 15, 40, 60));
    }

    [Test]
    public async Task Divide()
    {
        var rect = new Rectangle(10, 15, 40, 60);
        await Assert.That(rect / (2, 3)).IsEqualTo(new Rectangle(5, 5, 20, 20));
    }

    [Test]
    public async Task BoundingCenters()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        await Assert.That(rect.CenterLeft).IsEqualTo(new Point(10, 110));
        await Assert.That(rect.CenterRight).IsEqualTo(new Point(100, 110));
        await Assert.That(rect.CenterTop).IsEqualTo(new Point(55, 20));
        await Assert.That(rect.CenterBottom).IsEqualTo(new Point(55, 200));
    }

    [Test]
    public async Task Operators()
    {
        var rect = new Rectangle(1, 2, 3, 4);

        await Assert.That(rect + (1, 2)).IsEqualTo(new Rectangle(2, 4, 4, 6));
        await Assert.That(rect + new Point(1, 2)).IsEqualTo(new Rectangle(2, 4, 4, 6));

        await Assert.That(rect - (1, 2)).IsEqualTo(new Rectangle(0, 0, 2, 2));
        await Assert.That(rect - new Point(1, 2)).IsEqualTo(new Rectangle(0, 0, 2, 2));
    }
}
