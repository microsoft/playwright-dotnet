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
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport.Converters;

internal class ChannelOwnerListToGuidListConverter<T>
    : JsonConverter<IEnumerable<T>>
    where T : class, IChannelOwner
{
    private readonly Connection _connection;

    public ChannelOwnerListToGuidListConverter(Connection connection)
    {
        _connection = connection;
    }

    public override bool CanConvert(Type type)
        => type.IsArray && type.GetElementType() == typeof(T);

    public override IEnumerable<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (T item in value)
        {
            writer.WriteStartObject();
            writer.WriteString("guid", item.Channel.Guid);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}
