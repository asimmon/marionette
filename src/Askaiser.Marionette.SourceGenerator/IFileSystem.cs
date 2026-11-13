using System.Collections.Generic;

namespace Askaiser.Marionette.SourceGenerator;

/// <summary>
/// The file system operations the generator needs. Abstracted so that the generator can be tested without touching the disk.
/// </summary>
internal interface IFileSystem
{
    bool DirectoryExists(string path);

    /// <summary>
    /// Lists the files of a directory and of its subdirectories, without reading them.
    /// </summary>
    IEnumerable<ImageFile> EnumerateFiles(string directoryPath);

    byte[] ReadAllBytes(string path);
}
