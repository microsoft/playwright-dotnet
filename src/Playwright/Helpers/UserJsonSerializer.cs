// MIT License

using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright.Helpers;

internal static class UserJsonSerializer
{
    internal static string Serialize(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is JsonElement je)
        {
            return je.GetRawText();
        }

        if (value is JsonNode jn)
        {
            return jn.ToJsonString();
        }

        var type = value.GetType();

        var typeInfo = PlaywrightJsonContext.Default.GetTypeInfo(type);
        if (typeInfo != null)
        {
            return JsonSerializer.Serialize(value, typeInfo);
        }

        if (value is IDictionary<string, object?> dict)
        {
            return JsonSerializer.Serialize(dict, PlaywrightJsonContext.Default.DictionaryOfStringToObject);
        }

        if (value is IDictionary rawDict)
        {
            var concrete = new Dictionary<string, object?>();
            foreach (DictionaryEntry entry in rawDict)
            {
                concrete[entry.Key?.ToString() ?? string.Empty] = entry.Value;
            }

            return JsonSerializer.Serialize(concrete, PlaywrightJsonContext.Default.DictionaryOfStringToObject);
        }

        if (value is IList list)
        {
            var items = new List<object?>(list.Count);
            foreach (var item in list)
            {
                items.Add(item);
            }

            return JsonSerializer.Serialize(items, PlaywrightJsonContext.Default.ListOfObject);
        }

        throw new PlaywrightException(
            $"Type '{type.FullName}' is not registered for AOT-safe serialization. " +
            "Use primitives, JsonElement, Dictionary<string, object?>, arrays, or register your type in PlaywrightJsonContext.");
    }
}
