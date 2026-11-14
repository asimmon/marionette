using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Askaiser.Marionette;

/// <summary>
/// The outcome of the search of an element: every location where it is visible on the screen.
/// Enumerating a result enumerates its locations.
/// </summary>
public class SearchResult : IEnumerable<Rectangle>
{
    internal SearchResult(IElement element, IEnumerable<Rectangle> locations)
    {
        this.Element = element;
        this.Locations = new List<Rectangle>(locations);
        this.Success = this.Locations.Count > 0;
    }

    internal SearchResult(SearchResult other)
        : this(other.Element, other.Locations)
    {
    }

    /// <summary>
    /// Gets the element that was searched.
    /// </summary>
    public IElement Element { get; }

    /// <summary>
    /// Gets every location of the element, in pixels, in the coordinates of the virtual desktop that spans all monitors.
    /// The list is empty when the element was not found.
    /// </summary>
    public IReadOnlyList<Rectangle> Locations { get; }

    /// <summary>
    /// Gets a value indicating whether the element was found at least once.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the location of the element when it was found exactly once, in pixels, in the coordinates of the virtual desktop that spans all monitors.
    /// </summary>
    /// <exception cref="ElementNotFoundException">The element was not found.</exception>
    /// <exception cref="MultipleElementFoundException">The element was found at several locations. Use <see cref="Locations"/> instead.</exception>
    public Rectangle Location
    {
        get
        {
            this.EnsureSingleLocation(TimeSpan.Zero);
            return this.Locations[0];
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the locations of the element.
    /// </summary>
    public IEnumerator<Rectangle> GetEnumerator()
    {
        return this.Locations.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    internal SearchResult AdjustToMonitor(MonitorDescription monitor)
    {
        var newLocations = this.Locations.Select(x => x + (monitor.Left, monitor.Top));
        return new SearchResult(this.Element, newLocations);
    }

    internal SearchResult AdjustToSearchRectangle(Rectangle? rect)
    {
        if (rect == null)
        {
            return this;
        }

        var newLocations = this.Locations.Select(x => x + (rect.Left, rect.Top));
        return new SearchResult(this.Element, newLocations);
    }

    /// <summary>Returns the element and the center of each of its locations, or <c>none</c> when it was not found.</summary>
    public override string ToString()
    {
        return Messages.SearchResult_ToString.FormatInvariant(this.Element, this.Locations.Count > 0 ? this.Locations.ToCenterString() : "none");
    }

    /// <summary>
    /// Creates the result of a search that did not find an element.
    /// </summary>
    /// <param name="element">The element that was searched.</param>
    public static SearchResult NotFound(IElement element)
    {
        return new SearchResult(element, Enumerable.Empty<Rectangle>());
    }

    /// <summary>
    /// Throws an exception unless the element was found exactly once.
    /// </summary>
    /// <param name="waitFor">How long the element was waited for, which is part of the message of the <see cref="ElementNotFoundException"/> unless it is zero.</param>
    /// <exception cref="ElementNotFoundException">The element was not found.</exception>
    /// <exception cref="MultipleElementFoundException">The element was found at several locations.</exception>
    public void EnsureSingleLocation(TimeSpan waitFor)
    {
        if (this.Locations.Count == 0)
        {
            throw new ElementNotFoundException(this.Element, waitFor);
        }

        if (this.Locations.Count > 1)
        {
            throw new MultipleElementFoundException(this);
        }
    }
}
