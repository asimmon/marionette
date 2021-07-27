using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    internal class FakeElementRecognizer : IElementRecognizer
    {
        private readonly object _lockObj;
        private readonly Dictionary<IElement, Queue<Func<SearchResult>>> _expectedResultsByElements;

        public FakeElementRecognizer()
        {
            this._lockObj = new object();
            this._expectedResultsByElements = new Dictionary<IElement, Queue<Func<SearchResult>>>();

            this.RecognizeCallCount = 0;
        }

        public int RecognizeCallCount { get; private set; }

        public SearchResult AddExpectedResult(IElement element, SearchResult result)
        {
            this.AddExpectedResult(element, () => result);
            return result;
        }

        public void AddExpectedResult(IElement element, Func<SearchResult> resultFunc)
        {
            var queue = this._expectedResultsByElements.TryGetValue(element, out var existingQueue)
                ? existingQueue
                : this._expectedResultsByElements[element] = new Queue<Func<SearchResult>>();

            queue.Enqueue(resultFunc);
        }

        public Task<RecognizerSearchResult> Recognize(Bitmap screenshot, IElement element)
        {
            this.RecognizeCallCount++;

            Assert.NotNull(screenshot);
            Assert.NotNull(element);

            if (!this._expectedResultsByElements.TryGetValue(element, out var resultQueue))
            {
                throw new InvalidOperationException($"Register a least one search result for element '{element}' in the test setup.");
            }

            Func<SearchResult> expectedResult;

            lock (this._lockObj)
            {
                expectedResult = resultQueue.Count == 1 ? resultQueue.Peek() : resultQueue.Dequeue();
            }

            var result = new RecognizerSearchResult(new Bitmap(screenshot), expectedResult());
            return Task.FromResult(result);
        }
    }
}
