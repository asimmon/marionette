using System;
using OpenCvSharp;

namespace Askaiser.Marionette;

internal static class MatExtensions
{
    public static Mat ToGrayscale(this Mat mat)
    {
        var channelCount = mat.Channels();

        return channelCount switch
        {
            1 => mat, // Already grayscaled (single channel)
            3 => mat.CvtColor(ColorConversionCodes.BGR2GRAY), // BGR (no alpha channel) to grayscale
            4 => mat.CvtColor(ColorConversionCodes.BGRA2GRAY), // BGRA (with alpha channel) to grayscale
            _ => throw new ArgumentException(Messages.MatExtensions_Throw_InvalidImageChannelCount.FormatInvariant(channelCount), nameof(mat)),
        };
    }

    public static Mat ToBGR(this Mat mat)
    {
        var channelCount = mat.Channels();

        return channelCount switch
        {
            1 => mat.CvtColor(ColorConversionCodes.GRAY2BGR), // Grayscale to BGR
            3 => mat, // Already BGR (no alpha channel)
            4 => mat.CvtColor(ColorConversionCodes.BGRA2BGR), // Remove alpha channel from BGRA
            _ => throw new ArgumentException(Messages.MatExtensions_Throw_InvalidImageChannelCount.FormatInvariant(channelCount), nameof(mat)),
        };
    }
}
