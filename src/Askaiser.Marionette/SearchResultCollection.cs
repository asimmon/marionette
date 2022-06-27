using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Askaiser.Marionette;

public sealed class SearchResultCollection : IReadOnlyCollection<SearchResult>
{
    private readonly Dictionary<string, SearchResult> _results;

    internal SearchResultCollection(IEnumerable<SearchResult> results)
    {
        if (results == null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        this._results = new Dictionary<string, SearchResult>(StringComparer.OrdinalIgnoreCase);

        foreach (var result in results)
        {
            this._results.Add(result.Element.Name, result);
        }

        if (this._results.Count == 0)
        {
            throw new ArgumentException(Messages.SearchResultCollection_Throw_ResultsEmpty, nameof(results));
        }
    }

    public int Count
    {
        get => this._results.Count;
    }

    [SuppressMessage("Design", "CA1043:Use Integral Or String Argument For Indexers", Justification = "It's easier to access the search result by element")]
    public SearchResult this[IElement element]
    {
        get => element != null ? this[element] : throw new ArgumentNullException(nameof(element));
    }

    public SearchResult this[string name]
    {
        get => this._results.TryGetValue(name, out var result) ? result : throw new ArgumentException(Messages.Element_Throw_ElementNotFound.FormatInvariant(name), nameof(name));
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
