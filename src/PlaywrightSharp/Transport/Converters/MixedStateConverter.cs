using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Transport.Converters
{
    /// <summary>
    /// JSON converter for Mixed state.
    /// </summary>
    public class MixedStateConverter : JsonConverter<MixedState>
    {
        /// <inheritdoc/>
        public override MixedState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string val = reader.GetString();
            return val.ToLower() switch
            {
                "true" => MixedState.On,
                "checked" => MixedState.On,
                "enabled" => MixedState.On,
                "pressed" => MixedState.On,
                "false" => MixedState.Off,
                "unchecked" => MixedState.Off,
                "released" => MixedState.Off,
                "mixed" => MixedState.Mixed,
                _ => throw new NotImplementedException($"Don't know how to handle {val}"),
            };
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, MixedState value, JsonSerializerOptions options)
            => writer?.WriteStringValue(value.ToString());
    }
}
