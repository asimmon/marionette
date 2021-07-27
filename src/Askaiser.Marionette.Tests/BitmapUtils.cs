using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Tests
{
    public static class BitmapUtils
    {
        private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

        public static async Task<Bitmap> FromAssembly(string resourceName)
        {
            using (var stream = ExecutingAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Assembly resource '{resourceName}' not found.");
                }

                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                return (Bitmap)Image.FromStream(ms);
            }
        }

        public static Bitmap FromBytes(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            return (Bitmap)Image.FromStream(ms);
        }
    }
}
