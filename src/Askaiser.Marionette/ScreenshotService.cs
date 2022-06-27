using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

internal static class ScreenshotService
{
    public static Task<Bitmap> Take(Rectangle monitor)
    {
        return Task.Run(() => TakeInternal(monitor));
    }

    private static Bitmap TakeInternal(Rectangle monitor)
    {
        var bitmap = new Bitmap(monitor.Width, monitor.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(monitor.Left, monitor.Top, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }
}
