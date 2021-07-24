using System;
using System.Globalization;

namespace Askaiser.Marionette
{
    public record Point(int X, int Y)
    {
        public void Deconstruct(out int x, out int y)
        {
            x = this.X;
            y = this.Y;
        }

        public static Point operator +(Point point, ValueTuple<int, int> xy) => point with
        {
            X = point.X + xy.Item1,
            Y = point.Y + xy.Item2,
        };

        public static Point operator -(Point point, ValueTuple<int, int> xy) => point + (-xy.Item1, -xy.Item2);

        public static Point operator +(Point p1, Point p2) => p1 + (p2.X, p2.Y);

        public static Point operator -(Point p1, Point p2) => p1 + (-p2.X, -p2.Y);

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1})", this.X, this.Y);
        }
    }
}
