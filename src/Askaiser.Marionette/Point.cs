using System;
using System.Diagnostics.CodeAnalysis;

namespace Askaiser.Marionette;

/// <summary>
/// A position in screen pixels, in the coordinates of the virtual desktop that spans all monitors:
/// the origin is the top-left corner of the primary monitor, and monitors on its left or above it have negative coordinates.
/// </summary>
/// <param name="X">The horizontal coordinate, which grows towards the right.</param>
/// <param name="Y">The vertical coordinate, which grows towards the bottom.</param>
[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "I don't want to")]
public record Point(int X, int Y)
{
    /// <summary>The point at the origin, <c>(0,0)</c>, which is the top-left corner of the primary monitor.</summary>
    public static readonly Point Empty = new Point(0, 0);

    /// <summary>
    /// Moves a point by a horizontal and a vertical distance.
    /// </summary>
    /// <param name="point">The point to move.</param>
    /// <param name="xy">The distances, in pixels, to add to the X and Y coordinates.</param>
    public static Point operator +(Point point, ValueTuple<int, int> xy) => new Point(X: point.X + xy.Item1, Y: point.Y + xy.Item2);

    /// <summary>
    /// Moves a point by a horizontal and a vertical distance in the opposite direction.
    /// </summary>
    /// <param name="point">The point to move.</param>
    /// <param name="xy">The distances, in pixels, to subtract from the X and Y coordinates.</param>
    public static Point operator -(Point point, ValueTuple<int, int> xy) => point + (-xy.Item1, -xy.Item2);

    /// <summary>
    /// Adds the coordinates of two points.
    /// </summary>
    /// <param name="p1">The first point.</param>
    /// <param name="p2">The point whose coordinates are added to those of <paramref name="p1"/>.</param>
    public static Point operator +(Point p1, Point p2) => p1 + (p2.X, p2.Y);

    /// <summary>
    /// Subtracts the coordinates of a point from those of another one.
    /// </summary>
    /// <param name="p1">The first point.</param>
    /// <param name="p2">The point whose coordinates are subtracted from those of <paramref name="p1"/>.</param>
    public static Point operator -(Point p1, Point p2) => p1 + (-p2.X, -p2.Y);

    /// <summary>
    /// Deconstructs the point into its coordinates.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    public void Deconstruct(out int x, out int y)
    {
        x = this.X;
        y = this.Y;
    }

    /// <summary>Returns the coordinates of the point, formatted as <c>(x,y)</c>.</summary>
    public override string ToString()
    {
        return Messages.Point_ToString.FormatInvariant(this.X, this.Y);
    }
}
