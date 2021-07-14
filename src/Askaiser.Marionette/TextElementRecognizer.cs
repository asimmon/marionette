using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;

namespace Askaiser.Marionette
{
    internal sealed class TextElementRecognizer : IElementRecognizer, IDisposable
    {
        private const int UpscalingRatio = 2;

        private readonly Guid _defaultEngineId;
        private readonly ConcurrentDictionary<Guid, TesseractEngine> _engines;
        private readonly TestContextOptions _options;
        private int _activeEngineCount;

        public TextElementRecognizer(TestContextOptions options)
        {
            this._defaultEngineId = Guid.NewGuid();
            this._engines = new ConcurrentDictionary<Guid, TesseractEngine>();
            this._options = options;
            this._activeEngineCount = 0;
        }

        public async Task<SearchResult> Recognize(Bitmap screenshot, IElement element)
        {
            SearchResult RecognizeInternal()
            {
                var textElement = (TextElement)element;

                using var img = screenshot.ToMat()
                    .ConvertAndDispose(Upscale)
                    .ConvertAndDispose(GetConverters(textElement.Options))
                    .ConvertAndDispose(BitmapConverter.ToBitmap)
                    .ConvertAndDispose(PixConverter.ToPix);

                Guid engineId = default;

                try
                {
                    var newActiveEngineCount = Interlocked.Increment(ref this._activeEngineCount);
                    engineId = newActiveEngineCount > 1 ? Guid.NewGuid() : this._defaultEngineId;
                    var engine = this._engines.GetOrAdd(engineId, _ => this.CreateEngine());

                    using var page = engine.Process(img);
                    using var iterator = page.GetIterator();
                    var areas = TesseractResultHandler.Handle(iterator, textElement);

                    return new SearchResult(element, areas.Select(Downscale));
                }
                finally
                {
                    Interlocked.Decrement(ref this._activeEngineCount);
                    if (engineId != this._defaultEngineId && this._engines.TryRemove(engineId, out var engine))
                        engine.Dispose();
                }
            }

            return await Task.Run(RecognizeInternal).ConfigureAwait(false);
        }

        private TesseractEngine CreateEngine()
        {
            return new TesseractEngine(this._options.TesseractDataPath, this._options.TesseractLanguage, EngineMode.LstmOnly);
        }

        private static IEnumerable<Func<Mat, Mat>> GetConverters(TextOptions options)
        {
            if (options == TextOptions.None)
                yield break;

            if (options.HasFlag(TextOptions.Grayscale))
                yield return Grayscale;

            if (options.HasFlag(TextOptions.BlackAndWhite))
                yield return Binarize;

            if (options.HasFlag(TextOptions.Negative))
                yield return Negate;
        }

        private static Mat Upscale(Mat mat)
        {
            return mat.Resize(Multiply(mat.Size(), UpscalingRatio), 0, 0, InterpolationFlags.Nearest);
        }

        private static Mat Grayscale(Mat mat)
        {
            return mat.CvtColor(ColorConversionCodes.BGRA2GRAY);
        }

