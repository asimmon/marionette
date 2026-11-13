using System;
using System.Diagnostics;

namespace Askaiser.Marionette;

/// <summary>
/// A text to look for on the screen.
/// </summary>
[DebuggerDisplay("{Name}")]
public sealed class TextElement : IElement
{
    private readonly string _name = string.Empty;
    private readonly string _content = string.Empty;

    public TextElement(string name, string content, TextOptions options)
    {
        this.Name = name;
        this.Content = content;
        this.Options = options;
        this.IgnoreCase = true;
    }

    public TextElement(string content, TextOptions options = TextOptions.None)
        : this(Guid.NewGuid().ToString("N"), content, options)
    {
    }

    /// <summary>
    /// Gets the name of the element.
    /// </summary>
    public string Name
    {
        get => this._name;
        init => this._name = value?.Trim() is { Length: > 0 } trimmedValue ? trimmedValue : throw new ArgumentException(Messages.Element_Throw_InvalidName, nameof(value));
    }

    /// <summary>
    /// Gets the text to look for. Consecutive whitespace characters are treated as a single space.
    /// </summary>
    public string Content
    {
        get => this._content;
        init => this._content = value is { Length: > 0 } ? value : throw new ArgumentException(Messages.TextElement_Throw_EmptyContent, nameof(value));
    }

    /// <summary>
    /// Gets the preprocessing applied to the screen before text recognition.
    /// </summary>
    public TextOptions Options { get; init; }

    /// <summary>
    /// Gets a value indicating whether the case of letters is ignored. The default value is true.
    /// </summary>
    public bool IgnoreCase { get; init; }

    public override string ToString()
    {
        return this.Content;
    }
}
