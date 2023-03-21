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

#nullable enable

namespace Microsoft.Playwright;

public class BrowserContextRouteFromHAROptions
{
    public BrowserContextRouteFromHAROptions() { }

    public BrowserContextRouteFromHAROptions(BrowserContextRouteFromHAROptions clone)
    {
        if (clone == null)
        {
            return;
        }

        NotFound = clone.NotFound;
        Update = clone.Update;
        UpdateContent = clone.UpdateContent;
        UpdateMode = clone.UpdateMode;
        Url = clone.Url;
        UrlRegex = clone.UrlRegex;
        UrlString = clone.UrlString;
    }

    /// <summary>
    /// <list type="bullet">
    /// <item><description>If set to 'abort' any request not found in the HAR file will be aborted.</description></item>
    /// <item><description>If set to 'fallback' falls through to the next route handler in the handler chain.</description></item>
    /// </list>
    /// <para>Defaults to abort.</para>
    /// </summary>
    [JsonPropertyName("notFound")]
    public HarNotFound? NotFound { get; set; }

    /// <summary>
    /// <para>
    /// If specified, updates the given HAR with the actual network information instead
    /// of serving from file. The file is written to disk when <see cref="IBrowserContext.CloseAsync"/>
    /// is called.
    /// </para>
    /// </summary>
    [JsonPropertyName("update")]
    public bool? Update { get; set; }

    /// <summary>
    /// <para>
    /// Optional setting to control resource content management. If <c>attach</c> is specified,
    /// resources are persisted as separate files or entries in the ZIP archive. If <c>embed</c>
    /// is specified, content is stored inline the HAR file.
    /// </para>
    /// </summary>
    [JsonPropertyName("updateContent")]
    public RouteFromHarUpdateContentPolicy? UpdateContent { get; set; }

    /// <summary>
    /// <para>
    /// When set to <c>minimal</c>, only record information necessary for routing from HAR.
    /// This omits sizes, timing, page, cookies, security and other types of HAR information
    /// that are not used when replaying from HAR. Defaults to <c>minimal</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("updateMode")]
    public HarMode? UpdateMode { get; set; }

    /// <summary>
    /// <para>
    /// A glob pattern, regular expression or predicate to match the request URL. Only requests
    /// with URL matching the pattern will be served from the HAR file. If not specified,
    /// all requests are served from the HAR file.
    /// </para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>
    /// A glob pattern, regular expression or predicate to match the request URL. Only requests
    /// with URL matching the pattern will be served from the HAR file. If not specified,
    /// all requests are served from the HAR file.
    /// </para>
    /// </summary>
    [JsonPropertyName("urlRegex")]
    public Regex? UrlRegex { get; set; }

    /// <summary>
    /// <para>
    /// A glob pattern, regular expression or predicate to match the request URL. Only requests
    /// with URL matching the pattern will be served from the HAR file. If not specified,
    /// all requests are served from the HAR file.
    /// </para>
    /// </summary>
    [JsonPropertyName("urlString")]
    public string? UrlString { get; set; }
}

#nullable disable
