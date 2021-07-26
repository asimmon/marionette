using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Tests
{
    public static class BitmapUtils
    {
        public static async Task<Bitmap> FromFile(string path)
        {
#if NETFRAMEWORK
            await Task.Yield();
            var bytes = File.ReadAllBytes(path);
#else
            var bytes = await File.ReadAllBytesAsync(path);
#endif
            using var ms = new MemoryStream(bytes);
            return (Bitmap)Image.FromStream(ms);
        }
    }
}
