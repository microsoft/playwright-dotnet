using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport.Converters
{
    internal class ChannelOwnerToGuidConverter : JsonConverter<IChannelOwner>
    {
        private readonly Connection _connection;

        public ChannelOwnerToGuidConverter(Connection connection)
        {
            _connection = connection;
        }

        public override bool CanConvert(Type type)
            => typeof(IChannelOwner).IsAssignableFrom(type) && type != typeof(ElementHandle);

        public override IChannelOwner Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            string guid = document.RootElement.GetProperty("guid").ToString();
            return _connection.GetObject(guid);
        }

        public override void Write(Utf8JsonWriter writer, IChannelOwner value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("guid", value.Channel.Guid);
            writer.WriteEndObject();
        }
    }
}
