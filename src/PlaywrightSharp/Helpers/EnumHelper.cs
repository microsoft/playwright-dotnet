using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PlaywrightSharp.Helpers
{
    internal static class EnumHelper
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, Enum>> EnumCache
            = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, Enum>>();

        public static TEnum ToEnum<TEnum>(this string value)
            where TEnum : Enum
        {
            var enumValues = EnumCache.GetOrAdd(typeof(TEnum), type =>
            {
                string[] names = Enum.GetNames(type);
                var values = (TEnum[])Enum.GetValues(type);
                var dictionary = new Dictionary<string, Enum>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < names.Length; i++)
                {
                    dictionary.Add(names[i], values[i]);
                }

                return dictionary;
            });
            return (TEnum)enumValues[value];
        }
    }
}
