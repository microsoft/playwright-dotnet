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

    /// <summary>
    /// Convert a <see cref="JsonElement"/> to an object.
    /// </summary>
    /// <typeparam name="T">Type to convert the <see cref="JsonElement"/> to.</typeparam>
    /// <param name="element">Element to convert.</param>
    /// <param name="options">Serialization options.</param>
    /// <returns>Converted value.</returns>
    public static T ToObject<T>(this JsonElement element, JsonSerializerOptions? options = null)
    {
        if (options != null)
        {
            // ChannelOwner types: use converter directly to avoid STJ resolution issues.
            if (typeof(ChannelOwner).IsAssignableFrom(typeof(T)))
            {
                foreach (var converter in options.Converters)
                {
                    if (converter is ChannelOwnerToGuidConverter coConv && coConv.CanConvert(typeof(T)))
                    {
                        var bytes = Encoding.UTF8.GetBytes(element.GetRawText());
                        var reader = new Utf8JsonReader(bytes);
                        return (T)(object)coConv.Read(ref reader, typeof(T), options)!;
                    }
                }
                throw new InvalidOperationException($"Cannot deserialize ChannelOwner '{typeof(T).Name}': ChannelOwnerToGuidConverter not found in options.");
            }

            // Resolve JsonTypeInfo via options resolver chain.
            // On .NET 9+, GetTypeInfo(Type) is AOT-safe when a TypeInfoResolver is set.
#if NET9_0_OR_GREATER
            var typeInfo = options.GetTypeInfo(typeof(T));
            if (typeInfo != null)
            {
                return (T)JsonSerializer.Deserialize(element.GetRawText(), typeInfo)!;
            }
#else
            // .NET 8: Option-based deserialization for types registered in context.
            var contextTypeInfo = PlaywrightJsonContext.Default.GetTypeInfo(typeof(T));
            if (contextTypeInfo != null)
            {
                return (T)JsonSerializer.Deserialize(element.GetRawText(), contextTypeInfo)!;
            }
#endif

            // Enum types: handled via AotEnumMemberConverter.
            var actualType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (actualType.IsEnum)
            {
                var str = element.GetString() ?? throw new JsonException("Expected string value for enum.");
                return (T)AotEnumMemberConverter.FromWireString(actualType, str);
            }

#if !NET9_0_OR_GREATER
            // .NET 8 fallback for types not in context (protocol initializers with ChannelOwner properties).
#pragma warning disable IL2026, IL3050 // Uses converters only; no reflection at runtime.
            return JsonSerializer.Deserialize<T>(element.GetRawText(), options)!;
#pragma warning restore IL2026, IL3050
#endif

            throw new InvalidOperationException($"Type '{typeof(T)}' is not registered in PlaywrightJsonContext. Add [JsonSerializable(typeof({typeof(T).Name}))] to enable AOT-safe deserialization.");
        }

        // No explicit options: try source-gen context for AOT-safe path.
        var typeInfoNoOpts = PlaywrightJsonContext.Default.GetTypeInfo(typeof(T));
        if (typeInfoNoOpts != null)
        {
            return (T)JsonSerializer.Deserialize(element.GetRawText(), typeInfoNoOpts)!;
        }

        // Enum types handled via AotEnumMemberConverter.
        if (typeof(T).IsEnum)
        {
            var str = element.GetString() ?? throw new JsonException("Expected string value for enum.");
            return (T)AotEnumMemberConverter.FromWireString(typeof(T), str);
        }

        throw new InvalidOperationException($"Type '{typeof(T)}' is not registered in PlaywrightJsonContext. Add [JsonSerializable(typeof({typeof(T).Name}))] to enable AOT-safe deserialization.");
    }

    public static object ToObject(this JsonElement element, Type type, JsonSerializerOptions? options = null)
    {
        if (options != null)
        {
            if (typeof(ChannelOwner).IsAssignableFrom(type))
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
                throw new InvalidOperationException($"Cannot deserialize ChannelOwner '{type.Name}': ChannelOwnerToGuidConverter not found in options.");
            }

#if NET9_0_OR_GREATER
            var typeInfo = options.GetTypeInfo(type);
            if (typeInfo != null)
            {
                return JsonSerializer.Deserialize(element.GetRawText(), typeInfo)!;
            }
#else
            var contextTypeInfo = PlaywrightJsonContext.Default.GetTypeInfo(type);
            if (contextTypeInfo != null)
            {
                return JsonSerializer.Deserialize(element.GetRawText(), contextTypeInfo)!;
            }
#endif

            var nuType = Nullable.GetUnderlyingType(type) ?? type;
            if (nuType.IsEnum)
            {
                var str = element.GetString() ?? throw new JsonException("Expected string value for enum.");
                return AotEnumMemberConverter.FromWireString(nuType, str);
            }

#if !NET9_0_OR_GREATER
#pragma warning disable IL2026, IL3050
            return JsonSerializer.Deserialize(element.GetRawText(), type, options)!;
#pragma warning restore IL2026, IL3050
#endif

            throw new InvalidOperationException($"Type '{type}' is not registered in PlaywrightJsonContext. Add [JsonSerializable(typeof({type.Name}))] to enable AOT-safe deserialization.");
        }

        var typeInfoNoOpts = PlaywrightJsonContext.Default.GetTypeInfo(type);
        if (typeInfoNoOpts != null)
        {
            return JsonSerializer.Deserialize(element.GetRawText(), typeInfoNoOpts)!;
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
        var info = PlaywrightJsonContext.Default.GetTypeInfo(typeof(T));
        if (info == null)
        {
            throw new InvalidOperationException($"Type '{typeof(T)}' is not registered in PlaywrightJsonContext. Add [JsonSerializable(typeof({typeof(T).Name}))] to enable AOT-safe serialization.");
        }
        return JsonSerializer.Serialize(value, (JsonTypeInfo<T>)info);
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
