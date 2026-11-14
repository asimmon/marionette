using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Askaiser.Marionette;

/// <summary>
/// The outcome of the search of several elements at once: one <see cref="SearchResult"/> per element, identified by the name of the element.
/// Names are not case-sensitive.
/// </summary>
public sealed class SearchResultCollection : IReadOnlyCollection<SearchResult>
{
    private readonly Dictionary<string, SearchResult> _results;

    internal SearchResultCollection(IEnumerable<SearchResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

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

    /// <summary>
    /// Gets the number of results, which is the number of elements that were searched.
    /// </summary>
    public int Count
    {
        get => this._results.Count;
    }

    /// <summary>
    /// Gets the result of the search of an element, identified by its name.
    /// </summary>
    /// <param name="element">The element that was searched.</param>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
    /// <exception cref="ArgumentException">No element with this name was searched.</exception>
    [SuppressMessage("Design", "CA1043:Use Integral Or String Argument For Indexers", Justification = "It's easier to access the search result by element")]
    public SearchResult this[IElement element]
    {
        get => element != null ? this[element.Name] : throw new ArgumentNullException(nameof(element));
    }

    /// <summary>
    /// Gets the result of the search of the element with the specified name. The case of the name is ignored.
    /// </summary>
    /// <param name="name">The name of the element that was searched.</param>
    /// <exception cref="ArgumentException">No element with this name was searched.</exception>
    public SearchResult this[string name]
    {
        get => this._results.TryGetValue(name, out var result) ? result : throw new ArgumentException(Messages.Element_Throw_ElementNotFound.FormatInvariant(name), nameof(name));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the results.
    /// </summary>
    public IEnumerator<SearchResult> GetEnumerator()
    {
        return this._results.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
