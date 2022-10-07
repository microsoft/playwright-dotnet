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

public class PageAddStyleTagOptions
{
    public PageAddStyleTagOptions() { }

    public PageAddStyleTagOptions(PageAddStyleTagOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Content = clone.Content;
        Path = clone.Path;
        Url = clone.Url;
    }

    /// <summary><para>Raw CSS content to be injected into frame.</para></summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>
    /// Path to the CSS file to be injected into frame. If <c>path</c> is a relative path,
    /// then it is resolved relative to the current working directory.
    /// </para>
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary><para>URL of the <c>&lt;link&gt;</c> tag.</para></summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

#nullable disable
