using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Askaiser.Marionette.Imaging;

internal static class PixelOperations
{
    /// <summary>
    /// Sets the alpha channel of BGRA pixels to its maximum. Screen captures come with a meaningless alpha channel on most platforms.
    /// </summary>
    public static void MakeOpaque(Span<byte> bgra)
    {
        var pixels = MemoryMarshal.Cast<byte, uint>(bgra);
        var alpha = BitConverter.IsLittleEndian ? 0xFF000000u : 0x000000FFu;
        var i = 0;

        if (Vector.IsHardwareAccelerated)
        {
            var mask = new Vector<uint>(alpha);
            for (; i <= pixels.Length - Vector<uint>.Count; i += Vector<uint>.Count)
            {
                (new Vector<uint>(pixels.Slice(i)) | mask).CopyTo(pixels.Slice(i));
            }
        }

        for (; i < pixels.Length; i++)
        {
            pixels[i] |= alpha;
        }
    }
}
