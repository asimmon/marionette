using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Askaiser.Marionette.Recognition.Images;

/// <summary>
/// A linear projection of BGR colors onto a single integer channel. Image search runs on one of these planes
/// before candidates are verified against the full-color image. The three projections are orthogonal, so the
/// energy of any color image splits exactly between them and at least one carries a third of it.
/// </summary>
internal enum ColorPlaneKind
{
    /// <summary>B + G + R, an unweighted luminance. Used for almost every image.</summary>
    Sum = 0,

    /// <summary>R - B. Used for images whose contrast is mostly between red and blue tones of equal brightness.</summary>
    RedMinusBlue = 1,

    /// <summary>B - 2G + R. Used for images whose contrast is mostly between green and magenta tones of equal brightness.</summary>
    GreenContrast = 2,
}

internal static class ColorPlane
{
    public const int KindCount = 3;

    /// <summary>
    /// Returns the squared norm of the projection vector of a plane.
    /// </summary>
    public static int SquaredNorm(ColorPlaneKind kind) => kind switch
    {
        ColorPlaneKind.Sum => 3,
        ColorPlaneKind.RedMinusBlue => 2,
        _ => 6,
    };

    /// <summary>
    /// Projects BGRA pixels on a color plane.
    /// </summary>
    public static void Extract(ReadOnlySpan<byte> bgra, Span<short> destination, ColorPlaneKind kind)
    {
        var pixels = MemoryMarshal.Cast<byte, uint>(bgra);
        var i = 0;

        if (kind == ColorPlaneKind.Sum && BitConverter.IsLittleEndian)
        {
            i = ExtractSumVectorized(pixels, destination);
        }

        for (; i < destination.Length; i++)
        {
            var p = pixels[i];
            int b = (byte)p, g = (byte)(p >> 8), r = (byte)(p >> 16);

            destination[i] = kind switch
            {
                ColorPlaneKind.Sum => (short)(b + g + r),
                ColorPlaneKind.RedMinusBlue => (short)(r - b),
                _ => (short)(b - (2 * g) + r),
            };
        }
    }

    private static int ExtractSumVectorized(ReadOnlySpan<uint> pixels, Span<short> destination)
    {
        var i = 0;

        if (Vector256.IsHardwareAccelerated)
        {
            var mask = Vector256.Create(0xFFu);
            for (; i <= destination.Length - 16; i += 16)
            {
                var lo = SumChannels(Vector256.Create(pixels.Slice(i)), mask);
                var hi = SumChannels(Vector256.Create(pixels.Slice(i + 8)), mask);
                Vector256.Narrow(lo, hi).AsInt16().CopyTo(destination.Slice(i));
            }
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            var mask = Vector128.Create(0xFFu);
            for (; i <= destination.Length - 8; i += 8)
            {
                var lo = SumChannels(Vector128.Create(pixels.Slice(i)), mask);
                var hi = SumChannels(Vector128.Create(pixels.Slice(i + 4)), mask);
                Vector128.Narrow(lo, hi).AsInt16().CopyTo(destination.Slice(i));
            }
        }

        return i;
    }

    private static Vector256<uint> SumChannels(Vector256<uint> p, Vector256<uint> mask)
    {
        return (p & mask) + (Vector256.ShiftRightLogical(p, 8) & mask) + (Vector256.ShiftRightLogical(p, 16) & mask);
    }

    private static Vector128<uint> SumChannels(Vector128<uint> p, Vector128<uint> mask)
    {
        return (p & mask) + (Vector128.ShiftRightLogical(p, 8) & mask) + (Vector128.ShiftRightLogical(p, 16) & mask);
    }
}
