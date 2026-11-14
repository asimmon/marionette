using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Recognition;

internal sealed class FailureFileWriter : IFailureFileWriter
{
    public async Task SaveAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(path, content, cancellationToken).ConfigureAwait(false);
    }
}
