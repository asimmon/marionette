using System;

namespace Askaiser.Marionette
{
    public sealed class ElementNotFoundException : MarionetteException
    {
        public ElementNotFoundException(IElement element, TimeSpan duration)
            : base((duration == TimeSpan.Zero ? Messages.ElementNotFoundException_Message_WithoutDuration : Messages.ElementNotFoundException_Message_WithDuration).FormatInvariant(element, duration))
        {
            this.Element = element;
        }

        public IElement Element { get; }
    }
}
