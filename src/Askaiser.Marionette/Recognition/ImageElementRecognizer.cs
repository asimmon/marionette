using System;
using System.Collections.Generic;
using System.Threading;
using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Recognition.Images;

namespace Askaiser.Marionette.Recognition;

internal sealed class ImageElementRecognizer : IElementRecognizer
{
    public bool CanRecognize(IElement element)
    {
        return element is ImageElement;
    }

    public IReadOnlyList<Rectangle> Recognize(SearchSurface surface, DisplayInfo display, IElement element, CancellationToken cancellationToken)
    {
        var imageElement = (ImageElement)element;
        var template = imageElement.GetTemplate()
            ?? throw new ArgumentException(Messages.ImageElement_Throw_NoFeatures.FormatInvariant(imageElement.Name), nameof(element));

        var matches = TemplateMatcher.Find(surface, template, statistics: null, cancellationToken);
        var locations = new Rectangle[matches.Count];

        for (var i = 0; i < locations.Length; i++)
        {
            var match = matches[i];
            locations[i] = new Rectangle(match.X, match.Y, match.X + template.Width, match.Y + template.Height);
        }

        return locations;
    }
}
