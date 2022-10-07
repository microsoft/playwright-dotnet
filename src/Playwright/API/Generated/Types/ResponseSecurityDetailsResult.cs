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

using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Playwright;

public partial class ResponseSecurityDetailsResult
{
    /// <summary>
    /// <para>
    /// Common Name component of the Issuer field. from the certificate. This should only
    /// be used for informational purposes. Optional.
    /// </para>
    /// </summary>
    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }

    /// <summary><para>The specific TLS protocol used. (e.g. <c>TLS 1.3</c>). Optional.</para></summary>
    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    /// <summary>
    /// <para>
    /// Common Name component of the Subject field from the certificate. This should only
    /// be used for informational purposes. Optional.
    /// </para>
    /// </summary>
    [JsonPropertyName("subjectName")]
    public string? SubjectName { get; set; }

    /// <summary><para>Unix timestamp (in seconds) specifying when this cert becomes valid. Optional.</para></summary>
    [JsonPropertyName("validFrom")]
    public float? ValidFrom { get; set; }

    /// <summary><para>Unix timestamp (in seconds) specifying when this cert becomes invalid. Optional.</para></summary>
    [JsonPropertyName("validTo")]
    public float? ValidTo { get; set; }
}

#nullable disable
