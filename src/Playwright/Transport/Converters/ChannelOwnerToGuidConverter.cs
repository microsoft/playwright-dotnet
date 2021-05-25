using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport.Converters
{
    internal class ChannelOwnerToGuidConverter<T>
        : JsonConverter<T>
        where T : class, IChannelOwner
    {
        private readonly Connection _connection;

        public ChannelOwnerToGuidConverter(Connection connection)
        {
            _connection = connection;
        }

        public override bool CanConvert(Type type) => typeof(T).IsAssignableFrom(type);

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            string guid = document.RootElement.GetProperty("guid").ToString();
            return _connection.GetObject(guid) as T;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("guid", value.Channel.Guid);
            writer.WriteEndObject();
        }
    }
}
