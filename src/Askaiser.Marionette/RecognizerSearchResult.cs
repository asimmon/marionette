using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Askaiser.Marionette
{
    internal sealed class RecognizerSearchResult : SearchResult, IDisposable
    {
        public RecognizerSearchResult(Bitmap transformedScreenshot, IElement element, IEnumerable<Rectangle> areas)
            : base(element, areas)
        {
            this.TransformedScreenshot = transformedScreenshot;
        }

        public Bitmap TransformedScreenshot { get; }

        public static SearchResult NotFound(Bitmap screenshot, IElement element)
        {
            return new RecognizerSearchResult(screenshot, element, Enumerable.Empty<Rectangle>());
        }

        public void Dispose()
        {
            this.TransformedScreenshot.Dispose();
        }
    }
}
