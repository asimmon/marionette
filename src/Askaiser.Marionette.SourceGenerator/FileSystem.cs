using System.Collections.Generic;
using System.IO;

namespace Askaiser.Marionette.SourceGenerator
{
    internal sealed class FileSystem : IFileSystem
    {
        public IEnumerable<string> EnumerateFiles(string path)
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<string> EnumerateDirectories(string path)
        {
            return Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        public long GetFileSize(string path)
        {
            return new FileInfo(path).Length;
        }

        public byte[] GetFileBytes(string path)
        {
            return File.ReadAllBytes(path);
        }
    }
}
