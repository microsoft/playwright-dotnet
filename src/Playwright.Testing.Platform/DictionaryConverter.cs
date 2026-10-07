using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Testing.Platform;

internal sealed class DictionaryConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType || typeToConvert.GetGenericTypeDefinition() != typeof(IEnumerable<>))
        {
            return false;
        }
        var itemType = typeToConvert.GetGenericArguments()[0];
        return itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>) &&
            itemType.GetGenericArguments()[0] == typeof(string);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(
            typeToConvert.GetGenericArguments()[0].GetGenericArguments()[1]))!;

    private sealed class Converter<T> : JsonConverter<IEnumerable<KeyValuePair<string, T>>>
    {
        public override IEnumerable<KeyValuePair<string, T>>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<Dictionary<string, T>>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, IEnumerable<KeyValuePair<string, T>> value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value.ToDictionary(pair => pair.Key, pair => pair.Value), options);
    }
}
