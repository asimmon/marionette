namespace Askaiser.Marionette.Recognition.Text;

/// <summary>
/// The bounds of a piece of text located on an image, before its characters are read.
/// </summary>
internal readonly record struct TextBox(int Left, int Top, int Right, int Bottom)
{
    public int Width => this.Right - this.Left;

    public int Height => this.Bottom - this.Top;
}
