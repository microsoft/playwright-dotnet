using System;
using System.Collections.Generic;
using System.Linq;

namespace PlaywrightSharp.Helpers
{
    internal static class DictionaryExtensions
    {
        internal static Dictionary<TKey, TValue> Clone<TKey, TValue>(this Dictionary<TKey, TValue> dic)
            => dic.ToDictionary(entry => entry.Key, entry => entry.Value);

        internal static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            dictionary.TryGetValue(key, out var ret);
            return ret;
        }
    }
}
