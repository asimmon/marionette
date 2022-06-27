namespace Askaiser.Marionette;

public sealed class MultipleElementFoundException : MarionetteException
{
    public MultipleElementFoundException(SearchResult result)
        : base(Messages.MultipleElementFoundException_Message.FormatInvariant(result.Element, result.Locations.ToCenterString()))
    {
        this.Result = result;
    }

    public SearchResult Result { get; }
}
