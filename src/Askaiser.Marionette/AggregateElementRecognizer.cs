﻿using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

internal sealed class AggregateElementRecognizer : IElementRecognizer
{
    private readonly ImageElementRecognizer _imageElementRecognizer;
    private readonly TextElementRecognizer _textElementRecognizer;

    public AggregateElementRecognizer(ImageElementRecognizer imageElementRecognizer, TextElementRecognizer textElementRecognizer)
    {
        this._imageElementRecognizer = imageElementRecognizer;
        this._textElementRecognizer = textElementRecognizer;
    }

    public async Task<RecognizerSearchResult> Recognize(Bitmap screenshot, IElement element, CancellationToken token) => element switch
    {
        null => throw new ArgumentNullException(nameof(element)),
        ImageElement => await this._imageElementRecognizer.Recognize(screenshot, element, token).ConfigureAwait(false),
        TextElement => await this._textElementRecognizer.Recognize(screenshot, element, token).ConfigureAwait(false),
        _ => throw new NotSupportedException(element.GetType().FullName),
    };
}
