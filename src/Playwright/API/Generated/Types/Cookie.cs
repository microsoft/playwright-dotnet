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

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Playwright;

public partial class Cookie
{
    /// <summary><para></para></summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary><para></para></summary>
    [Required]
    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;

    /// <summary><para>either url or domain / path are required. Optional.</para></summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary><para>either url or domain / path are required Optional.</para></summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    /// <summary><para>either url or domain / path are required Optional.</para></summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary><para>Unix time in seconds. Optional.</para></summary>
    [JsonPropertyName("expires")]
    public float? Expires { get; set; }

    /// <summary><para>Optional.</para></summary>
    [JsonPropertyName("httpOnly")]
    public bool? HttpOnly { get; set; }

    /// <summary><para>Optional.</para></summary>
    [JsonPropertyName("secure")]
    public bool? Secure { get; set; }

    /// <summary><para>Optional.</para></summary>
    [JsonPropertyName("sameSite")]
    public SameSiteAttribute? SameSite { get; set; }
}

#nullable disable
