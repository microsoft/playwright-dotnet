using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlaywrightSharp.Transport.Channels
{
    internal class ChannelOwnerToGuidConverter : JsonConverter<IChannelOwner>
    {
        private readonly Playwright _playwright;

        public ChannelOwnerToGuidConverter(Playwright playwright)
        {
            _playwright = playwright;
        }

        public override bool CanConvert(Type type) => typeof(IChannelOwner).IsAssignableFrom(type);

        public override IChannelOwner Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            string guid = document.RootElement.GetProperty("guid").ToString();
            return _playwright.GetObject(guid);
        }

        public override void Write(Utf8JsonWriter writer, IChannelOwner value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Channel.Guid);
    }
}
