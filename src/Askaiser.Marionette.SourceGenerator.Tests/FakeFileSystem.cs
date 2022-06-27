using System;
using System.Collections.Generic;
using System.Linq;

namespace Askaiser.Marionette.SourceGenerator.Tests;

public class FakeFileSystem : IFileSystem
{
    private const char ForwardSlash = '/';
    private const char BackSlash = '\\';
    private static readonly char[] Slashes = { ForwardSlash, BackSlash };

    private readonly Dictionary<string, HashSet<string>> _childDirectories;
    private readonly Dictionary<string, HashSet<string>> _childFiles;
    private readonly Dictionary<string, FakeFileInfo> _fileInfos;

    public FakeFileSystem()
    {
        this._childDirectories = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        this._childFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        this._fileInfos = new Dictionary<string, FakeFileInfo>(StringComparer.OrdinalIgnoreCase);
    }

    public FakeFileSystem(params string[] entries)
    {
        this.AddEntries(entries);
    }

    public void AddEntries(params string[] entries)
    {
        foreach (var entry in entries)
        {
            this.AddEntry(entry);
        }
    }

    public void AddEntry(string entry)
    {
        if (entry == null)
        {
            return;
        }

        entry = entry.Trim();
        if (entry.Length == 0)
        {
            return;
        }

        var pathParts = entry.Split(Slashes, StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length == 0)
        {
            return;
        }

        for (var i = 1; i <= pathParts.Length; i++)
        {
            var left = pathParts[..^i];
            var right = pathParts[..^(i - 1)];

            if (left.Length == 0)
            {
                continue;
            }

            var dirPath = string.Join(BackSlash, left);
            var itemPath = string.Join(BackSlash, right);

            var isFile = right[^1].Contains('.');
            if (isFile)
            {
                var subItems = this._childFiles.TryGetValue(dirPath, out var x) ? x : this._childFiles[dirPath] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                subItems.Add(itemPath);
                this._fileInfos[itemPath] = new FakeFileInfo(itemPath);
            }
            else
            {
                var subItems = this._childDirectories.TryGetValue(dirPath, out var x) ? x : this._childDirectories[dirPath] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                subItems.Add(itemPath);
            }
        }
    }

    public IEnumerable<string> EnumerateFiles(string path)
    {
        path = path.Replace(ForwardSlash, BackSlash).TrimEnd(BackSlash);
        return this._childFiles.TryGetValue(path, out var files) ? files : Enumerable.Empty<string>();
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        path = path.Replace(ForwardSlash, BackSlash).TrimEnd(BackSlash);
        return this._childDirectories.TryGetValue(path, out var directories) ? directories : Enumerable.Empty<string>();
    }

    public long GetFileSize(string path)
    {
        return this.GetFileBytes(path).Length;
    }

    public byte[] GetFileBytes(string path)
    {
        return this.TryGetFile(path, out var file) ? file.Bytes : Array.Empty<byte>();
    }

    public void SetFileBytes(string path, byte[] bytes)
    {
        this.AddEntry(path);

        if (this.TryGetFile(path, out var file))
        {
            file.Bytes = bytes;
        }
    }

    private bool TryGetFile(string path, out FakeFileInfo file)
    {
        file = null;

        if (path == null)
        {
            return false;
        }

        if (!path.Contains('.'))
        {
            return false;
        }

        path = path.Replace(ForwardSlash, BackSlash).TrimEnd(BackSlash);
        return this._fileInfos.TryGetValue(path, out file);
    }

    private sealed class FakeFileInfo
    {
        private byte[] _bytes;

        public FakeFileInfo(string path)
        {
            this.Path = path ?? throw new ArgumentNullException(nameof(path));
            this.Bytes = Array.Empty<byte>();
        }

        public string Path { get; }

        public byte[] Bytes
        {
            get => this._bytes;
            set => this._bytes = value ?? throw new ArgumentNullException(nameof(value));
        }

        private bool Equals(FakeFileInfo other)
        {
            return this.Path == other.Path;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is FakeFileInfo other && this.Equals(other));
        }

        public override int GetHashCode()
        {
            return this.Path.GetHashCode();
        }
    }
}
