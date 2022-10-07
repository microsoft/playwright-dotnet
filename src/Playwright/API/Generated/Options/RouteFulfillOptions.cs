/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Playwright;

public class RouteFulfillOptions
{
    public RouteFulfillOptions() { }

    public RouteFulfillOptions(RouteFulfillOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Body = clone.Body;
        BodyBytes = clone.BodyBytes;
        ContentType = clone.ContentType;
        Headers = clone.Headers;
        Path = clone.Path;
        Response = clone.Response;
        Status = clone.Status;
    }

    /// <summary><para>Optional response body as text.</para></summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary><para>Optional response body as raw bytes.</para></summary>
    [JsonPropertyName("bodyBytes")]
    public byte[]? BodyBytes { get; set; }

    /// <summary><para>If set, equals to setting <c>Content-Type</c> response header.</para></summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary><para>Response headers. Header values will be converted to a string.</para></summary>
    [JsonPropertyName("headers")]
    public IEnumerable<KeyValuePair<string, string>>? Headers { get; set; }

    /// <summary>
    /// <para>
    /// File path to respond with. The content type will be inferred from file extension.
    /// If <c>path</c> is a relative path, then it is resolved relative to the current working
    /// directory.
    /// </para>
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// <para>
    /// <see cref="IAPIResponse"/> to fulfill route's request with. Individual fields of
    /// the response (such as headers) can be overridden using fulfill options.
    /// </para>
    /// </summary>
    [JsonPropertyName("response")]
    public IAPIResponse? Response { get; set; }

    /// <summary><para>Response status code, defaults to <c>200</c>.</para></summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }
}

#nullable disable
