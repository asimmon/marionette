using System;

namespace Askaiser.UITesting
{
    public record Rectangle
    {
        public Rectangle(int left, int top, int right, int bottom)
        {
            if (left < 0) throw new ArgumentOutOfRangeException(nameof(left), $"Left property cannot be negative: {left}.");
            if (top < 0) throw new ArgumentOutOfRangeException(nameof(top), $"Top property cannot be negative: {top}.");
            if (right < 0) throw new ArgumentOutOfRangeException(nameof(right), $"Right property cannot be negative: {right}.");
            if (bottom < 0) throw new ArgumentOutOfRangeException(nameof(bottom), $"Bottom property cannot be negative: {bottom}.");

            if (left > right) throw new ArgumentOutOfRangeException(nameof(left), $"Left property {left} is greater than the Right property {right}.");
            if (top > bottom) throw new ArgumentOutOfRangeException(nameof(top), $"Top property {top} is greater than the Bottom property {bottom}.");

            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }

        public int Left { get; private init; }
        public int Top { get; private init; }
        public int Right { get; private init; }
        public int Bottom { get; private init; }

        public int Width => this.Right - this.Left;
        public int Height => this.Bottom - this.Top;

        public Point Center
        {
            get
            {
                var halfWidth = (int)Math.Round(this.Width / 2d);
                var halfHeight = (int)Math.Round(this.Height / 2d);
                return new Point(this.Left + halfWidth, this.Top + halfHeight);
            }
        }

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