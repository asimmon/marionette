using Xunit;

namespace Askaiser.Marionette.Tests;

public abstract class BaseRecognizerTests
{
    protected static void AssertResult(SearchResult result, params Point[] expectedCenters)
    {
        Assert.True(result.Success);
        Assert.Equal(expectedCenters.Length, result.Locations.Count);

        for (var i = 0; i < expectedCenters.Length; i++)
        {
            Assert.Equal(expectedCenters[i], result.Locations[i].Center);
        }
    }
}
