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

public class PageEmulateMediaOptions
{
    public PageEmulateMediaOptions() { }

    public PageEmulateMediaOptions(PageEmulateMediaOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        ColorScheme = clone.ColorScheme;
        ForcedColors = clone.ForcedColors;
        Media = clone.Media;
        ReducedMotion = clone.ReducedMotion;
    }

    /// <summary>
    /// <para>
    /// Emulates <c>'prefers-colors-scheme'</c> media feature, supported values are <c>'light'</c>,
    /// <c>'dark'</c>, <c>'no-preference'</c>. Passing <c>'Null'</c> disables color scheme
    /// emulation.
    /// </para>
    /// </summary>
    [JsonPropertyName("colorScheme")]
    public ColorScheme? ColorScheme { get; set; }

    [JsonPropertyName("forcedColors")]
    public ForcedColors? ForcedColors { get; set; }

    /// <summary>
    /// <para>
    /// Changes the CSS media type of the page. The only allowed values are <c>'Screen'</c>,
    /// <c>'Print'</c> and <c>'Null'</c>. Passing <c>'Null'</c> disables CSS media emulation.
    /// </para>
    /// </summary>
    [JsonPropertyName("media")]
    public Media? Media { get; set; }

    /// <summary>
    /// <para>
    /// Emulates <c>'prefers-reduced-motion'</c> media feature, supported values are <c>'reduce'</c>,
    /// <c>'no-preference'</c>. Passing <c>null</c> disables reduced motion emulation.
    /// </para>
    /// </summary>
    [JsonPropertyName("reducedMotion")]
    public ReducedMotion? ReducedMotion { get; set; }
}

#nullable disable
