using System;
using System.Collections.Generic;
using System.IO;

namespace Askaiser.Marionette.SourceGenerator;

#pragma warning disable RS1035 // Do not use APIs banned for analyzers: images are found from a directory path, which requires reading the disk
internal sealed class PhysicalFileSystem : IFileSystem
{
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public IEnumerable<ImageFile> EnumerateFiles(string directoryPath)
    {
        var root = new DirectoryInfo(directoryPath);
        var prefixLength = root.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1;

        foreach (var file in root.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            var relativePath = file.FullName.Length > prefixLength ? file.FullName.Substring(prefixLength) : file.Name;
            yield return new ImageFile(file.FullName, relativePath.Replace('\\', '/'), file.Length, file.LastWriteTimeUtc.Ticks);
        }
    }

    public byte[] ReadAllBytes(string path)
    {
        // Image editors may still hold the file: allow sharing so that a build does not fail because of it.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var bytes = new byte[stream.Length];
        var offset = 0;

        while (offset < bytes.Length)
        {
            var read = stream.Read(bytes, offset, bytes.Length - offset);
            if (read == 0)
            {
                Array.Resize(ref bytes, offset);
                break;
            }

            offset += read;
        }

        return bytes;
    }
}
