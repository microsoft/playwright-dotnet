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
using System.Text.RegularExpressions;

namespace Microsoft.Playwright;

public class TracingStartHarOptions
{
    public TracingStartHarOptions() { }

    public TracingStartHarOptions(TracingStartHarOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Content = clone.Content;
        Mode = clone.Mode;
        UrlFilter = clone.UrlFilter;
        UrlFilterRegex = clone.UrlFilterRegex;
        UrlFilterString = clone.UrlFilterString;
    }

    /// <summary>
    /// <para>
    /// Optional setting to control resource content management. If <c>omit</c> is specified,
    /// content is not persisted. If <c>attach</c> is specified, resources are persisted
    /// as separate files or entries in the ZIP archive. If <c>embed</c> is specified, content
    /// is stored inline the HAR file as per HAR specification. Defaults to <c>attach</c>
    /// for <c>.zip</c> output files and to <c>embed</c> for all other file extensions.
    /// </para>
    /// </summary>
    [JsonPropertyName("content")]
    public HarContentPolicy? Content { get; set; }

    /// <summary>
    /// <para>
    /// When set to <c>minimal</c>, only record information necessary for routing from HAR.
    /// This omits sizes, timing, page, cookies, security and other types of HAR information
    /// that are not used when replaying from HAR. Defaults to <c>full</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("mode")]
    public HarMode? Mode { get; set; }

    /// <summary>
    /// <para>
    /// A glob or regex pattern to filter requests that are stored in the HAR. Defaults
    /// to none.
    /// </para>
    /// </summary>
    [JsonPropertyName("urlFilter")]
    public string? UrlFilter { get; set; }

    /// <summary>
    /// <para>
    /// A glob or regex pattern to filter requests that are stored in the HAR. Defaults
    /// to none.
    /// </para>
    /// </summary>
    [JsonPropertyName("urlFilterRegex")]
    public Regex? UrlFilterRegex { get; set; }

    /// <summary>
    /// <para>
    /// A glob or regex pattern to filter requests that are stored in the HAR. Defaults
    /// to none.
    /// </para>
    /// </summary>
    [JsonPropertyName("urlFilterString")]
    public string? UrlFilterString { get; set; }
}
