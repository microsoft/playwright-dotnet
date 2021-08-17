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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Helpers
{
    /// <summary>
    /// JSON extensions.
    /// </summary>
    internal static class JsonExtensions
    {
        static JsonExtensions() => DefaultJsonSerializerOptions = GetNewDefaultSerializerOptions();

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
        public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null)
            => JsonSerializer.Deserialize<T>(element.GetRawText(), options ?? DefaultJsonSerializerOptions);

        /// <summary>
        /// Convert a <see cref="JsonElement"/> to an object.
        /// </summary>
        /// <param name="element">Element to convert.</param>
        /// <param name="type">Type to convert the <see cref="JsonElement"/> to.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>Converted value.</returns>
        public static object ToObject(this JsonElement element, Type type, JsonSerializerOptions options = null)
            => JsonSerializer.Deserialize(element.GetRawText(), type, options ?? DefaultJsonSerializerOptions);

        /// <summary>
        /// Serialize an object.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="value">Object to serialize.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>Serialized object.</returns>
        public static string ToJson<T>(this T value, JsonSerializerOptions options = null)
            => JsonSerializer.Serialize(value, options ?? DefaultJsonSerializerOptions);

        /// <summary>
        /// Convert a <see cref="JsonDocument"/> to an object.
        /// </summary>
        /// <typeparam name="T">Type to convert the <see cref="JsonElement"/> to.</typeparam>
        /// <param name="document">Document to convert.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>Converted value.</returns>
        public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions options = null)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return document.RootElement.ToObject<T>(options ?? DefaultJsonSerializerOptions);
        }

        internal static JsonSerializerOptions GetNewDefaultSerializerOptions()
            => new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                Converters =
                {
                    new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase),
                },
            };

#nullable enable
        internal static string? ToOptionalString(this JsonElement? element, string name)
        {
            if (!element.HasValue)
            {
                return null;
            }

#pragma warning disable IDE0018 // Inline variable declaration will cause issues due to Roslyn design: https://github.com/dotnet/roslyn/issues/54711
            System.Text.Json.JsonElement retElement;
#pragma warning restore IDE0018 // Inline variable declaration
            if (element.Value.TryGetProperty(name, out retElement))
            {
                return retElement.ToString();
            }

            return null;
        }
#nullable disable
    }
}
