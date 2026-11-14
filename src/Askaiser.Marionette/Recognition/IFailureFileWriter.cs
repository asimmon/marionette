using System;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Recognition;

/// <summary>
/// Saves what was on the screen when an element could not be found, to help understand why: a screenshot or a video.
/// </summary>
internal interface IFailureFileWriter
{
    Task SaveAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);
}
