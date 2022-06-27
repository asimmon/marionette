using System.IO;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

internal class RealFileWriter : IFileWriter
{
    public async Task SaveScreenshot(string path, byte[] screenshotBytes)
    {
#if NETSTANDARD2_0
            using (var fileStream = File.Open(path, FileMode.Create))
            {
                await fileStream.WriteAsync(screenshotBytes, 0, screenshotBytes.Length).ConfigureAwait(false);
            }
#else
        FileStream fileStream;
        await using ((fileStream = File.Open(path, FileMode.Create)).ConfigureAwait(false))
        {
            await fileStream.WriteAsync(screenshotBytes).ConfigureAwait(false);
        }
#endif
    }
}
