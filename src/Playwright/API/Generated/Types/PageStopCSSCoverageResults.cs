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
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Playwright;

internal partial class PageStopCSSCoverageResults
{
    [Required]
    [JsonPropertyName("entries")]
    public List<PageStopCSSCoverageResult> Entries { get; set; } = new List<PageStopCSSCoverageResult>();
}

public partial class PageStopCSSCoverageResult
{
    /// <summary><para>StyleSheet URL</para></summary>
    [Required]
    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    /// <summary><para>StyleSheet content, if available.</para></summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary><para>Source ranges inside the CSS</para></summary>
    [Required]
    [JsonPropertyName("ranges")]
    public List<PageStopCSSCoverageResultRange> Ranges { get; set; } = default!;
}

public partial class PageStopCSSCoverageResultRange
{
    /// <summary><para>A start offset in text, inclusive</para></summary>
    [Required]
    [JsonPropertyName("start")]
    public int Start { get; set; } = default!;

    /// <summary><para>An end offset in text, exclusive</para></summary>
    [Required]
    [JsonPropertyName("end")]
    public int End { get; set; } = default!;
}

#nullable disable
