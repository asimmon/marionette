using System;
using System.Collections.Generic;
using System.Threading;
using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Recognition.Images;
using Askaiser.Marionette.Recognition.Text;

namespace Askaiser.Marionette.Recognition;

/// <summary>
/// Finds text. The text of a screenshot is read once and shared by every text element searched in it.
/// </summary>
internal sealed class TextElementRecognizer : IElementRecognizer, IDisposable
{
    private readonly TextRecognitionEngine _engine;

    public TextElementRecognizer(TextRecognitionModel model)
    {
        this._engine = new TextRecognitionEngine(model);
    }

    public bool CanRecognize(IElement element)
    {
        return element is TextElement;
    }

    public IReadOnlyList<Rectangle> Recognize(SearchSurface surface, DisplayInfo display, IElement element, CancellationToken cancellationToken)
    {
        var textElement = (TextElement)element;

        // On a high pixel density monitor text is proportionally larger: locating it on a reduced image is as accurate and much faster.
        var detectionScale = Math.Clamp(1f / display.ScaleFactor, 0.25f, 1f);

        var rows = surface.GetOrCreate(new TextKey(textElement.Options), current =>
        {
            using var transformed = TextPreprocessor.Apply(current.Pixels, textElement.Options);
            return this._engine.Read(transformed ?? current.Pixels, detectionScale);
        });

        cancellationToken.ThrowIfCancellationRequested();
        return TextFinder.Find(rows, textElement.Content, textElement.IgnoreCase);
    }

    public void Dispose()
    {
        this._engine.Dispose();
    }

    private sealed record TextKey(TextOptions Options);
}
