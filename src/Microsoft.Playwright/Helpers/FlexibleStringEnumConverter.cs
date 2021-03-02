using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Helpers
{
    /// <summary>
    /// It converts a string to an enum, using a default value as a fallback.
    /// </summary>
    /// <typeparam name="T">Type to convert to.</typeparam>
    public class FlexibleStringEnumConverter<T> : JsonConverter<T>
    {
        private readonly Type _enumType = typeof(T);
        private readonly Dictionary<string, EnumInfo> _transformedToRaw;
        private readonly T _default;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlexibleStringEnumConverter{T}"/> class.
        /// </summary>
        /// <param name="defaultValue">Default value.</param>
        public FlexibleStringEnumConverter(T defaultValue)
        {
            _default = defaultValue;
            string[] builtInNames = _enumType.GetEnumNames();
            var builtInValues = _enumType.GetEnumValues();

            _transformedToRaw = new Dictionary<string, EnumInfo>();

            for (int i = 0; i < builtInNames.Length; i++)
            {
                Enum enumValue = (Enum)builtInValues.GetValue(i);
                int rawValue = Convert.ToInt32(enumValue);

                string name = builtInNames[i];
                var field = _enumType.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                var enumMemberAttribute = field.GetCustomAttribute<EnumMemberAttribute>(true);
                string transformedName = enumMemberAttribute?.Value ?? name;

                _transformedToRaw[transformedName] = new EnumInfo(name, enumValue, rawValue);
            }
        }

        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeof(T).IsAssignableFrom(typeToConvert);

        /// <inheritdoc/>
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var token = reader.TokenType;

            if (token == JsonTokenType.String)
            {
                string enumString = reader.GetString();

                // Case sensitive search attempted first.
                if (_transformedToRaw.TryGetValue(enumString, out var enumInfo))
                {
                    return (T)Enum.ToObject(_enumType, enumInfo.RawValue);
                }

                // Case insensitive search attempted second.
                foreach (var enumItem in _transformedToRaw)
                {
                    if (string.Equals(enumItem.Key, enumString, StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)Enum.ToObject(_enumType, enumItem.Value.RawValue);
                    }
                }
            }

            return _default;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => throw new NotImplementedException();

        private class EnumInfo
        {
            public EnumInfo(string name, Enum enumValue, int rawValue)
            {
                Name = name;
                EnumValue = enumValue;
                RawValue = rawValue;
            }

            public string Name { get; set; }

            public Enum EnumValue { get; set; }

            public int RawValue { get; set; }
        }
    }
}
