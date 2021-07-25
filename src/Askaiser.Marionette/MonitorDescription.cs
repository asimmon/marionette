using System.Globalization;

namespace Askaiser.Marionette
{
    public record MonitorDescription(int Index, int Left, int Top, int Right, int Bottom)
        : Rectangle(Left, Top, Right, Bottom)
    {
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Monitor #{0} {1}", this.Index, base.ToString());
        }
    }
}
