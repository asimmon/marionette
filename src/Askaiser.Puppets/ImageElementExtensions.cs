using System.Drawing;
using System.IO;

namespace Askaiser.Puppets
{
    internal static class ImageElementExtensions
    {
        public static Bitmap ToBitmap(this ImageElement element)
        {
            using var elementStream = new MemoryStream(element.Content);
            return (Bitmap)Image.FromStream(elementStream);
        }
    }
}
