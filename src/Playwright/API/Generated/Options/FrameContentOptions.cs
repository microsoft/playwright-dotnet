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

namespace Microsoft.Playwright;

public class FrameContentOptions
{
    public FrameContentOptions() { }

    public FrameContentOptions(FrameContentOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        IncludeShadow = clone.IncludeShadow;
    }

    /// <summary>
    /// <para>
    /// When true, contents of open shadow roots are included as <a href="https://developer.mozilla.org/en-US/docs/Web/API/Web_components/Using_shadow_DOM#declaratively_with_html">declarative
    /// shadow DOM</a>, i.e. <c>&lt;template shadowrootmode="open"&gt;</c> elements nested
    /// inside their host elements. Closed shadow roots are never included. Defaults to
    /// <c>false</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("includeShadow")]
    public bool? IncludeShadow { get; set; }
}
