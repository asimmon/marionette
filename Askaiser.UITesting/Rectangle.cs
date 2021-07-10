using System;
using System.Globalization;

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

        public Rectangle FromLeft(int width)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            var right = this.Left + width;
            if (right > this.Right) throw new ArgumentOutOfRangeException(nameof(width));

            return this with { Right = right };
        }

        public Rectangle FromTop(int height)
        {
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));
            var bottom = this.Top + height;
            if (bottom > this.Bottom) throw new ArgumentOutOfRangeException(nameof(height));

            return this with { Bottom = bottom };
        }

        public Rectangle FromBottom(int height)
        {
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));
            var top = this.Bottom - height;
            if (top < this.Top) throw new ArgumentOutOfRangeException(nameof(height));

            return this with { Top = top };
        }

        public Rectangle FromRight(int width)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            var left = this.Right - width;
            if (left < this.Left) throw new ArgumentOutOfRangeException(nameof(width));

            return this with { Left = left };
        }

        public Rectangle FromTopLeft(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

            var right = this.Left + width;
            var bottom = this.Top + height;

            if (right > this.Right) throw new ArgumentOutOfRangeException(nameof(width));
            if (bottom > this.Bottom) throw new ArgumentOutOfRangeException(nameof(height));

            return this with { Right = right, Bottom = bottom };
        }

        public Rectangle FromTopRight(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

            var left = this.Right - width;
            var bottom = this.Top + height;

            if (left < this.Left) throw new ArgumentOutOfRangeException(nameof(width));
            if (bottom > this.Bottom) throw new ArgumentOutOfRangeException(nameof(height));

            return this with { Left = left, Bottom = bottom };
        }

        public Rectangle FromBottomLeft(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

            var right = this.Left + width;
            var top = this.Bottom - height;

            if (right > this.Right) throw new ArgumentOutOfRangeException(nameof(width));
            if (top < this.Top) throw new ArgumentOutOfRangeException(nameof(height));

            return this with { Top = top, Right = right };
        }

        public Rectangle FromBottomRight(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

            var left = this.Right - width;
            var top = this.Bottom - height;

            if (left < this.Left) throw new ArgumentOutOfRangeException(nameof(width));
            if (top < this.Top) throw new ArgumentOutOfRangeException(nameof(height));

            return this with { Left = left, Top = top };
        }

        public Rectangle FromCenter(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (width > this.Width) throw new ArgumentOutOfRangeException(nameof(width));
            if (height > this.Height) throw new ArgumentOutOfRangeException(nameof(height));

            var halfWidth = (int)Math.Round(width / 2d);
            var halfHeight =  (int)Math.Round(height / 2d);

            var (centerX, centerY) = this.Center;

            var left = centerX - halfWidth;
            var top = centerY - halfHeight;
            var right = centerX + halfWidth;
            var bottom = centerY + halfHeight;

            var widthDiff = width - (right - left);
            var heightDiff = height - (bottom - top);

            if (widthDiff < 0)
                right += widthDiff;
            else
                left += widthDiff;

            if (heightDiff < 0)
                bottom += heightDiff;
            else
                top += heightDiff;

            return new Rectangle(left, top, right, bottom);
        }

        public Rectangle Multiply(double factor) => this with
        {
            Left = (int)Math.Round(this.Left * factor),
            Top = (int)Math.Round(this.Top * factor),
            Right = (int)Math.Round(this.Right * factor),
            Bottom = (int)Math.Round(this.Bottom * factor),
        };

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1},{2},{3})", this.Left, this.Top, this.Right, this.Bottom);
        }
    }
}
