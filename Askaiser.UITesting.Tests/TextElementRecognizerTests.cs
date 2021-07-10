using System;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.UITesting.Tests
{
    public sealed class TextElementRecognizerTests : BaseRecognizerTests, IDisposable
    {
        private readonly TextElementRecognizer _recognizer;

        public TextElementRecognizerTests()
        {
            this._recognizer = new TextElementRecognizer(new TestContextOptions());
        }

        [Theory]
        [InlineData(0, 0, 120, 30, "Google News", TextOptions.BlackAndWhite | TextOptions.Negative, 80, 18)]
        [InlineData(380, 170, 880, 336, "Headlines", TextOptions.None, 62, 17)]
        [InlineData(380, 170, 880, 336, "mperatures in southern califor", TextOptions.None, 172, 150)]
        public async Task Recognize_WhenSingleMatch_Works(int x1, int y1, int x2, int y2, string searched, TextOptions options, int expectedX, int expectedY)
        {
            using var screenshot = await BitmapFromFile("./images/google-news.png");
            using var cropped = screenshot.Crop(new Rectangle(x1, y1, x2, y2));
            var element = new TextElement(searched, options);

            var result = await this._recognizer.Recognize(cropped, element);

            AssertResult(result, new Point(expectedX, expectedY));
        }

        public void Dispose()
        {
            this._recognizer?.Dispose();
        }
    }
}
