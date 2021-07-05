using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Askaiser.UITesting
{
    public sealed class SearchResultCollection : IReadOnlyCollection<SearchResult>
    {
        private readonly List<SearchResult> _results;

        internal SearchResultCollection(IEnumerable<SearchResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            this._results = new List<SearchResult>(results);
            if (this._results.Count == 0)
                throw new ArgumentException("Results cannot be empty.", nameof(results));
        }

        public int Count
        {
            get => this._results.Count;
        }

        public SearchResult this[IElement element]
        {
            get => this._results.First(x => x.Element.Equals(element));
        }

        public SearchResult this[string elementName]
        {
            get => this._results.First(x => x.Element.Name.Equals(elementName, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerator<SearchResult> GetEnumerator()
        {
            return this._results.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}