        private static Mat Binarize(Mat mat)
        {
            const float unusedThresholdOverridenByOtsuAlgorithm = 128;
            return mat.Threshold(unusedThresholdOverridenByOtsuAlgorithm, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        }

        private static Mat Negate(Mat mat)
        {
            return mat.OnesComplement().ConvertAndDispose(x => x.ToMat());
        }

        private static OpenCvSharp.Size Multiply(OpenCvSharp.Size size, int factor)
        {
            return new OpenCvSharp.Size(size.Width * factor, size.Height * factor);
        }

        private static Rectangle MergeRectangles(Rectangle r1, Rectangle r2)
        {
            var minLeft = Math.Min(r1.Left, r2.Left);
            var minTop = Math.Min(r1.Top, r2.Top);
            var maxRight = Math.Max(r1.Right, r2.Right);
            var maxBottom = Math.Max(r1.Bottom, r2.Bottom);

            return new Rectangle(minLeft, minTop, maxRight, maxBottom);
        }

        private static Rectangle Downscale(Rectangle r)
        {
            return r.Multiply(1f / UpscalingRatio);
        }

        private sealed class TesseractResultHandler
        {
            private readonly ResultIterator _iterator;
            private readonly string _searchedText;
            private readonly List<Rectangle> _confirmedResults;
            private readonly Func<char, char, bool> _charEquals;
            private Rectangle _currentResult;
            private int _characterIndex;

            private TesseractResultHandler(ResultIterator iterator, TextElement element)
            {
                this._iterator = iterator;
                this._searchedText = string.Join(' ', element.Content.Split().TrimAndRemoveEmptyEntries());
                this._charEquals = element.IgnoreCase ? AreEqualOrdinalIgnoreCase : AreEqualOrdinal;
                this._confirmedResults = new List<Rectangle>();
                this._currentResult = null;
                this._characterIndex = 0;
            }

            public static IEnumerable<Rectangle> Handle(ResultIterator iterator, TextElement element)
            {
                return new TesseractResultHandler(iterator, element).Handle();
            }

            private IEnumerable<Rectangle> Handle()
            {
                do
                    do
                        this.HandleIteration();
                    while (this._iterator.Next(PageIteratorLevel.Word, PageIteratorLevel.Symbol));
                while (this._iterator.Next(PageIteratorLevel.Word));

                return this._confirmedResults;
            }

            private void HandleIteration()
            {
                this.HandleBeginningOfNewWord();

                if (this.TryGetNextSymbolRectangle(out var symbolRectangle))
                    this.AppendSymbolRectangleToCurrentResult(symbolRectangle);
                else if (this._characterIndex > 0)
                    this.ResetCurrentResult();
            }

            private void HandleBeginningOfNewWord()
            {
                var isAtBeginningOfAnyButFirstWord = this._iterator.IsAtBeginningOf(PageIteratorLevel.Word) && this._characterIndex > 0;
                if (isAtBeginningOfAnyButFirstWord)
                {
                    var isAlsoAtBeginningOfWordInSearchText = this._searchedText[this._characterIndex++] == ' ';
                    if (!isAlsoAtBeginningOfWordInSearchText)
                        this.ResetCurrentResult();
                }
            }

            private void ResetCurrentResult()
            {
                this._characterIndex = 0;
                this._currentResult = null;
            }

            private bool TryGetNextSymbolRectangle(out Rectangle symbolRectangle)
            {
                symbolRectangle = default;

                if (!this._iterator.IsAtBeginningOf(PageIteratorLevel.Symbol))
                    return false;

                var symbol = this._iterator.GetText(PageIteratorLevel.Symbol);
                if (symbol is { Length: 0 })
                    return false;

                for (var i = 0; i < symbol.Length && i < this._searchedText.Length; i++)
                {
                    var character = symbol[i];
                    if (!this._charEquals(character, this._searchedText[this._characterIndex++]))
                        return false;
                }

                if (!this._iterator.TryGetBoundingBox(PageIteratorLevel.Symbol, out var symbolBounds))
                    return false;

                symbolRectangle = new Rectangle(symbolBounds.X1, symbolBounds.Y1, symbolBounds.X2, symbolBounds.Y2);
                return true;
            }

            private void AppendSymbolRectangleToCurrentResult(Rectangle symbolRectangle)
            {
                this._currentResult = this._currentResult == null ? symbolRectangle : MergeRectangles(this._currentResult, symbolRectangle);

                var hasReachedEndOfSearchedText = this._characterIndex >= this._searchedText.Length;
                if (hasReachedEndOfSearchedText)
                {
                    this._confirmedResults.Add(this._currentResult);
                    this.ResetCurrentResult();
                }
            }

            private static bool AreEqualOrdinalIgnoreCase(char x, char y) => char.ToUpperInvariant(x) == char.ToUpperInvariant(y);
            private static bool AreEqualOrdinal(char x, char y) => x == y;
        }

        public void Dispose()
        {
            foreach (var engine in this._engines.Values)
                engine.Dispose();
        }
    }
}
