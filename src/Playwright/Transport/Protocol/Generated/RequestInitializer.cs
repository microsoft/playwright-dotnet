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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Transport.Protocol;

internal class RequestInitializer
{
    [JsonPropertyName("frame")]
    public Core.Frame Frame { get; set; } = null!;

    [JsonPropertyName("serviceWorker")]
    public Core.Worker ServiceWorker { get; set; } = null!;

    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = null!;

    [JsonPropertyName("method")]
    public string Method { get; set; } = null!;

    [JsonPropertyName("postData")]
    public byte[] PostData { get; set; } = null!;

    [JsonPropertyName("headers")]
    public List<NameValue> Headers { get; set; } = null!;

    [JsonPropertyName("isNavigationRequest")]
    public bool IsNavigationRequest { get; set; }

    [JsonPropertyName("redirectedFrom")]
    public Core.Request RedirectedFrom { get; set; } = null!;
}
