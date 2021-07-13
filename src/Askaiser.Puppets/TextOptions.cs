using System;

namespace Askaiser.Puppets
{
    [Flags]
    public enum TextOptions
    {
        None = 0,
        Grayscale = 1 << 0,
        BlackAndWhite = 1 << 1 | Grayscale,
        Negative = 1 << 2,
    }
}
