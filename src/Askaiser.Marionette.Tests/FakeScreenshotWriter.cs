using System.Collections.Generic;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Tests
{
    internal sealed class FakeScreenshotWriter : IFileWriter
    {
        private readonly List<FakeSavedFailure> _savedFailures;

        public FakeScreenshotWriter()
        {
            this._savedFailures = new List<FakeSavedFailure>();
        }

        public IReadOnlyList<FakeSavedFailure> SavedFailures
        {
            get => this._savedFailures;
        }

        public Task SaveScreenshot(string path, byte[] screenshotBytes)
        {
            using var img = BitmapUtils.FromBytes(screenshotBytes);
            this._savedFailures.Add(new FakeSavedFailure(img.Width, img.Height, path));
            return Task.CompletedTask;
        }
    }
}
