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
using System.Text.Json;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Whenever the page sends a request for a network resource the following sequence
/// of events are emitted by <see cref="IPage"/>:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IPage.Request"/> emitted when the request is issued by the page.</description></item>
/// <item><description>
/// <see cref="IPage.Response"/> emitted when/if the response status and headers are
/// received for the request.
/// </description></item>
/// <item><description>
/// <see cref="IPage.RequestFinished"/> emitted when the response body is downloaded
/// and the request is complete.
/// </description></item>
/// </list>
/// <para>
/// If request fails at some point, then instead of <c>'requestfinished'</c> event (and
/// possibly instead of 'response' event), the  <see cref="IPage.RequestFailed"/> event
/// is emitted.
/// </para>
/// <para>
/// If request gets a 'redirect' response, the request is successfully finished with
/// the 'requestfinished' event, and a new request is  issued to a redirected url.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// HTTP Error responses, such as 404 or 503, are still successful responses from HTTP
/// standpoint, so request will complete with <c>'requestfinished'</c> event.
/// </para>
/// </remarks>
public partial interface IRequest
{
    /// <summary>
    /// <para>
    /// An object with all the request HTTP headers associated with this request. The header
    /// names are lower-cased.
    /// </para>
    /// </summary>
    Task<Dictionary<string, string>> AllHeadersAsync();

    /// <summary>
    /// <para>
    /// The method returns <c>null</c> unless this request has failed, as reported by <c>requestfailed</c>
    /// event.
    /// </para>
    /// <para>Example of logging of all the failed requests:</para>
    /// <code>
    /// page.RequestFailed += (_, request) =&gt;<br/>
    /// {<br/>
    ///     Console.WriteLine(request.Failure);<br/>
    /// };
    /// </code>
    /// </summary>
    string? Failure { get; }

    /// <summary><para>Returns the <see cref="IFrame"/> that initiated this request.</para></summary>
    IFrame Frame { get; }

    /// <summary>
    /// <para>
    /// An object with the request HTTP headers. The header names are lower-cased. Note
    /// that this method does not return security-related headers, including cookie-related
    /// ones. You can use <see cref="IRequest.AllHeadersAsync"/> for complete list of headers
    /// that include <c>cookie</c> information.
    /// </para>
    /// </summary>
    Dictionary<string, string> Headers { get; }

    /// <summary>
    /// <para>
    /// An array with all the request HTTP headers associated with this request. Unlike
    /// <see cref="IRequest.AllHeadersAsync"/>, header names are NOT lower-cased. Headers
    /// with multiple entries, such as <c>Set-Cookie</c>, appear in the array multiple times.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Header>> HeadersArrayAsync();

    /// <summary><para>Returns the value of the header matching the name. The name is case insensitive.</para></summary>
    /// <param name="name">Name of the header.</param>
    Task<string?> HeaderValueAsync(string name);

    /// <summary><para>Whether this request is driving frame's navigation.</para></summary>
    bool IsNavigationRequest { get; }

    /// <summary><para>Request's method (GET, POST, etc.)</para></summary>
    string Method { get; }

    /// <summary><para>Request's post body, if any.</para></summary>
    string? PostData { get; }

    /// <summary><para>Request's post body in a binary form, if any.</para></summary>
    byte[]? PostDataBuffer { get; }

    /// <summary>
    /// <para>Request that was redirected by the server to this one, if any.</para>
    /// <para>
    /// When the server responds with a redirect, Playwright creates a new <see cref="IRequest"/>
    /// object. The two requests are connected by <c>redirectedFrom()</c> and <c>redirectedTo()</c>
    /// methods. When multiple server redirects has happened, it is possible to construct
    /// the whole redirect chain by repeatedly calling <c>redirectedFrom()</c>.
    /// </para>
    /// <para>For example, if the website <c>http://example.com</c> redirects to <c>https://example.com</c>:</para>
    /// <code>
    /// var response = await page.GotoAsync("http://www.microsoft.com");<br/>
    /// Console.WriteLine(response.Request.RedirectedFrom?.Url); // http://www.microsoft.com
    /// </code>
    /// <para>If the website <c>https://google.com</c> has no redirects:</para>
    /// <code>
    /// var response = await page.GotoAsync("https://www.google.com");<br/>
    /// Console.WriteLine(response.Request.RedirectedFrom?.Url); // null
    /// </code>
    /// </summary>
    IRequest? RedirectedFrom { get; }

    /// <summary>
    /// <para>New request issued by the browser if the server responded with redirect.</para>
    /// <para>This method is the opposite of <see cref="IRequest.RedirectedFrom"/>:</para>
    /// <code>Console.WriteLine(request.RedirectedFrom?.RedirectedTo == request); // True</code>
    /// </summary>
    IRequest? RedirectedTo { get; }

    /// <summary>
    /// <para>
    /// Contains the request's resource type as it was perceived by the rendering engine.
    /// ResourceType will be one of the following: <c>document</c>, <c>stylesheet</c>, <c>image</c>,
    /// <c>media</c>, <c>font</c>, <c>script</c>, <c>texttrack</c>, <c>xhr</c>, <c>fetch</c>,
    /// <c>eventsource</c>, <c>websocket</c>, <c>manifest</c>, <c>other</c>.
    /// </para>
    /// </summary>
    string ResourceType { get; }

    /// <summary>
    /// <para>
    /// Returns the matching <see cref="IResponse"/> object, or <c>null</c> if the response
    /// was not received due to error.
    /// </para>
    /// </summary>
    Task<IResponse?> ResponseAsync();

    /// <summary><para>Returns resource size information for given request.</para></summary>
    Task<RequestSizesResult> SizesAsync();

    /// <summary>
    /// <para>
    /// Returns resource timing information for given request. Most of the timing values
    /// become available upon the response, <c>responseEnd</c> becomes available when request
    /// finishes. Find more information at <a href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceResourceTiming">Resource
    /// Timing API</a>.
    /// </para>
    /// <code>
    /// var request = await page.RunAndWaitForRequestFinishedAsync(async () =&gt;<br/>
    /// {<br/>
    ///     await page.GotoAsync("https://www.microsoft.com");<br/>
    /// });<br/>
    /// Console.WriteLine(request.Timing.ResponseEnd);
    /// </code>
    /// </summary>
    RequestTimingResult Timing { get; }

    /// <summary><para>URL of the request.</para></summary>
    string Url { get; }

    /// <summary>
    /// <para>
    /// Returns parsed request's body for <c>form-urlencoded</c> and JSON as a fallback
    /// if any.
    /// </para>
    /// <para>
    /// When the response is <c>application/x-www-form-urlencoded</c> then a key/value object
    /// of the values will be returned. Otherwise it will be parsed as JSON.
    /// </para>
    /// </summary>
    JsonElement? PostDataJSON();
}

#nullable disable
