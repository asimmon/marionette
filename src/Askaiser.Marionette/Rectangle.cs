using System;
using System.Diagnostics.CodeAnalysis;

namespace Askaiser.Marionette;

[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "I don't want to")]
public record Rectangle
{
    public Rectangle(int left, int top, int right, int bottom)
    {
        if (left > right)
        {
            throw new ArgumentOutOfRangeException(nameof(left), Messages.Rectangle_Throw_LeftGreaterThanRight.FormatInvariant(left, right));
        }

        if (top > bottom)
        {
            throw new ArgumentOutOfRangeException(nameof(top), Messages.Rectangle_Throw_TopGreaterThanBottom.FormatInvariant(top, bottom));
        }

        this.Left = left;
        this.Top = top;
        this.Right = right;
        this.Bottom = bottom;
    }

    public Rectangle(Point topLeft, Point bottomRight)
        : this(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y)
    {
    }

    public int Left { get; private init; }

    public int Top { get; private init; }

    public int Right { get; private init; }

    public int Bottom { get; private init; }

    public int Width
    {
        get => this.Right - this.Left;
    }

    public int Height
    {
        get => this.Bottom - this.Top;
    }

    public Point TopLeft
    {
        get => new Point(this.Left, this.Top);
    }

    public Point TopRight
    {
        get => new Point(this.Right, this.Top);
    }

    public Point BottomLeft
    {
        get => new Point(this.Left, this.Bottom);
    }

    public Point BottomRight
    {
        get => new Point(this.Right, this.Bottom);
    }

    public Point CenterLeft
    {
        get => new Point(this.Left, this.Center.Y);
    }

    public Point CenterRight
    {
        get => new Point(this.Right, this.Center.Y);
    }

    public Point CenterTop
    {
        get => new Point(this.Center.X, this.Top);
    }

    public Point CenterBottom
    {
        get => new Point(this.Center.X, this.Bottom);
    }

    public Point Center
    {
        get
        {
            var halfWidth = (int)Math.Round(this.Width / 2d);
            var halfHeight = (int)Math.Round(this.Height / 2d);
            return new Point(this.Left + halfWidth, this.Top + halfHeight);
        }
    }

    public static Rectangle operator +(Rectangle rect, ValueTuple<int, int> xy) => rect with
    {
        Left = rect.Left + xy.Item1,
        Top = rect.Top + xy.Item2,
        Right = rect.Right + xy.Item1,
        Bottom = rect.Bottom + xy.Item2,
    };

    public static Rectangle operator -(Rectangle rect, ValueTuple<int, int> xy) => rect with
    {
        Left = rect.Left - xy.Item1,
        Top = rect.Top - xy.Item2,
        Right = rect.Right - xy.Item1,
        Bottom = rect.Bottom - xy.Item2,
    };

    public static Rectangle operator +(Rectangle rect, Point xy) => rect + (xy.X, xy.Y);

    public static Rectangle operator -(Rectangle rect, Point xy) => rect - (xy.X, xy.Y);

    public static Rectangle operator *(Rectangle rect, ValueTuple<double, double> xy) => rect with
    {
        Left = (int)Math.Round(rect.Left * xy.Item1),
        Top = (int)Math.Round(rect.Top * xy.Item2),
        Right = (int)Math.Round(rect.Right * xy.Item1),
        Bottom = (int)Math.Round(rect.Bottom * xy.Item2),
    };

    public static Rectangle operator /(Rectangle rect, ValueTuple<double, double> xy) => rect * (1d / xy.Item1, 1d / xy.Item2);

    public void Deconstruct(out int left, out int top, out int right, out int bottom)
    {
        left = this.Left;
        top = this.Top;
        right = this.Right;
        bottom = this.Bottom;
    }

    public Rectangle FromLeft(int width)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        var right = this.Left + width;
        if (right > this.Right)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        return this with { Right = right };
    }

    public Rectangle FromTop(int height)
    {
        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var bottom = this.Top + height;
        if (bottom > this.Bottom)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        return this with { Bottom = bottom };
    }

    public Rectangle FromBottom(int height)
    {
        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var top = this.Bottom - height;
        if (top < this.Top)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        return this with { Top = top };
    }

    public Rectangle FromRight(int width)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        var left = this.Right - width;
        if (left < this.Left)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        return this with { Left = left };
    }

    public Rectangle FromTopLeft(int width, int height)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var right = this.Left + width;
        var bottom = this.Top + height;

        if (right > this.Right)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (bottom > this.Bottom)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        return this with { Right = right, Bottom = bottom };
    }

    public Rectangle FromTopRight(int width, int height)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var left = this.Right - width;
        var bottom = this.Top + height;

        if (left < this.Left)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (bottom > this.Bottom)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        return this with { Left = left, Bottom = bottom };
    }

    public Rectangle FromBottomLeft(int width, int height)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var right = this.Left + width;
        var top = this.Bottom - height;

        if (right > this.Right)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (top < this.Top)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        return this with { Top = top, Right = right };
    }

    public Rectangle FromBottomRight(int width, int height)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var left = this.Right - width;
        var top = this.Bottom - height;

        if (left < this.Left)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (top < this.Top)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        return this with { Left = left, Top = top };
    }

    public Rectangle FromCenter(int width, int height)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (width > this.Width)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height > this.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var halfWidth = (int)Math.Round(width / 2d);
        var halfHeight = (int)Math.Round(height / 2d);

        var (centerX, centerY) = this.Center;

        var left = centerX - halfWidth;
        var top = centerY - halfHeight;
        var right = centerX + halfWidth;
        var bottom = centerY + halfHeight;

        var widthDiff = width - (right - left);
        var heightDiff = height - (bottom - top);

        if (widthDiff < 0)
        {
            right += widthDiff;
        }
        else
        {
            left += widthDiff;
        }

        if (heightDiff < 0)
        {
            bottom += heightDiff;
        }
        else
        {
            top += heightDiff;
        }

        return new Rectangle(left, top, right, bottom);
    }

    public override string ToString()
    {
        return Messages.Rectangle_ToString.FormatInvariant(this.Left, this.Top, this.Right, this.Bottom);
    }
}
