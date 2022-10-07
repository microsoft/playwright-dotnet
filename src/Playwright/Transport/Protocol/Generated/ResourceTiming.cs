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

using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Transport.Protocol;

internal class ResourceTiming
{
    [JsonPropertyName("startTime")]
    public int StartTime { get; set; }

    [JsonPropertyName("domainLookupStart")]
    public int DomainLookupStart { get; set; }

    [JsonPropertyName("domainLookupEnd")]
    public int DomainLookupEnd { get; set; }

    [JsonPropertyName("connectStart")]
    public int ConnectStart { get; set; }

    [JsonPropertyName("secureConnectionStart")]
    public int SecureConnectionStart { get; set; }

    [JsonPropertyName("connectEnd")]
    public int ConnectEnd { get; set; }

    [JsonPropertyName("requestStart")]
    public int RequestStart { get; set; }

    [JsonPropertyName("responseStart")]
    public int ResponseStart { get; set; }
}
