using System.Collections.Generic;

namespace Askaiser.Marionette.SourceGenerator;

internal interface IFileSystem
{
    IEnumerable<string> EnumerateFiles(string path);

    IEnumerable<string> EnumerateDirectories(string path);

    long GetFileSize(string path);

    byte[] GetFileBytes(string path);
}
