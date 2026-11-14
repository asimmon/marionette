using System;
using System.Collections;
using System.Collections.Generic;

namespace Askaiser.Marionette;

/// <summary>
/// A set of elements identified by their name. Names are not case-sensitive.
/// A collection can be saved to and loaded from a JSON document with <see cref="ElementCollectionExtensions"/>.
/// </summary>
public sealed class ElementCollection : IEnumerable<IElement>
{
    private readonly Dictionary<string, IElement> _elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElementCollection"/> class that is empty.
    /// </summary>
    public ElementCollection()
    {
        this._elements = new Dictionary<string, IElement>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    public int Count
    {
        get => this._elements.Count;
    }

    /// <summary>
    /// Gets the element with the specified name. The case of the name is ignored.
    /// </summary>
    /// <param name="name">The name of the element.</param>
    /// <exception cref="ArgumentException">The collection does not contain an element with this name.</exception>
    public IElement this[string name]
    {
        get => this.TryGetValue(name, out var element) && element != null ? element : throw new ArgumentException(Messages.Element_Throw_ElementNotFound.FormatInvariant(name), nameof(name));
    }

    /// <summary>
    /// Adds an element to the collection.
    /// </summary>
    /// <param name="element">The element to add.</param>
    /// <exception cref="ArgumentException">The collection already contains an element with the same name.</exception>
    public void Add(IElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        this._elements.Add(element.Name, element);
    }

    /// <summary>
    /// Removes the element that has the same name as the specified element. Nothing happens when there is none.
    /// </summary>
    /// <param name="element">The element to remove.</param>
    public void Remove(IElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        this._elements.Remove(element.Name);
    }

    /// <summary>
    /// Removes the element with the specified name. Nothing happens when there is none.
    /// </summary>
    /// <param name="name">The name of the element to remove.</param>
    public void Remove(string name)
    {
        this._elements.Remove(name);
    }

    /// <summary>
    /// Returns whether the collection contains an element that has the same name as the specified element.
    /// </summary>
    /// <param name="element">The element to look for.</param>
    public bool Contains(IElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return this.TryGetValue(element.Name, out _);
    }

    /// <summary>
    /// Returns whether the collection contains an element with the specified name.
    /// </summary>
    /// <param name="name">The name of the element to look for.</param>
    public bool Contains(string name)
    {
        return this.TryGetValue(name, out _);
    }

    /// <summary>
    /// Removes every element from the collection.
    /// </summary>
    public void Clear()
    {
        this._elements.Clear();
    }

    /// <summary>
    /// Gets the element with the specified name, when there is one. The case of the name is ignored.
    /// </summary>
    /// <param name="name">The name of the element.</param>
    /// <param name="value">The element with this name, or null when the collection does not contain one.</param>
    /// <returns>True when the collection contains an element with this name.</returns>
    public bool TryGetValue(string name, out IElement? value)
    {
        return this._elements.TryGetValue(name, out value);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the elements of the collection.
    /// </summary>
    public IEnumerator<IElement> GetEnumerator()
    {
        return this._elements.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
