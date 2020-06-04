using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlaywrightSharp.Firefox.Protocol.Runtime;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Firefox.Helper
{
    internal class RemoteObjectJsonConverter : JsonConverter<RemoteObject>
    {
        public override RemoteObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            RemoteObjectType? type = RemoteObjectType.Undefined;
            RemoteObjectSubtype? subtype = null;
            string objectId = null;
            RemoteObjectUnserializableValue? unserializableValue = null;
            object value = null;

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string name = reader.GetString();
                reader.Read();
                switch (name)
                {
                    case "objectId":
                        objectId = reader.GetString();
                        break;
                    case "type":
                        type = reader.GetString()?.ToEnum<RemoteObjectType>();
                        break;
                    case "subtype":
                        subtype = reader.GetString()?.ToEnum<RemoteObjectSubtype>();
                        break;
                    case "unserializableValue":
                        unserializableValue = reader.GetString()?.ToEnum<RemoteObjectUnserializableValue>();
                        break;
                    case "value":
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.Null:
                                type = null;
                                subtype = RemoteObjectSubtype.Null;
                                break;
                            default:
                                type = null;
                                value = JsonDocument.ParseValue(ref reader).RootElement;
                                break;
                        }

                        break;
                }
            }

            return new RemoteObject
            {
                Type = type,
                Subtype = subtype,
                ObjectId = objectId,
                UnserializableValue = unserializableValue,
                Value = value,
            };
        }

        public override void Write(Utf8JsonWriter writer, RemoteObject value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToJson(JsonHelper.DefaultJsonSerializerOptions));
        }
    }
}
