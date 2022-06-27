using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Askaiser.Marionette;

internal sealed class ImageElementRecognizer : IElementRecognizer
{
    public async Task<RecognizerSearchResult> Recognize(Bitmap screenshot, IElement element, CancellationToken token)
    {
        RecognizerSearchResult RecognizeInternal()
        {
            var imageElement = (ImageElement)element;

            using var preprocessedScreenshotMat = screenshot.ToMat().ConvertAndDispose(x => NormalizeMatChannels(x, imageElement.Grayscale));

            if (token.IsCancellationRequested)
            {
                return RecognizerSearchResult.NotFound(preprocessedScreenshotMat.ToBitmap(), element);
            }

            using var elementTemplate = imageElement.ToBitmap()
                .ConvertAndDispose(x => x.ToMat())
                .ConvertAndDispose(x => NormalizeMatChannels(x, imageElement.Grayscale));

            if (token.IsCancellationRequested)
            {
                return RecognizerSearchResult.NotFound(preprocessedScreenshotMat.ToBitmap(), element);
            }

            // OpenCV template matching
            // https://stackoverflow.com/a/35346975/825695
            using var workingScreenshotMat = preprocessedScreenshotMat
                .MatchTemplate(elementTemplate, TemplateMatchModes.CCoeffNormed)
                .ConvertAndDispose(x => x.Threshold((double)imageElement.Threshold, 1d, ThresholdTypes.Tozero));

            if (token.IsCancellationRequested)
            {
                return RecognizerSearchResult.NotFound(preprocessedScreenshotMat.ToBitmap(), element);
            }

            var locations = new List<Rectangle>();

            var loDiff = new Scalar(0.1);
            var upDiff = new Scalar(1.0);

            while (!token.IsCancellationRequested)
            {
                workingScreenshotMat.MinMaxLoc(out _, out var maxval, out _, out var maxloc);

                var notFound = maxval < (double)imageElement.Threshold;
                if (notFound)
                {
                    return new RecognizerSearchResult(preprocessedScreenshotMat.ToBitmap(), element, locations);
                }

                locations.Add(new Rectangle(maxloc.X, maxloc.Y, maxloc.X + elementTemplate.Width, maxloc.Y + elementTemplate.Height));
                workingScreenshotMat.FloodFill(maxloc, new Scalar(0), out _, loDiff, upDiff);
            }

            return RecognizerSearchResult.NotFound(preprocessedScreenshotMat.ToBitmap(), element);
        }

        // ReSharper disable once MethodSupportsCancellation
        return await Task.Run(RecognizeInternal).ConfigureAwait(false);
    }

    private static Mat NormalizeMatChannels(Mat mat, bool isGrayscale)
    {
        return isGrayscale ? mat.ToGrayscale() : mat.ToBGR();
    }
}
