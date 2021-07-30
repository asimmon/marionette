using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public sealed class ImageElementRecognizerTests : BaseRecognizerTests
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
            using var screenshot = await BitmapUtils.FromAssembly("Askaiser.Marionette.Tests.images.google-news.png");
            using var searched = screenshot.Crop(new Rectangle(x1, y1, x2, y2));
            var element = new ImageElement("searched", searched.GetBytes(ImageFormat.Png), Convert.ToDecimal(threshold, CultureInfo.InvariantCulture), grayscale);

            using var result = await this._recognizer.Recognize(screenshot, element, CancellationToken.None).ConfigureAwait(false);

            AssertResult(result, new Point(expectedX, expectedY));
        }

        [Fact]
        public async Task Recognize_WhenTwoMatchesInScreenshot_Works()
        {
            using var screenshot = await BitmapUtils.FromAssembly("Askaiser.Marionette.Tests.images.google-news.png");
            using var searched = screenshot.Crop(new Rectangle(1376, 390, 1420, 436));
            var element = new ImageElement("searched", searched.GetBytes(ImageFormat.Png), ImageElement.DefaultThreshold, false);

            using var result = await this._recognizer.Recognize(screenshot, element, CancellationToken.None).ConfigureAwait(false);

            AssertResult(result, new Point(1398, 413), new Point(1464, 413));
        }
    }
}
