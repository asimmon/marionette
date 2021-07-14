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
            var bytes = await File.ReadAllBytesAsync(path);
            await using var stream = new MemoryStream(bytes);
            return (Bitmap)Image.FromStream(stream);
        }

        protected static void AssertResult(SearchResult result, params Point[] expectedCenters)
        {
            Assert.True(result.Success);
            Assert.Equal(expectedCenters.Length, result.Areas.Count);

            for (var i = 0; i < expectedCenters.Length; i++)
            {
                Assert.Equal(expectedCenters[i], result.Areas[i].Center);
            }
        }
    }
}
