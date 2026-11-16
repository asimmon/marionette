using System.IO;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Recognition;

internal sealed class FailureScreenshotWriter : IFailureScreenshotWriter
{
    public async Task SaveAsync(string path, byte[] pngBytes)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(path, pngBytes).ConfigureAwait(false);
    }
}
