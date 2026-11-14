namespace Askaiser.Marionette;

/// <summary>
/// The exception thrown when an element that was expected at a single location is visible at several locations on the screen.
/// Searching a smaller part of the screen with a <c>searchRect</c> usually removes the ambiguity.
/// </summary>
public sealed class MultipleElementFoundException : MarionetteException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultipleElementFoundException"/> class.
    /// </summary>
    /// <param name="result">The result of the search, with every location of the element.</param>
    public MultipleElementFoundException(SearchResult result)
        : base(Messages.MultipleElementFoundException_Message.FormatInvariant(result.Element, result.Locations.ToCenterString()))
    {
        this.Result = result;
    }

    /// <summary>
    /// Gets the result of the search, with every location of the element.
    /// </summary>
    public SearchResult Result { get; }
}
