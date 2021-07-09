using System.Collections.Generic;

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
    }
}
