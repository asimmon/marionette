using System.Collections.Generic;
using System.Threading;
using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Recognition.Images;

namespace Askaiser.Marionette.Recognition;

/// <summary>
/// Finds the occurrences of an element in a part of a screenshot.
/// </summary>
internal interface IElementRecognizer
{
    bool CanRecognize(IElement element);

    /// <summary>
    /// Returns the bounds of every occurrence of the element, relative to the surface. This method is CPU-bound and is called from the thread pool.
    /// </summary>
    IReadOnlyList<Rectangle> Recognize(SearchSurface surface, DisplayInfo display, IElement element, CancellationToken cancellationToken);
}
