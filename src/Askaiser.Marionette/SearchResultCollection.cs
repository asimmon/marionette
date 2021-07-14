using System;
using System.Collections;
using System.Collections.Generic;

namespace Askaiser.Marionette
{
    public sealed class SearchResultCollection : IReadOnlyCollection<SearchResult>
    {
        private readonly Dictionary<string, SearchResult> _results;

        internal SearchResultCollection(IEnumerable<SearchResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            this._results = new Dictionary<string, SearchResult>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in results)
                this._results.Add(result.Element.Name, result);

            if (this._results.Count == 0)
                throw new ArgumentException("Results cannot be empty.", nameof(results));
        }

        public int Count
        {
            get => this._results.Count;
        }

        public SearchResult this[IElement element]
        {
            get => element != null ? this[element] : throw new ArgumentNullException(nameof(element));
        }

        public SearchResult this[string name]
        {
            get => this._results.TryGetValue(name, out var result) ? result : throw new ArgumentException($"Element {name} not found.", nameof(name));
        }

        public IEnumerator<SearchResult> GetEnumerator()
        {
            return this._results.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
