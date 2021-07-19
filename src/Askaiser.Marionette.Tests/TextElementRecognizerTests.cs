using System;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public sealed class TextElementRecognizerTests : BaseRecognizerTests, IDisposable
    {
        private readonly TextElementRecognizer _recognizer;

        public TextElementRecognizerTests()
        {
            this._recognizer = new TextElementRecognizer(new DriverOptions());
        }

        [Theory]
        [InlineData(0, 0, 120, 30, "Google News", TextOptions.BlackAndWhite | TextOptions.Negative, 80, 18)]
        [InlineData(380, 170, 880, 336, "Headlines", TextOptions.None, 62, 17)]
        [InlineData(380, 170, 880, 336, "mperatures in southern califor", TextOptions.None, 172, 150)]
        [InlineData(20, 810, 160, 860, "English (United States)", TextOptions.None, 67, 36)]
        [InlineData(0, 0, 1920, 1080, "historic miami-dade courthouse closed due to ", TextOptions.None, 600, 665)]
        public async Task Recognize_WhenSingleMatch_Works(int x1, int y1, int x2, int y2, string searched, TextOptions options, int expectedX, int expectedY)
        {
            using var screenshot = await BitmapFromFile("./images/google-news.png");
            using var cropped = screenshot.Crop(new Rectangle(x1, y1, x2, y2));
            var element = new TextElement(searched, options);

            var result = await this._recognizer.Recognize(cropped, element);

            AssertResult(result, new Point(expectedX, expectedY));
        }

        [Fact]
        public async Task Recognize_WhenMultipleMatches_Works()
        {
            using var screenshot = await BitmapFromFile("./images/google-news.png");
            using var cropped = screenshot.Crop(new Rectangle(410, 300, 800, 550));
            var element = new TextElement("California") { IgnoreCase = false };

            var result = await this._recognizer.Recognize(cropped, element);

            AssertResult(result, new Point(255, 18), new Point(306, 132));
        }

        public void Dispose()
        {
            this._recognizer?.Dispose();
        }
    }
}
