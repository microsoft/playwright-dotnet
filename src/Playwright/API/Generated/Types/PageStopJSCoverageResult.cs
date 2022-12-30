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

internal partial class PageStopJSCoverageResults
{
    [Required]
    [JsonPropertyName("entries")]
    public List<PageStopJSCoverageResult> Entries { get; set; } = new List<PageStopJSCoverageResult>();
}

public partial class PageStopJSCoverageResult
{
    /// <summary><para>JavaScript script name or url</para></summary>
    [Required]
    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    /// <summary><para>JavaScript script id</para></summary>
    [Required]
    [JsonPropertyName("scriptId")]
    public string ScriptId { get; set; } = default!;

    /// <summary><para>JavaScript Source</para></summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary><para>Functions contained in the script that has coverage data</para></summary>
    [Required]
    [JsonPropertyName("functions")]
    public List<PageStopJSCoverageResultFunction> Functions { get; set; } = default!;
}
public partial class PageStopJSCoverageResultFunction
{
    /// <summary><para>JavaScript function name</para></summary>
    [Required]
    [JsonPropertyName("functionName")]
    public string FunctionName { get; set; } = default!;

    /// <summary><para>Whether coverage data for this function has block granularity</para></summary>
    [Required]
    [JsonPropertyName("isBlockCoverage")]
    public bool IsBlockCoverage { get; set; } = default!;

    /// <summary><para>Source ranges inside the function with coverage data</para></summary>
    [Required]
    [JsonPropertyName("ranges")]
    public List<PageStopJSCoverageResultRange> Ranges { get; set; } = default!;
}

public partial class PageStopJSCoverageResultRange
{
    /// <summary><para>JavaScript script source offset for the range start</para></summary>
    [Required]
    [JsonPropertyName("startOffset")]
    public int Start { get; set; } = default!;

    /// <summary><para>JavaScript script source offset for the range end</para></summary>
    [Required]
    [JsonPropertyName("endOffset")]
    public int End { get; set; } = default!;

    /// <summary><para>Collected execution count of the source range</para></summary>
    [Required]
    [JsonPropertyName("count")]
    public int Count { get; set; } = default!;
}

#nullable disable
