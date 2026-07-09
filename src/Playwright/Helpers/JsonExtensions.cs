/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Converters;

namespace Microsoft.Playwright.Helpers;

/// <summary>
/// JSON extensions.
/// </summary>
internal static class JsonExtensions
{
    static JsonExtensions() => DefaultJsonSerializerOptions = GetNewDefaultSerializerOptions(false);

    /// <summary>
    /// Base serialization options used by Microsoft.Playwright.
    /// </summary>
    public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }

    public static T ToObject<T>(this JsonElement element, JsonSerializerOptions? options = null)
        => (T)element.ToObject(typeof(T), options);

    public static object ToObject(this JsonElement element, Type type, JsonSerializerOptions? options = null)
    {
        JsonTypeInfo? ResolveTypeInfo()
        {
            if (options != null)
            {
                return options.GetTypeInfo(type);
            }
            return PlaywrightJsonContext.Default.GetTypeInfo(type);
        }

        if (typeof(ChannelOwner).IsAssignableFrom(type))
        {
            if (options != null)
            {
                foreach (var converter in options.Converters)
                {
                    if (converter is ChannelOwnerToGuidConverter coConv && coConv.CanConvert(type))
                    {
                        var bytes = Encoding.UTF8.GetBytes(element.GetRawText());
                        var reader = new Utf8JsonReader(bytes);
                        return coConv.Read(ref reader, type, options)!;
                    }
                }
            }
            throw new InvalidOperationException($"Cannot deserialize ChannelOwner '{type.Name}': ChannelOwnerToGuidConverter not found in options.");
        }

        var typeInfo = ResolveTypeInfo();
        if (typeInfo != null)
        {
            return JsonSerializer.Deserialize(element.GetRawText(), typeInfo)!;
        }

        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        if (effectiveType.IsEnum)
        {
            var str = element.GetString() ?? throw new JsonException("Expected string value for enum.");
            return AotEnumMemberConverter.FromWireString(effectiveType, str);
        }

        throw new InvalidOperationException($"Type '{type}' is not registered in PlaywrightJsonContext. Add [JsonSerializable(typeof({type.Name}))] to enable AOT-safe deserialization.");
    }

    public static string ToJson<T>(this T value, JsonSerializerOptions? options = null)
    {
        var typeInfo = options?.GetTypeInfo(typeof(T)) ?? PlaywrightJsonContext.Default.GetTypeInfo(typeof(T));
        if (typeInfo == null)
        {
            throw new InvalidOperationException($"Type '{typeof(T)}' is not registered in PlaywrightJsonContext. Add [JsonSerializable(typeof({typeof(T).Name}))] to enable AOT-safe serialization.");
        }
        return JsonSerializer.Serialize(value, typeInfo);
    }

    /// <summary>
    /// Convert a <see cref="JsonDocument"/> to an object.
    /// </summary>
    /// <typeparam name="T">Type to convert the <see cref="JsonElement"/> to.</typeparam>
    /// <param name="document">Document to convert.</param>
    /// <param name="options">Serialization options.</param>
    /// <returns>Converted value.</returns>
    public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions? options = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return document.RootElement.ToObject<T>(options ?? DefaultJsonSerializerOptions);
    }

    internal static JsonSerializerOptions GetNewDefaultSerializerOptions(bool keepNulls)
    {
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        options.Converters.Add(new AotEnumMemberConverter());
        if (!keepNulls)
        {
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        }
        return options;
    }
}
