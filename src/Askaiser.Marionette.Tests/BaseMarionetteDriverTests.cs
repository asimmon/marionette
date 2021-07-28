using System.Threading.Tasks;
using FakeItEasy;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public class BaseMarionetteDriverTests : IAsyncLifetime
    {
        public virtual async Task InitializeAsync()
        {
            using var screenshot = await BitmapUtils.FromAssembly("Askaiser.Marionette.Tests.images.google-news.png");
            this.MonitorService = new FakeMonitorService(screenshot);

            this.ElementRecognizer = new FakeElementRecognizer();
            this.FileWriter = new FakeScreenshotWriter();

            this.MouseController = A.Fake<IMouseController>();
            this.KeyboardController = A.Fake<IKeyboardController>();
        }

        internal FakeMonitorService MonitorService { get; private set; }

        internal FakeElementRecognizer ElementRecognizer { get; private set; }

        internal FakeScreenshotWriter FileWriter { get; private set; }

        internal IMouseController MouseController { get; private set; }

        internal IKeyboardController KeyboardController { get; private set; }

        protected MarionetteDriver CreateDriver(DriverOptions options = null)
        {
            options ??= new DriverOptions();

            return new MarionetteDriver(options, this.FileWriter, this.MonitorService, this.ElementRecognizer, this.MouseController, this.KeyboardController);
        }

        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        protected static void AssertSearchResult(SearchResult expected, SearchResult actual, Rectangle searchRect = null)
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
    }
}
