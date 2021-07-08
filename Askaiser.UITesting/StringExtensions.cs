using System;
using System.Globalization;
using System.Linq;

namespace Askaiser.UITesting
{
    internal static class StringExtensions
    {
        public static string ToPascalCasedPropertyName(this string text)
        {
            return string.Join(string.Empty, text
                .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.ToLowerInvariant())
                .Select(x => x.Replace(" ", ""))
                .Select(x => x.Length > 1 ? char.ToUpper(x[0], CultureInfo.InvariantCulture) + x[1..] : new string(char.ToUpper(x[0], CultureInfo.InvariantCulture), 1)));
        }
    }
}