using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.UITesting
{
    internal interface IElementRecognizer
    {
        Task<SearchResult> Recognize(Bitmap screenshot, IElement element);
    }
}