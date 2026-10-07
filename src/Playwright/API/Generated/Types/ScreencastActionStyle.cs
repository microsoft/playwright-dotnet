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

public partial class ScreencastActionStyle
{
    /// <summary>
    /// <para>
    /// CSS declarations for the marker at the action point. The marker is positioned at
    /// the action point, has zero size and is centered on the point, so its size and look
    /// come from this style. Not shown when omitted.
    /// </para>
    /// </summary>
    [JsonPropertyName("point")]
    public string? Point { get; set; }

    /// <summary>
    /// <para>
    /// CSS declarations for the box that covers the target element. The box is positioned
    /// and sized to the element bounds. Not shown when omitted.
    /// </para>
    /// </summary>
    [JsonPropertyName("highlight")]
    public string? Highlight { get; set; }

    /// <summary>
    /// <para>
    /// CSS declarations for the action title, for example <c>'font-size: 32px; background:
    /// #333'</c>. The title is placed according to <see cref="IScreencast.ShowActionsAsync"/>.
    /// </para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
