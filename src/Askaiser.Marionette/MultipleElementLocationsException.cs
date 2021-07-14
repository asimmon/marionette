using System.Globalization;

namespace Askaiser.Marionette
{
    internal sealed class MultipleElementLocationsException : UITestingException
    {
        public MultipleElementLocationsException(SearchResult result)
            : base(string.Format(CultureInfo.InvariantCulture, "Element '{0}' was found at multiple locations: {1}.", result.Element, result.Areas.ToCenterString()))
        {
            this.Result = result;
        }

        public SearchResult Result { get; }
    }
}
