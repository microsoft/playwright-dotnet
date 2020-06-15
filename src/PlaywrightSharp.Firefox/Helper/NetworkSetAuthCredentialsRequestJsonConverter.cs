using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlaywrightSharp.Firefox.Protocol.Network;
using PlaywrightSharp.Firefox.Protocol.Runtime;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Firefox.Helper
{
    /// <summary>
    /// We need to be able to send null values in order to reset the username/password values.
    /// </summary>
    internal class NetworkSetAuthCredentialsRequestJsonConverter : JsonConverter<NetworkSetAuthCredentialsRequest>
    {
        public override NetworkSetAuthCredentialsRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("this type should not be de-serialized");

        public override void Write(Utf8JsonWriter writer, NetworkSetAuthCredentialsRequest value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("username");
            if (value.Username == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Username);
            }

            writer.WritePropertyName("password");
            if (value.Password == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Password);
            }

            writer.WriteEndObject();
        }
    }
}
