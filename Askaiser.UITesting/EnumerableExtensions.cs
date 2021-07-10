using System.Collections.Generic;
using System.Linq;

namespace Askaiser.UITesting
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<string> TrimAndRemoveEmptyEntries(this IEnumerable<string> elements)
        {
            foreach (var element in elements)
            {
                if (element.Trim() is { Length: > 0 } trimmedElement)
                    yield return trimmedElement;
            }
        }

        public static string ToCenterString(this IEnumerable<Rectangle> locations) => string.Join(", ", locations.Select(l =>
        {
            var (x, y) = l.Center;
            return $"({x},{y})";
        }));
    }
}
