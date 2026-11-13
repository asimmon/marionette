namespace Askaiser.Marionette.Recognition.Text;

/// <summary>
/// A piece of text read on the screen, with the horizontal extent of each of its characters.
/// Coordinates are in pixels, relative to the image the text was read from.
/// </summary>
internal sealed class TextLine
{
    public TextLine(string text, float[] characterLefts, float[] characterRights, int top, int bottom, float confidence)
    {
        this.Text = text;
        this.CharacterLefts = characterLefts;
        this.CharacterRights = characterRights;
        this.Top = top;
        this.Bottom = bottom;
        this.Confidence = confidence;
    }

    public string Text { get; }

    /// <summary>Gets the left edge of every character of <see cref="Text"/>.</summary>
    public float[] CharacterLefts { get; }

    /// <summary>Gets the right edge of every character of <see cref="Text"/>.</summary>
    public float[] CharacterRights { get; }

    public int Top { get; }

    public int Bottom { get; }

    public float Confidence { get; }

    public float Left => this.CharacterLefts.Length > 0 ? this.CharacterLefts[0] : 0;

    public float Right => this.CharacterRights.Length > 0 ? this.CharacterRights[^1] : 0;

    public override string ToString()
    {
        return this.Text;
    }
}
