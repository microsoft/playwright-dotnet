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
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class HttpMethodConverter : JsonConverter<HttpMethod>
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeof(HttpMethod) == typeToConvert;

        /// <inheritdoc/>
        public override HttpMethod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                "GET" => HttpMethod.Get,
                "DELETE" => HttpMethod.Delete,
                "HEAD" => HttpMethod.Head,
                "OPTIONS" => HttpMethod.Options,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "TRACE" => HttpMethod.Trace,
#if NETSTANDARD2_1
                "PATCH" => HttpMethod.Patch,
#endif
                _ => HttpMethod.Get,
            };
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, HttpMethod value, JsonSerializerOptions options)
            => writer?.WriteStringValue(value?.Method);
    }
}
