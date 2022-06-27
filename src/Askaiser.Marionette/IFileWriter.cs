using System.Threading.Tasks;

namespace Askaiser.Marionette;

internal interface IFileWriter
{
    Task SaveScreenshot(string path, byte[] screenshotBytes);
}
