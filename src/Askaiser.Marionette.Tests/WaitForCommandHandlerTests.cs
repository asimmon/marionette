using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Askaiser.Marionette.Commands;
using FakeItEasy;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public sealed class WaitForCommandHandlerTests : BaseWaitForCommandHandlerTests, IAsyncLifetime, IDisposable
    {
        private Bitmap _screenshot;
        private FakeMonitorService _monitorService;
        private IElementRecognizer _elementRecognizer;
        private IElement _searchedElement;
        private IElement[] _searchedElements;
        private List<FailureScreenshot> _failures;
        private IFileWriter _fileWriter;

        public async Task InitializeAsync()
        {
            this._screenshot = await BitmapUtils.FromFile("./images/google-news.png");
            this._monitorService = new FakeMonitorService(this._screenshot);
            this._elementRecognizer = A.Fake<IElementRecognizer>();
            this._searchedElement = new FakeElement("Foo.Bar.0");
            this._searchedElements = new[] { this._searchedElement };
            this._failures = new List<FailureScreenshot>();
            this._fileWriter = A.Fake<IFileWriter>();
            A.CallTo(() => this._fileWriter.SaveScreenshot(A<string>._, A<byte[]>._))
                .Invokes(x =>
                {
                    var path = x.GetArgument<string>(0);
                    var fileBytes = x.GetArgument<byte[]>(1);

                    Assert.NotNull(path);
                    Assert.NotNull(fileBytes);

                    using var ms = new MemoryStream(fileBytes);
                    using var img = Image.FromStream(ms);

                    this._failures.Add(new FailureScreenshot(img.Width, img.Height, path));
                })
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task Execute_WhenNegativeTimeout_Throws()
        {
            var opts = new DriverOptions();
            var handler = new WaitForCommandHandler(opts, this._fileWriter, this._monitorService, this._elementRecognizer);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            {
                return handler.Execute(new WaitForCommand(this._searchedElements, TimeSpan.FromSeconds(-1), null, 0, NoSingleResultBehavior.Throw));
            });
        }

        [Fact]
        public async Task Execute_WhenSingleLocation_Works()
        {
            var opts = new DriverOptions();
            var handler = new WaitForCommandHandler(opts, this._fileWriter, this._monitorService, this._elementRecognizer);

            var expectedResult = new SearchResult(this._searchedElement, new[] { new Rectangle(10, 40, 20, 50) });
            this.RegisterRecognizeResult(this._searchedElement, expectedResult);

            var actualResult = await handler.Execute(new WaitForCommand(this._searchedElements, TimeSpan.Zero, null, 0, NoSingleResultBehavior.Throw));

            AssertSearchResult(expectedResult, actualResult, null);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Execute_WhenNoLocation_Throws(bool useFailureScreenshotPath)
        {
            var opts = useFailureScreenshotPath ? new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" } : new DriverOptions();

            var handler = new WaitForCommandHandler(opts, this._fileWriter, this._monitorService, this._elementRecognizer);

            var expectedResult = new SearchResult(this._searchedElement, Array.Empty<Rectangle>());
            this.RegisterRecognizeResult(this._searchedElement, expectedResult);

            var ex = await Assert.ThrowsAsync<ElementNotFoundException>(() =>
            {
                return handler.Execute(new WaitForCommand(this._searchedElements, TimeSpan.Zero, null, 0, NoSingleResultBehavior.Throw));
            });

            Assert.Equal(this._searchedElement, ex.Element);

            if (useFailureScreenshotPath)
            {
                var failure = Assert.Single(this._failures);

                Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
                Assert.EndsWith("_foobar0.png", failure.Path);

                Assert.Equal(this._screenshot.Width, failure.Width);
                Assert.Equal(this._screenshot.Height, failure.Height);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Execute_WhenTooManyLocations_Throws(bool useFailureScreenshotPath)
        {
            var opts = useFailureScreenshotPath ? new DriverOptions { FailureScreenshotPath = @"C:\foo\bar" } : new DriverOptions();

            var handler = new WaitForCommandHandler(opts, this._fileWriter, this._monitorService, this._elementRecognizer);

            var loc1 = new Rectangle(10, 40, 20, 50);
            var loc2 = new Rectangle(100, 400, 200, 500);
            var expectedResult = new RecognizerSearchResult(new Bitmap(this._screenshot), this._searchedElement, new[] { loc1, loc2 });
            this.RegisterRecognizeResult(this._searchedElement, expectedResult);

            var ex = await Assert.ThrowsAsync<MultipleElementFoundException>(() =>
            {
                return handler.Execute(new WaitForCommand(this._searchedElements, TimeSpan.Zero, null, 0, NoSingleResultBehavior.Throw));
            });

            AssertSearchResult(expectedResult, ex.Result, null);

            if (useFailureScreenshotPath)
            {
                var failure = Assert.Single(this._failures);

                Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
                Assert.EndsWith("_foobar0.png", failure.Path);

                Assert.Equal(this._screenshot.Width, failure.Width);
                Assert.Equal(this._screenshot.Height, failure.Height);
            }
        }

        [Fact]
        public async Task Execute_WhenNoLocationWithSearchRect_Throws()
        {
            var opts = new DriverOptions
            {
                FailureScreenshotPath = @"C:\foo\bar",
            };

            var handler = new WaitForCommandHandler(opts, this._fileWriter, this._monitorService, this._elementRecognizer);

            var expectedResult = new RecognizerSearchResult(new Bitmap(this._screenshot), this._searchedElement, Array.Empty<Rectangle>());
            this.RegisterRecognizeResult(this._searchedElement, expectedResult);

            var searchRect = new Rectangle(10, 20, 210, 320);

            var ex = await Assert.ThrowsAsync<ElementNotFoundException>(() =>
            {
                return handler.Execute(new WaitForCommand(this._searchedElements, TimeSpan.Zero, searchRect, 0, NoSingleResultBehavior.Throw));
            });

            Assert.Equal(this._searchedElement, ex.Element);

            var failure = Assert.Single(this._failures);

            Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
            Assert.EndsWith("_foobar0.png", failure.Path);

            Assert.Equal(searchRect.Width, failure.Width);
            Assert.Equal(searchRect.Height, failure.Height);
        }

        [Fact]
        public async Task Execute_WhenTooManyLocationsWithSearchRect_Throws()
        {
            var opts = new DriverOptions
            {
                FailureScreenshotPath = @"C:\foo\bar",
            };

            var handler = new WaitForCommandHandler(opts, this._fileWriter, this._monitorService, this._elementRecognizer);

            var loc1 = new Rectangle(10, 40, 20, 50);
            var loc2 = new Rectangle(100, 400, 200, 500);
            var expectedResult = new SearchResult(this._searchedElement, new[] { loc1, loc2 });
            this.RegisterRecognizeResult(this._searchedElement, expectedResult);

            var searchRect = new Rectangle(5, 7, 450, 630);

            var ex = await Assert.ThrowsAsync<MultipleElementFoundException>(() =>
            {
                return handler.Execute(new WaitForCommand(this._searchedElements, TimeSpan.Zero, searchRect, 0, NoSingleResultBehavior.Throw));
            });

            AssertSearchResult(expectedResult, ex.Result, searchRect);

            var failure = Assert.Single(this._failures);

            Assert.StartsWith(opts.FailureScreenshotPath, failure.Path);
            Assert.EndsWith("_foobar0.png", failure.Path);

            Assert.Equal(searchRect.Width, failure.Width);
            Assert.Equal(searchRect.Height, failure.Height);
        }

        private void RegisterRecognizeResult(IElement element, SearchResult expectedResult)
        {
            A.CallTo(() => this._elementRecognizer.Recognize(A<Bitmap>._, element)).ReturnsLazily(x =>
            {
                var screenshot = x.GetArgument<Bitmap>(0);
                Assert.NotNull(screenshot);
                var result = new RecognizerSearchResult(new Bitmap(screenshot), expectedResult);
                return Task.FromResult(result);
            });
        }

        private static void AssertSearchResult(SearchResult expected, SearchResult actual, Rectangle searchRect)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);

            expected = expected.AdjustToSearchRectangle(searchRect);

            Assert.Equal(expected.Success, actual.Success);
            Assert.Equal(expected.Element, actual.Element);

            Assert.Equal(expected.Locations.Count, actual.Locations.Count);

            for (var i = 0; i < expected.Locations.Count; i++)
            {
                Assert.Equal(expected.Locations[i], actual.Locations[i]);
            }
        }

        private class FailureScreenshot
        {
            public FailureScreenshot(int width, int height, string path)
            {
                this.Width = width;
                this.Height = height;
                this.Path = path;
            }

            public int Width { get; }

            public int Height { get; }

            public string Path { get; }
        }

        public Task DisposeAsync()
        {
            this.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            this._monitorService?.Dispose();
        }
    }
}
