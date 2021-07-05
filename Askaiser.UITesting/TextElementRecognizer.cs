using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.UITesting
{
    internal sealed class TextElementRecognizer : IElementRecognizer
    {
        public Task<SearchResult> Recognize(Bitmap screenshot, IElement element)
        {
            throw new NotImplementedException("Text recognition (OCR) is not yet implemented");
        }
    }
}