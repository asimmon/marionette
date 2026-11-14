namespace Askaiser.Marionette;

/// <summary>
/// Something to look for on the screen. The driver knows how to find images (<see cref="ImageElement"/>) and text (<see cref="TextElement"/>).
/// </summary>
public interface IElement
{
    /// <summary>
    /// Gets the name of the element, which identifies it in an <see cref="ElementCollection"/> and in a <see cref="SearchResultCollection"/>.
    /// </summary>
    string Name { get; }
}
