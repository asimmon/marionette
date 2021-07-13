using System;
using System.Globalization;

namespace Askaiser.Puppets
{
    internal sealed class ElementNotFoundException : UITestingException
    {
        private const string MessageFormatWithDuration = "Element '{0}' was not found within the allotted time: {1}.";
        private const string MessageFormatWithoutDuration = "Element '{0}' was not found.";

        public ElementNotFoundException(IElement element, TimeSpan duration)
            : base(string.Format(CultureInfo.InvariantCulture, duration == TimeSpan.Zero ? MessageFormatWithoutDuration : MessageFormatWithDuration, element, duration))
        {
            this.Element = element;
        }

        public IElement Element { get; }
    }
}
