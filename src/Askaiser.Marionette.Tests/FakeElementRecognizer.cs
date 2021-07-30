using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    internal class FakeElementRecognizer : IElementRecognizer
    {
        private readonly Dictionary<IElement, Func<Task<SearchResult>>> _expectedResultsByElements;

        private int _recognizeCallCount;

        public FakeElementRecognizer()
        {
            this._expectedResultsByElements = new Dictionary<IElement, Func<Task<SearchResult>>>();
            this._recognizeCallCount = 0;
        }

        public int RecognizeCallCount
        {
            get => this._recognizeCallCount;
        }

        public SearchResult AddExpectedResult(IElement element, SearchResult result)
        {
            this.AddExpectedResult(element, () => Task.FromResult(result));
            return result;
        }

        public void AddExpectedResult(IElement element, Func<Task<SearchResult>> resultFunc)
        {
            this._expectedResultsByElements.Add(element, resultFunc);
        }

        public async Task<RecognizerSearchResult> Recognize(Bitmap screenshot, IElement element, CancellationToken token)
        {
            Interlocked.Increment(ref this._recognizeCallCount);

            Assert.NotNull(screenshot);
            Assert.NotNull(element);

            if (!this._expectedResultsByElements.TryGetValue(element, out var resultFunc))
            {
                throw new InvalidOperationException($"Register a least one search result for element '{element}' in the test setup.");
            }

            var innerResult = await resultFunc();
            return new RecognizerSearchResult(new Bitmap(screenshot), innerResult);
        }
    }
}
