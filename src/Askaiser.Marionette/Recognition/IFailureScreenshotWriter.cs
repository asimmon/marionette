using System.Threading.Tasks;

namespace Askaiser.Marionette.Recognition;

/// <summary>
/// Saves what was on the screen when an element could not be found, to help understand why.
/// </summary>
internal interface IFailureScreenshotWriter
{
    Task SaveAsync(string path, byte[] pngBytes);
}
