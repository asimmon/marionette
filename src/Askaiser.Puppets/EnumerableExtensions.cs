using System.Collections.Generic;
using System.Linq;

namespace Askaiser.Puppets
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

        public static string ToCenterString(this IEnumerable<Rectangle> locations) => string.Join(", ", locations.Select(l => l.Center.ToString()));
    }
}
