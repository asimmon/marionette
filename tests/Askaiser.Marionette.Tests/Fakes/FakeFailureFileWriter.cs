using Askaiser.Marionette.Recognition;

namespace Askaiser.Marionette.Tests.Fakes;

/// <summary>
/// Keeps the screenshots and the videos of failures in memory. A video is an animated PNG file, which decodes as its last image.
/// </summary>
internal sealed class FakeFailureFileWriter : IFailureFileWriter
{
    private readonly List<(string Path, Image Image, byte[] Content)> _saved = [];

    public IReadOnlyList<(string Path, Image Image, byte[] Content)> Saved
    {
        get
        {
            lock (this._saved)
            {
                return this._saved.ToArray();
            }
        }
    }

    public Task SaveAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        lock (this._saved)
        {
            this._saved.Add((path, Image.FromBytes(content.Span), content.ToArray()));
        }

        return Task.CompletedTask;
    }
}
