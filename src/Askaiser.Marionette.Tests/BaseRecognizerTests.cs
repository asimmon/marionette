using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public abstract class BaseRecognizerTests
    {
        protected static async Task<Bitmap> BitmapFromFile(string path)
        {
#if NETFRAMEWORK
            await Task.Yield();
            var bytes = File.ReadAllBytes(path);
#else
            var bytes = await File.ReadAllBytesAsync(path);
#endif
            using var ms = new MemoryStream(bytes);
            return (Bitmap)Image.FromStream(ms);
        }

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
}
