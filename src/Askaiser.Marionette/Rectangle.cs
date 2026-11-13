using System;

namespace Askaiser.Marionette;

/// <summary>
/// A rectangle in screen pixels. <see cref="Right"/> and <see cref="Bottom"/> are exclusive: a rectangle covering
/// a single pixel at the origin is <c>(0, 0, 1, 1)</c>.
/// </summary>
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

    public int Width => this.Right - this.Left;

    public int Height => this.Bottom - this.Top;

    public Point TopLeft => new Point(this.Left, this.Top);

    public Point TopRight => new Point(this.Right, this.Top);

    public Point BottomLeft => new Point(this.Left, this.Bottom);

    public Point BottomRight => new Point(this.Right, this.Bottom);

    public Point CenterLeft => new Point(this.Left, this.Center.Y);

    public Point CenterRight => new Point(this.Right, this.Center.Y);

    public Point CenterTop => new Point(this.Center.X, this.Top);

    public Point CenterBottom => new Point(this.Center.X, this.Bottom);

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

    /// <summary>Returns the leftmost part of this rectangle, with the specified width.</summary>
    public Rectangle FromLeft(int width)
    {
        this.EnsureWidthFits(width);
        return this with { Right = this.Left + width };
    }

    /// <summary>Returns the topmost part of this rectangle, with the specified height.</summary>
    public Rectangle FromTop(int height)
    {
        this.EnsureHeightFits(height);
        return this with { Bottom = this.Top + height };
    }

    /// <summary>Returns the bottommost part of this rectangle, with the specified height.</summary>
    public Rectangle FromBottom(int height)
    {
        this.EnsureHeightFits(height);
        return this with { Top = this.Bottom - height };
    }

    /// <summary>Returns the rightmost part of this rectangle, with the specified width.</summary>
    public Rectangle FromRight(int width)
    {
        this.EnsureWidthFits(width);
        return this with { Left = this.Right - width };
    }

    /// <summary>Returns the top-left corner of this rectangle, with the specified size.</summary>
    public Rectangle FromTopLeft(int width, int height)
    {
        this.EnsureWidthFits(width);
        this.EnsureHeightFits(height);
        return this with { Right = this.Left + width, Bottom = this.Top + height };
    }

    /// <summary>Returns the top-right corner of this rectangle, with the specified size.</summary>
    public Rectangle FromTopRight(int width, int height)
    {
        this.EnsureWidthFits(width);
        this.EnsureHeightFits(height);
        return this with { Left = this.Right - width, Bottom = this.Top + height };
    }

    /// <summary>Returns the bottom-left corner of this rectangle, with the specified size.</summary>
    public Rectangle FromBottomLeft(int width, int height)
    {
        this.EnsureWidthFits(width);
        this.EnsureHeightFits(height);
        return this with { Top = this.Bottom - height, Right = this.Left + width };
    }

    /// <summary>Returns the bottom-right corner of this rectangle, with the specified size.</summary>
    public Rectangle FromBottomRight(int width, int height)
    {
        this.EnsureWidthFits(width);
        this.EnsureHeightFits(height);
        return this with { Left = this.Right - width, Top = this.Bottom - height };
    }

    /// <summary>Returns the central part of this rectangle, with the specified size.</summary>
    public Rectangle FromCenter(int width, int height)
    {
        this.EnsureWidthFits(width);
        this.EnsureHeightFits(height);

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

    /// <summary>
    /// Returns the part of this rectangle that is also inside another one, or null when they do not overlap.
    /// </summary>
    internal Rectangle? Intersect(Rectangle other)
    {
        var left = Math.Max(this.Left, other.Left);
        var top = Math.Max(this.Top, other.Top);
        var right = Math.Min(this.Right, other.Right);
        var bottom = Math.Min(this.Bottom, other.Bottom);

        return left < right && top < bottom ? new Rectangle(left, top, right, bottom) : null;
    }

    private void EnsureWidthFits(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, this.Width);
    }

    private void EnsureHeightFits(int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, this.Height);
    }
}
