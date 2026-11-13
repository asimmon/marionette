using System;
using System.Diagnostics.CodeAnalysis;

namespace Askaiser.Marionette;

/// <summary>
/// Optional preprocessing applied to the screen before text recognition.
/// The default text recognition engine works best on unmodified screenshots, so <see cref="None"/> is the recommended value.
/// </summary>
[Flags]
[SuppressMessage("Usage", "CA2217:Do not mark enums with FlagsAttribute", Justification = "We use bit fields")]
public enum TextOptions
{
    /// <summary>Do not preprocess the screen. This is the default and the most accurate option in most cases.</summary>
    None = 0,

    /// <summary>Convert the screen to grayscale.</summary>
    Grayscale = 1 << 0,

    /// <summary>Convert the screen to grayscale then to pure black and white using an automatic threshold.</summary>
    BlackAndWhite = 1 << 1 | Grayscale,

    /// <summary>Invert the colors of the screen.</summary>
    Negative = 1 << 2,
}
