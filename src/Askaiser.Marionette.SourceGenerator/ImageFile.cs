using System;

namespace Askaiser.Marionette.SourceGenerator;

/// <summary>
/// A file of an image library directory. Its size and modification time are a cheap way to notice that its content changed.
/// </summary>
/// <param name="FullPath">The path of the file.</param>
/// <param name="RelativePath">The path of the file relative to the image library directory, with forward slashes.</param>
/// <param name="Length">The size of the file in bytes.</param>
/// <param name="LastWriteTicks">The last modification time of the file.</param>
internal sealed record ImageFile(string FullPath, string RelativePath, long Length, long LastWriteTicks) : IEquatable<ImageFile>;
