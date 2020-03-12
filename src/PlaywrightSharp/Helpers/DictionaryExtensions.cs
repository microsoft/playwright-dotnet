using System.Collections.Generic;
#if NETSTANDARD2_1
using System.Collections.Concurrent;
#endif
using System.Linq;

namespace PlaywrightSharp.Helpers
{
    internal static class DictionaryExtensions
    {
        internal static Dictionary<TKey, TValue> Clone<TKey, TValue>(this Dictionary<TKey, TValue> dic)
            => dic.ToDictionary(entry => entry.Key, entry => entry.Value);

#if NETSTANDARD2_0
        internal static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            dictionary.TryGetValue(key, out var ret);
            return ret;
        }
#else
        internal static TValue GetValueOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
        {
            dictionary.TryGetValue(key, out var ret);
            return ret;
        }
#endif
    }
}
