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

public class RouteFetchOptions
{
    public RouteFetchOptions() { }

    public RouteFetchOptions(RouteFetchOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Headers = clone.Headers;
        MaxRedirects = clone.MaxRedirects;
        Method = clone.Method;
        PostData = clone.PostData;
        Timeout = clone.Timeout;
        Url = clone.Url;
    }

    /// <summary><para>If set changes the request HTTP headers. Header values will be converted to a string.</para></summary>
    [JsonPropertyName("headers")]
    public IEnumerable<KeyValuePair<string, string>>? Headers { get; set; }

    /// <summary>
    /// <para>
    /// Maximum number of request redirects that will be followed automatically. An error
    /// will be thrown if the number is exceeded. Defaults to <c>20</c>. Pass <c>0</c> to
    /// not follow redirects.
    /// </para>
    /// </summary>
    [JsonPropertyName("maxRedirects")]
    public int? MaxRedirects { get; set; }

    /// <summary><para>If set changes the request method (e.g. GET or POST).</para></summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary><para>If set changes the post data of request.</para></summary>
    [JsonPropertyName("postData")]
    public byte[]? PostData { get; set; }

    /// <summary>
    /// <para>
    /// Request timeout in milliseconds. Defaults to <c>30000</c> (30 seconds). Pass <c>0</c>
    /// to disable timeout.
    /// </para>
    /// </summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }

    /// <summary><para>If set changes the request URL. New URL must have same protocol as original one.</para></summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

#nullable disable
