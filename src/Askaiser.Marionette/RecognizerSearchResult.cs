using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Askaiser.Marionette
{
    internal sealed class RecognizerSearchResult : SearchResult, IDisposable
    {
        public RecognizerSearchResult(Bitmap transformedScreenshot, IElement element, IEnumerable<Rectangle> locations)
            : base(element, locations)
        {
            this.TransformedScreenshot = transformedScreenshot;
        }

        internal RecognizerSearchResult(Bitmap transformedScreenshot, SearchResult searchResult)
            : this(transformedScreenshot, searchResult.Element, searchResult.Locations)
        {
        }

        public Bitmap TransformedScreenshot { get; }

        public static RecognizerSearchResult NotFound(Bitmap screenshot, IElement element)
        {
            return new RecognizerSearchResult(screenshot, element, Enumerable.Empty<Rectangle>());
        }

        public void Dispose()
        {
            this.TransformedScreenshot.Dispose();
        }
    }
}
