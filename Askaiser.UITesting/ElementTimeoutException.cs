using System;
using System.Globalization;

namespace Askaiser.UITesting
{
    internal sealed class ElementTimeoutException : UITestingException
    {
        public ElementTimeoutException(IElement element, TimeSpan duration)
            : base(string.Format(CultureInfo.InvariantCulture, "Element {0} not found within the allotted time: {1}.", element, duration))
        {
        }
    }
}
