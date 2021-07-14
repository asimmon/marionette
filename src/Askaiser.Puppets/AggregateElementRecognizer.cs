﻿using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.Puppets
{
    internal sealed class AggregateElementRecognizer : IElementRecognizer
    {
        private readonly ImageElementRecognizer _imageElementRecognizer;
        private readonly TextElementRecognizer _textElementRecognizer;

        public AggregateElementRecognizer(ImageElementRecognizer imageElementRecognizer, TextElementRecognizer textElementRecognizer)
        {
            this._imageElementRecognizer = imageElementRecognizer;
            this._textElementRecognizer = textElementRecognizer;
        }

        public async Task<SearchResult> Recognize(Bitmap screenshot, IElement element) => element switch
        {
            null => throw new ArgumentNullException(nameof(element)),
            ImageElement imageElement => await this._imageElementRecognizer.Recognize(screenshot, imageElement).ConfigureAwait(false),
            TextElement textElement => await this._textElementRecognizer.Recognize(screenshot, textElement).ConfigureAwait(false),
            _ => throw new NotSupportedException(element.GetType().FullName)
        };
    }
}