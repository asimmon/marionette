namespace Askaiser.UITesting
{
    public record Rectangle(int Left, int Top, int Right, int Bottom)
    {
        public int Width => this.Right - this.Left;
        public int Heigh => this.Bottom - this.Top;

        public void Deconstruct(out int left, out int top, out int right, out int bottom)
        {
            left = this.Left;
            top = this.Top;
            right = this.Right;
            bottom = this.Bottom;
        }

        internal Rectangle AddOffset(int leftOffset, int topOffset) => this with
        {
            Left = this.Left + leftOffset,
            Top = this.Top + topOffset,
            Right = this.Right + leftOffset,
            Bottom = this.Bottom + topOffset,
        };
    }
}