using System.Globalization;

namespace Askaiser.Marionette
{
    internal sealed class MultipleElementFoundException : MarionetteException
    {
        public MultipleElementFoundException(SearchResult result)
            : base(string.Format(CultureInfo.InvariantCulture, "Element '{0}' was found at multiple locations: {1}.", result.Element, result.Locations.ToCenterString()))
        {
            this.Result = result;
        }

        public SearchResult Result { get; }
    }
}
