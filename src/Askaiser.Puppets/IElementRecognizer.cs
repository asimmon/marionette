using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.Puppets
{
    internal interface IElementRecognizer
    {
        Task<SearchResult> Recognize(Bitmap screenshot, IElement element);
    }
}
