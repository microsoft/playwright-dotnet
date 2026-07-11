/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Transport.Converters;

internal class ChannelOwnerListToGuidListConverter
    : JsonConverter<IEnumerable<ChannelOwner>>
{
    private readonly Connection _connection;

    public ChannelOwnerListToGuidListConverter(Connection connection)
    {
        _connection = connection;
    }

    public override IEnumerable<ChannelOwner> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.None)
        {
            reader.Read();
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null!;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected start of array or null, got {reader.TokenType}.");
        }

        var results = new List<ChannelOwner>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected start of object.");
            }

            string? guid = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    if (string.Equals(propertyName, "guid", StringComparison.OrdinalIgnoreCase))
                    {
                        reader.Read();
                        guid = reader.GetString();
                    }
                    else
                    {
                        reader.Read();
                        reader.Skip();
                    }
                }
            }

            if (guid != null)
            {
                var channelOwner = _connection.GetObject(guid);
                if (channelOwner != null)
                {
                    results.Add(channelOwner);
                }
                else
                {
                    throw new JsonException($"ChannelOwner with guid '{guid}' not found in connection.");
                }
            }
        }

        return results;
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<ChannelOwner> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStartObject();
            writer.WriteString("guid", item.Guid);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}
