namespace Askaiser.UITesting
{
    public record Point(int X, int Y)
    {
        public void Deconstruct(out int x, out int y)
        {
            x = this.X;
            y = this.Y;
        }
    }
}