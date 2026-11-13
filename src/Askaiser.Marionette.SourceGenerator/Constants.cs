namespace Askaiser.Marionette.SourceGenerator;

internal static class Constants
{
    public const string AttributeFullName = "Askaiser.Marionette.ImageLibraryAttribute";

    /// <summary>Images are embedded in the assembly: screenshots of whole screens do not belong in a library.</summary>
    public const long MaxImageSize = 2 * 1024 * 1024;

    public const decimal DefaultThreshold = 0.95m;
}
