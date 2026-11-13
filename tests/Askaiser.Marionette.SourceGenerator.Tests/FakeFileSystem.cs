namespace Askaiser.Marionette.SourceGenerator.Tests;

/// <summary>
/// An in-memory file system that counts how often files are read.
/// </summary>
internal sealed class FakeFileSystem : IFileSystem
{
    private readonly Dictionary<string, (byte[] Content, long LastWriteTicks, long? Length)> _files = new(StringComparer.Ordinal);
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal);
    private int _readCount;

    public int ReadCount => this._readCount;

    /// <summary>Gets or sets a value indicating whether files are listed in reverse order, to verify that the generated code does not depend on it.</summary>
    public bool ListInReverseOrder { get; set; }

    public static string Normalize(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }

    public FakeFileSystem AddDirectory(string path)
    {
        this._directories.Add(Normalize(path));
        return this;
    }

    public FakeFileSystem AddFile(string path, byte[] content, long lastWriteTicks = 1, long? reportedLength = null)
    {
        path = Normalize(path);
        this._files[path] = (content, lastWriteTicks, reportedLength);

        for (var directory = Path.GetDirectoryName(path); !string.IsNullOrEmpty(directory); directory = Path.GetDirectoryName(directory))
        {
            this._directories.Add(Normalize(directory));
        }

        return this;
    }

    public bool DirectoryExists(string path)
    {
        return this._directories.Contains(Normalize(path));
    }

    public IEnumerable<ImageFile> EnumerateFiles(string directoryPath)
    {
        var prefix = Normalize(directoryPath) + "/";
        var files = this._files
            .Where(file => file.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(file => new ImageFile(file.Key, file.Key.Substring(prefix.Length), file.Value.Length ?? file.Value.Content.Length, file.Value.LastWriteTicks))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal);

        return this.ListInReverseOrder ? files.Reverse() : files;
    }

    public byte[] ReadAllBytes(string path)
    {
        Interlocked.Increment(ref this._readCount);
        return this._files[Normalize(path)].Content;
    }
}
