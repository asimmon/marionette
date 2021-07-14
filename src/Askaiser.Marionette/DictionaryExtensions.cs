using System;
using System.Collections.Generic;

namespace Askaiser.Marionette
{
    internal static class DictionaryExtensions
    {
        public static TVal GetOrCreate<TKey, TVal>(this Dictionary<TKey, TVal> dictionary, TKey key, Func<TKey, TVal> valueFactory)
        {
            return dictionary.TryGetValue(key, out var value) ? value : dictionary[key] = valueFactory(key);
        }
    }
}
