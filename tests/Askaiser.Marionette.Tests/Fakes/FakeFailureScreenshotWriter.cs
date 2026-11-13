using Askaiser.Marionette.Recognition;

namespace Askaiser.Marionette.Tests.Fakes;

internal sealed class FakeFailureScreenshotWriter : IFailureScreenshotWriter
{
    private readonly List<(string Path, Image Image)> _saved = [];

    public IReadOnlyList<(string Path, Image Image)> Saved => this._saved;

    public Task SaveAsync(string path, byte[] pngBytes)
    {
        lock (this._saved)
        {
            this._saved.Add((path, Image.FromBytes(pngBytes)));
        }

        return Task.CompletedTask;
    }
}
