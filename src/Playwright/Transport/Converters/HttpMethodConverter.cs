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
