using Xunit;

namespace Askaiser.Marionette.Tests;

public class PointTests
{
    [Fact]
    public void Add()
    {
        var p = new Point(1, 2);
        Assert.Equal(new Point(2, 5), p + (1, 3));
        Assert.Equal(new Point(2, 5), p + new Point(1, 3));
    }

    [Fact]
    public void Substract()
    {
        var p = new Point(1, 2);
        Assert.Equal(new Point(0, -1), p - (1, 3));
        Assert.Equal(new Point(0, -1), p - new Point(1, 3));
    }
}
