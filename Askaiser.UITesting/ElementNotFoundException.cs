using System.Globalization;

namespace Askaiser.UITesting
{
    internal sealed class ElementNotFoundException : UITestingException
    {
        public ElementNotFoundException(IElement element)
            : base(string.Format(CultureInfo.InvariantCulture, "Element '{0}' was not found.", element))
        {
        }
    }
}
