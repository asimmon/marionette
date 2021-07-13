namespace Askaiser.Puppets
{
    public record MonitorDescription(int Index, int Left, int Top, int Right, int Bottom) : Rectangle(Left, Top, Right, Bottom);
}
