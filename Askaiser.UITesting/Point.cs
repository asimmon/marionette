using System.Globalization;

namespace Askaiser.UITesting
{
    public record Point(int X, int Y)
    {
        public void Deconstruct(out int x, out int y)
        {
            x = this.X;
            y = this.Y;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1})", this.X, this.Y);
        }
    }
}
