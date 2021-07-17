using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Askaiser.Marionette.SourceGenerator
{
    internal static class StringExtensions
    {
        private static readonly Regex NonAlphanumericalRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string ToCSharpPropertyName(this string text)
        {
            return string.Join(string.Empty, text
                .Split('-').TrimAndRemoveEmptyEntries().ToArray()
                .Select(x => x.ToLowerInvariant())
                .Select(x => NonAlphanumericalRegex.Replace(x, string.Empty))
                .Select(x =>
                {
                    return x.Length > 1
                        ? char.ToUpper(x[0], CultureInfo.InvariantCulture) + x.Substring(1)
                        : new string(char.ToUpper(x[0], CultureInfo.InvariantCulture), 1);
                }));
        }
    }
}
