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
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Playwright;

public class LocatorScreenshotOptions
{
    public LocatorScreenshotOptions() { }

    public LocatorScreenshotOptions(LocatorScreenshotOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Animations = clone.Animations;
        Caret = clone.Caret;
        Mask = clone.Mask;
        OmitBackground = clone.OmitBackground;
        Path = clone.Path;
        Quality = clone.Quality;
        Scale = clone.Scale;
        Timeout = clone.Timeout;
        Type = clone.Type;
    }

    /// <summary>
    /// <para>
    /// When set to <c>"disabled"</c>, stops CSS animations, CSS transitions and Web Animations.
    /// Animations get different treatment depending on their duration:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// finite animations are fast-forwarded to completion, so they'll fire <c>transitionend</c>
    /// event.
    /// </description></item>
    /// <item><description>
    /// infinite animations are canceled to initial state, and then played over after the
    /// screenshot.
    /// </description></item>
    /// </list>
    /// <para>Defaults to <c>"allow"</c> that leaves animations untouched.</para>
    /// </summary>
    [JsonPropertyName("animations")]
    public ScreenshotAnimations? Animations { get; set; }

    /// <summary>
    /// <para>
    /// When set to <c>"hide"</c>, screenshot will hide text caret. When set to <c>"initial"</c>,
    /// text caret behavior will not be changed.  Defaults to <c>"hide"</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("caret")]
    public ScreenshotCaret? Caret { get; set; }

    /// <summary>
    /// <para>
    /// Specify locators that should be masked when the screenshot is taken. Masked elements
    /// will be overlaid with a pink box <c>#FF00FF</c> that completely covers its bounding
    /// box.
    /// </para>
    /// </summary>
    [JsonPropertyName("mask")]
    public IEnumerable<ILocator>? Mask { get; set; }

    /// <summary>
    /// <para>
    /// Hides default white background and allows capturing screenshots with transparency.
    /// Not applicable to <c>jpeg</c> images. Defaults to <c>false</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("omitBackground")]
    public bool? OmitBackground { get; set; }

    /// <summary>
    /// <para>
    /// The file path to save the image to. The screenshot type will be inferred from file
    /// extension. If <paramref name="path"/> is a relative path, then it is resolved relative
    /// to the current working directory. If no path is provided, the image won't be saved
    /// to the disk.
    /// </para>
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary><para>The quality of the image, between 0-100. Not applicable to <c>png</c> images.</para></summary>
    [JsonPropertyName("quality")]
    public int? Quality { get; set; }

    /// <summary>
    /// <para>
    /// When set to <c>"css"</c>, screenshot will have a single pixel per each css pixel
    /// on the page. For high-dpi devices, this will keep screenshots small. Using <c>"device"</c>
    /// option will produce a single pixel per each device pixel, so screenhots of high-dpi
    /// devices will be twice as large or even larger.
    /// </para>
    /// <para>Defaults to <c>"device"</c>.</para>
    /// </summary>
    [JsonPropertyName("scale")]
    public ScreenshotScale? Scale { get; set; }

    /// <summary>
    /// <para>
    /// Maximum time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.
    /// The default value can be changed by using the <see cref="IBrowserContext.SetDefaultTimeout"/>
    /// or <see cref="IPage.SetDefaultTimeout"/> methods.
    /// </para>
    /// </summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }

    /// <summary><para>Specify screenshot type, defaults to <c>png</c>.</para></summary>
    [JsonPropertyName("type")]
    public ScreenshotType? Type { get; set; }
}

#nullable disable
