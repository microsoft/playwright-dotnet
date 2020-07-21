using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlaywrightSharp.Transport.Channels
{
    internal class HttpMethodConverter : JsonConverter<HttpMethod>
    {
        private readonly Connection _connection;

        public HttpMethodConverter(Connection connection)
        {
            _connection = connection;
        }

        public override bool CanConvert(Type type) => typeof(HttpMethod) == type;

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

        public override void Write(Utf8JsonWriter writer, HttpMethod value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Method);
    }
}
