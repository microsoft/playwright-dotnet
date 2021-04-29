using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport.Converters
{
    /// <summary>
    /// We shouldn't need this class, having <see cref="ChannelOwnerToGuidConverter"/>.
    /// But we have some issues with .NET 5 https://github.com/dotnet/runtime/issues/45833.
    /// </summary>
    internal class ElementHandleToGuidConverter : JsonConverter<ElementHandle>
    {
        private readonly ChannelOwnerToGuidConverter _channelConverter;

        public ElementHandleToGuidConverter(Connection connection)
        {
            _channelConverter = new ChannelOwnerToGuidConverter(connection);
        }

        public override bool CanConvert(Type type) => type == typeof(ElementHandle);

        public override ElementHandle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => _channelConverter.Read(ref reader, typeToConvert, options) as ElementHandle;

        public override void Write(Utf8JsonWriter writer, ElementHandle value, JsonSerializerOptions options)
            => _channelConverter.Write(writer, value, options);
    }
}
