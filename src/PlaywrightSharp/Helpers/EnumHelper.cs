using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace PlaywrightSharp.Helpers
{
    internal static class EnumHelper
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<Enum, string>> EnumToStringCache
            = new ConcurrentDictionary<Type, IReadOnlyDictionary<Enum, string>>();

        public static string ToValueString<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            var enumValues = EnumToStringCache.GetOrAdd(typeof(TEnum), type =>
            {
                string[] names = Enum.GetNames(type);
                var dictionary = new Dictionary<Enum, string>();
                foreach (string t in names)
                {
                    var field = type.GetField(t);
                    string valueName = field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? t;
                    var value = (TEnum)field.GetValue(null);
                    dictionary[value] = valueName;
                }

                return dictionary;
            });

            return enumValues[value];
        }

        public static TEnum EnsureDefaultValue<TEnum>(this TEnum value, TEnum defaultValue)
            where TEnum : Enum =>
            value switch
            {
                0 => default,
                _ => value,
            };
    }
}
