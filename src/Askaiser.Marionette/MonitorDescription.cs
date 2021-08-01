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
            return Messages.MonitorDescription_ToString.FormatInvariant(this.Index, base.ToString(), this.Width, this.Height);
        }
    }
}
