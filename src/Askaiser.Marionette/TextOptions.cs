using System;
using System.Diagnostics.CodeAnalysis;

namespace Askaiser.Marionette;

[Flags]
[SuppressMessage("Usage", "CA2217:Do not mark enums with FlagsAttribute", Justification = "We use bit fields")]
public enum TextOptions
{
    None = 0,
    Grayscale = 1 << 0,
    BlackAndWhite = 1 << 1 | Grayscale,
    Negative = 1 << 2,
}
