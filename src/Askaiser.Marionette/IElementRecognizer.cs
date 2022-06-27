using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

internal interface IElementRecognizer
{
    Task<RecognizerSearchResult> Recognize(Bitmap screenshot, IElement element, CancellationToken token);
}
