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

        public override IChannelOwner Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IChannelOwner value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Channel.Guid);
    }
}