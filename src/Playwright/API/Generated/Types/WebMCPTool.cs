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

namespace Microsoft.Playwright;

public partial class WebMCPTool
{
    /// <summary><para>Tool name, unique within the frame.</para></summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary><para>Tool description.</para></summary>
    [Required]
    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    /// <summary><para>JSON Schema of the tool input, when the page provides one.</para></summary>
    [JsonPropertyName("inputSchema")]
    public object? InputSchema { get; set; }

    /// <summary><para>Hints the page provides about the tool.</para></summary>
    [JsonPropertyName("annotations")]
    public WebMCPToolAnnotations? Annotations { get; set; }
}
