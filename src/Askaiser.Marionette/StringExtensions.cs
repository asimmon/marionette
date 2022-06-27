using System.Globalization;

namespace Askaiser.Marionette;

internal static class StringExtensions
{
    public static string FormatInvariant(this string format, object arg)
    {
        return string.Format(CultureInfo.InvariantCulture, format, arg);
    }

    public static string FormatInvariant(this string format, object arg0, object arg1)
    {
        return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1);
    }

    public static string FormatInvariant(this string format, object arg0, object arg1, object arg2)
    {
        return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2);
    }

    public static string FormatInvariant(this string format, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
