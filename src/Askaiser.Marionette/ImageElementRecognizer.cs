﻿using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Askaiser.Marionette
{
    internal sealed class ImageElementRecognizer : IElementRecognizer
    {
        public async Task<RecognizerSearchResult> Recognize(Bitmap screenshot, IElement element)
        {
            RecognizerSearchResult RecognizeInternal()
            {
                var imageElement = (ImageElement)element;

                // https://stackoverflow.com/a/35346975/825695
                using var elementTemplate = imageElement.ToBitmap()
                    .ConvertAndDispose(x => x.ToMat())
                    .ConvertAndDispose(x => imageElement.Grayscale ? x.CvtColor(ColorConversionCodes.BGRA2GRAY) : x);

                using var preprocessedScreenshotMat = screenshot.ToMat().ConvertAndDispose(x => imageElement.Grayscale ? x.CvtColor(ColorConversionCodes.BGRA2GRAY) : x);
                using var workingScreenshotMat = preprocessedScreenshotMat
                    .MatchTemplate(elementTemplate, TemplateMatchModes.CCoeffNormed)
                    .ConvertAndDispose(x => x.Threshold((double)imageElement.Threshold, 1d, ThresholdTypes.Tozero));

                var locations = new List<Rectangle>();

                var loDiff = new Scalar(0.1);
                var upDiff = new Scalar(1.0);

                while (true)
                {
                    workingScreenshotMat.MinMaxLoc(out _, out var maxval, out _, out var maxloc);

                    var notFound = maxval < (double)imageElement.Threshold;
                    if (notFound)
                    {
                        var transformedScreenshot = preprocessedScreenshotMat.ToBitmap();
                        return new RecognizerSearchResult(transformedScreenshot, element, locations);
                    }

                    locations.Add(new Rectangle(maxloc.X, maxloc.Y, maxloc.X + elementTemplate.Width, maxloc.Y + elementTemplate.Height));
                    workingScreenshotMat.FloodFill(maxloc, new Scalar(0), out _, loDiff, upDiff);
                }
            }

            return await Task.Run(RecognizeInternal).ConfigureAwait(false);
        }
    }
}
