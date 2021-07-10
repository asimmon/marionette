using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.UITesting.Tests
{
    public class ImageElementRecognizerTests
    {
        private readonly ImageElementRecognizer _recognizer;

        public ImageElementRecognizerTests()
        {
            this._recognizer = new ImageElementRecognizer();
        }

        [Theory]
        [InlineData(0, 0, 200, 100, "0.95", false, 100, 50)]
        [InlineData(1000, 300, 1100, 400, "0.95", false, 1050, 350)]
        [InlineData(20, 620, 162, 660, "0.95", false, 91, 640)]
        [InlineData(0, 0, 200, 100, "0.8", false, 100, 50)]
        [InlineData(1000, 300, 1100, 400, "0.8", false, 1050, 350)]
        [InlineData(20, 620, 162, 660, "0.8", false, 91, 640)]
        [InlineData(0, 0, 200, 100, "0.95", true, 100, 50)]
        [InlineData(1000, 300, 1100, 400, "0.95", true, 1050, 350)]
        [InlineData(20, 620, 162, 660, "0.95", true, 91, 640)]
        public async Task Recognize_WhenSingleMatch_Works(int x1, int y1, int x2, int y2, string threshold, bool grayscale, int expectedX, int expectedY)
        {
            using var screenshot = await BitmapFromFile("./images/google-news.png");
            using var searched = screenshot.Crop(new Rectangle(x1, y1, x2, y2));
            var element = new ImageElement("searched", searched.GetBytes(ImageFormat.Png), Convert.ToDecimal(threshold, CultureInfo.InvariantCulture), grayscale);

            var result = await this._recognizer.Recognize(screenshot, element).ConfigureAwait(false);

            AssertResult(result, new Point(expectedX, expectedY));
        }

        [Fact]
        public async Task Recognize_WhenTwoMatchesInScreenshot_Works()
        {
            using var screenshot = await BitmapFromFile("./images/google-news.png");
            using var searched = screenshot.Crop(new Rectangle(1376, 390, 1420, 436));
            var element = new ImageElement("searched", searched.GetBytes(ImageFormat.Png), ImageElement.DefaultThreshold, false);

            var result = await this._recognizer.Recognize(screenshot, element).ConfigureAwait(false);

            AssertResult(result, new Point(1398, 413), new Point(1464, 413));
        }

        private static void AssertResult(SearchResult result, params Point[] expectedCenters)
        {
            Assert.True(result.Success);
            Assert.Equal(expectedCenters.Length, result.Areas.Count);

            for (var i = 0; i < expectedCenters.Length; i++)
            {
                Assert.Equal(expectedCenters[i], result.Areas[i].Center);
            }
        }

        private static async Task<Bitmap> BitmapFromFile(string path)
        {
            var bytes = await File.ReadAllBytesAsync(path);
            await using var stream = new MemoryStream(bytes);
            return (Bitmap)Image.FromStream(stream);
        }
    }
}
