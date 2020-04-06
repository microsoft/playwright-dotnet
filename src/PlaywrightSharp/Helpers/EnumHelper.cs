using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace PlaywrightSharp.Helpers
{
    internal static class EnumHelper
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, Enum>> StringToEnumCache
            = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, Enum>>();

        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<Enum, string>> EnumToStringCache
            = new ConcurrentDictionary<Type, IReadOnlyDictionary<Enum, string>>();

        public static TEnum ToEnum<TEnum>(this string value)
            where TEnum : Enum
        {
            var enumValues = StringToEnumCache.GetOrAdd(typeof(TEnum), type =>
            {
                string[] names = Enum.GetNames(type);
                var values = (TEnum[])Enum.GetValues(type);
                var dictionary = new Dictionary<string, Enum>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < names.Length; i++)
                {
                    dictionary.Add(names[i], values[i]);
                    string value = type.GetField(names[i]).GetCustomAttribute<EnumMemberAttribute>()?.Value;
                    if (value != null)
                    {
                        dictionary[value] = values[i];
                    }
                }

                return dictionary;
            });
            return (TEnum)enumValues[value];
        }

        public static string ToValueString<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            var enumValues = EnumToStringCache.GetOrAdd(typeof(TEnum), type =>
            {
                string[] names = Enum.GetNames(type);
                var dictionary = new Dictionary<Enum, string>();
                for (int i = 0; i < names.Length; i++)
                {
                    var field = type.GetField(names[i]);
                    string valueName = field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? names[i];
                    var value = (TEnum)field.GetValue(null);
                    dictionary[value] = valueName;
                }

                return dictionary;
            });

            return enumValues[value];
        }
    }
}
