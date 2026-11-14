using System;

namespace Askaiser.Marionette;

/// <summary>
/// The exception thrown when an element is not visible on the screen, or did not show up within the allotted time.
/// </summary>
public sealed class ElementNotFoundException : MarionetteException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElementNotFoundException"/> class.
    /// </summary>
    /// <param name="element">The element that was not found.</param>
    /// <param name="duration">How long the element was waited for, which is part of the message unless it is zero.</param>
    public ElementNotFoundException(IElement element, TimeSpan duration)
        : base((duration == TimeSpan.Zero ? Messages.ElementNotFoundException_Message_WithoutDuration : Messages.ElementNotFoundException_Message_WithDuration).FormatInvariant(element, duration))
    {
        this.Element = element;
    }

    /// <summary>
    /// Gets the element that was not found.
    /// </summary>
    public IElement Element { get; }
}
