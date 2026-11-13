namespace Askaiser.Marionette.Recognition.Images;

/// <summary>
/// A linear projection of BGR colors onto a single integer channel. Image search runs on one of these planes
/// before candidates are verified against the full-color image. The three projections are orthogonal, so the
/// energy of any color image splits exactly between them and at least one carries a third of it.
/// </summary>
internal enum ColorPlaneKind
{
    /// <summary>B + G + R, an unweighted luminance. Used for almost every image.</summary>
    Sum = 0,

    /// <summary>R - B. Used for images whose contrast is mostly between red and blue tones of equal brightness.</summary>
    RedMinusBlue = 1,

    /// <summary>B - 2G + R. Used for images whose contrast is mostly between green and magenta tones of equal brightness.</summary>
    GreenContrast = 2,
}
