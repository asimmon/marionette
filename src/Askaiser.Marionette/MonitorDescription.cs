using System.Globalization;

namespace Askaiser.Marionette
{
    public record MonitorDescription(int Index, int Left, int Top, int Right, int Bottom)
        : Rectangle(Left, Top, Right, Bottom)
    {
        public bool IsPrimary
        {
            get => this.Left == 0 && this.Top == 0;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Monitor #{0} {1} {2}x{3}", this.Index, base.ToString(), this.Width, this.Height);
        }
    }
}
