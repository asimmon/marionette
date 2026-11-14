using System;

namespace Askaiser.Marionette;

/// <summary>
/// A rectangle in screen pixels. <see cref="Right"/> and <see cref="Bottom"/> are exclusive: a rectangle covering
/// a single pixel at the origin is <c>(0, 0, 1, 1)</c>.
/// </summary>
public record Rectangle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Rectangle"/> class from the coordinates of its edges.
    /// </summary>
    /// <param name="left">The X coordinate of the left edge, which is the first column of pixels inside the rectangle.</param>
    /// <param name="top">The Y coordinate of the top edge, which is the first row of pixels inside the rectangle.</param>
    /// <param name="right">The X coordinate of the right edge, which is the first column of pixels after the rectangle.</param>
    /// <param name="bottom">The Y coordinate of the bottom edge, which is the first row of pixels after the rectangle.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="left"/> is greater than <paramref name="right"/>, or <paramref name="top"/> is greater than <paramref name="bottom"/>.</exception>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="Rectangle"/> class from two opposite corners.
    /// </summary>
    /// <param name="topLeft">The top-left corner, which is inside the rectangle.</param>
    /// <param name="bottomRight">The bottom-right corner, which is just outside the rectangle.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="topLeft"/> is on the right of, or below, <paramref name="bottomRight"/>.</exception>
    public Rectangle(Point topLeft, Point bottomRight)
        : this(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y)
    {
    }

    /// <summary>Gets the X coordinate of the left edge, which is the first column of pixels inside the rectangle.</summary>
    public int Left { get; private init; }

    /// <summary>Gets the Y coordinate of the top edge, which is the first row of pixels inside the rectangle.</summary>
    public int Top { get; private init; }

    /// <summary>Gets the X coordinate of the right edge, which is the first column of pixels after the rectangle.</summary>
    public int Right { get; private init; }

    /// <summary>Gets the Y coordinate of the bottom edge, which is the first row of pixels after the rectangle.</summary>
    public int Bottom { get; private init; }

    /// <summary>Gets the width of the rectangle in pixels.</summary>
    public int Width => this.Right - this.Left;

    /// <summary>Gets the height of the rectangle in pixels.</summary>
    public int Height => this.Bottom - this.Top;

    /// <summary>Gets the top-left corner of the rectangle.</summary>
    public Point TopLeft => new Point(this.Left, this.Top);

    /// <summary>Gets the top-right corner of the rectangle.</summary>
    public Point TopRight => new Point(this.Right, this.Top);

    /// <summary>Gets the bottom-left corner of the rectangle.</summary>
    public Point BottomLeft => new Point(this.Left, this.Bottom);

    /// <summary>Gets the bottom-right corner of the rectangle.</summary>
    public Point BottomRight => new Point(this.Right, this.Bottom);

    /// <summary>Gets the middle of the left edge of the rectangle.</summary>
    public Point CenterLeft => new Point(this.Left, this.Center.Y);

    /// <summary>Gets the middle of the right edge of the rectangle.</summary>
    public Point CenterRight => new Point(this.Right, this.Center.Y);

    /// <summary>Gets the middle of the top edge of the rectangle.</summary>
    public Point CenterTop => new Point(this.Center.X, this.Top);

    /// <summary>Gets the middle of the bottom edge of the rectangle.</summary>
    public Point CenterBottom => new Point(this.Center.X, this.Bottom);

    /// <summary>Gets the center of the rectangle. This is where mouse actions on an element happen.</summary>
    public Point Center
    {
        get
        {
            var halfWidth = (int)Math.Round(this.Width / 2d);
            var halfHeight = (int)Math.Round(this.Height / 2d);
            return new Point(this.Left + halfWidth, this.Top + halfHeight);
        }
    }

    /// <summary>
    /// Moves a rectangle by a horizontal and a vertical distance, without changing its size.
    /// </summary>
    /// <param name="rect">The rectangle to move.</param>
    /// <param name="xy">The distances, in pixels, to add to the X and Y coordinates.</param>
    public static Rectangle operator +(Rectangle rect, ValueTuple<int, int> xy) => rect with
    {
        Left = rect.Left + xy.Item1,
        Top = rect.Top + xy.Item2,
        Right = rect.Right + xy.Item1,
        Bottom = rect.Bottom + xy.Item2,
    };

    /// <summary>
    /// Moves a rectangle by a horizontal and a vertical distance in the opposite direction, without changing its size.
    /// </summary>
    /// <param name="rect">The rectangle to move.</param>
    /// <param name="xy">The distances, in pixels, to subtract from the X and Y coordinates.</param>
    public static Rectangle operator -(Rectangle rect, ValueTuple<int, int> xy) => rect with
    {
        Left = rect.Left - xy.Item1,
        Top = rect.Top - xy.Item2,
        Right = rect.Right - xy.Item1,
        Bottom = rect.Bottom - xy.Item2,
    };

    /// <summary>
    /// Moves a rectangle by the coordinates of a point, without changing its size.
    /// This converts a rectangle relative to a monitor into desktop coordinates when the point is the top-left corner of the monitor.
    /// </summary>
    /// <param name="rect">The rectangle to move.</param>
    /// <param name="xy">The point whose coordinates are added to those of the rectangle.</param>
    public static Rectangle operator +(Rectangle rect, Point xy) => rect + (xy.X, xy.Y);

    /// <summary>
    /// Moves a rectangle by the coordinates of a point in the opposite direction, without changing its size.
    /// This converts a rectangle in desktop coordinates into a rectangle relative to a monitor when the point is the top-left corner of the monitor.
    /// </summary>
    /// <param name="rect">The rectangle to move.</param>
    /// <param name="xy">The point whose coordinates are subtracted from those of the rectangle.</param>
    public static Rectangle operator -(Rectangle rect, Point xy) => rect - (xy.X, xy.Y);

    /// <summary>
    /// Scales a rectangle from the origin: the coordinates of its edges are multiplied by a horizontal and a vertical factor, then rounded.
    /// Both the position and the size of the rectangle change.
    /// </summary>
    /// <param name="rect">The rectangle to scale.</param>
    /// <param name="xy">The factors that the X and Y coordinates are multiplied by.</param>
    public static Rectangle operator *(Rectangle rect, ValueTuple<double, double> xy) => rect with
    {
        Left = (int)Math.Round(rect.Left * xy.Item1),
        Top = (int)Math.Round(rect.Top * xy.Item2),
        Right = (int)Math.Round(rect.Right * xy.Item1),
        Bottom = (int)Math.Round(rect.Bottom * xy.Item2),
    };

    /// <summary>
    /// Scales a rectangle from the origin: the coordinates of its edges are divided by a horizontal and a vertical factor, then rounded.
    /// Both the position and the size of the rectangle change.
    /// </summary>
    /// <param name="rect">The rectangle to scale.</param>
    /// <param name="xy">The factors that the X and Y coordinates are divided by.</param>
    public static Rectangle operator /(Rectangle rect, ValueTuple<double, double> xy) => rect * (1d / xy.Item1, 1d / xy.Item2);

    /// <summary>
    /// Deconstructs the rectangle into the coordinates of its edges.
    /// </summary>
    /// <param name="left">The X coordinate of the left edge.</param>
    /// <param name="top">The Y coordinate of the top edge.</param>
    /// <param name="right">The X coordinate of the right edge, which is exclusive.</param>
    /// <param name="bottom">The Y coordinate of the bottom edge, which is exclusive.</param>
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

    /// <summary>Returns the coordinates of the edges of the rectangle, formatted as <c>(left,top,right,bottom)</c>.</summary>
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
