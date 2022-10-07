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

using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Whenever a network route is set up with <see cref="IPage.RouteAsync"/> or <see cref="IBrowserContext.RouteAsync"/>,
/// the <c>Route</c> object allows to handle the route.
/// </para>
/// <para>Learn more about <a href="https://playwright.dev/dotnet/docs/network">networking</a>.</para>
/// </summary>
public partial interface IRoute
{
    /// <summary><para>Aborts the route's request.</para></summary>
    /// <param name="errorCode">
    /// Optional error code. Defaults to <c>failed</c>, could be one of the following:
    /// <list type="bullet">
    /// <item><description><c>'aborted'</c> - An operation was aborted (due to user action)</description></item>
    /// <item><description>
    /// <c>'accessdenied'</c> - Permission to access a resource, other than the network,
    /// was denied
    /// </description></item>
    /// <item><description>
    /// <c>'addressunreachable'</c> - The IP address is unreachable. This usually means
    /// that there is no route to the specified host or network.
    /// </description></item>
    /// <item><description><c>'blockedbyclient'</c> - The client chose to block the request.</description></item>
    /// <item><description>
    /// <c>'blockedbyresponse'</c> - The request failed because the response was delivered
    /// along with requirements which are not met ('X-Frame-Options' and 'Content-Security-Policy'
    /// ancestor checks, for instance).
    /// </description></item>
    /// <item><description>
    /// <c>'connectionaborted'</c> - A connection timed out as a result of not receiving
    /// an ACK for data sent.
    /// </description></item>
    /// <item><description><c>'connectionclosed'</c> - A connection was closed (corresponding to a TCP FIN).</description></item>
    /// <item><description><c>'connectionfailed'</c> - A connection attempt failed.</description></item>
    /// <item><description><c>'connectionrefused'</c> - A connection attempt was refused.</description></item>
    /// <item><description><c>'connectionreset'</c> - A connection was reset (corresponding to a TCP RST).</description></item>
    /// <item><description><c>'internetdisconnected'</c> - The Internet connection has been lost.</description></item>
    /// <item><description><c>'namenotresolved'</c> - The host name could not be resolved.</description></item>
    /// <item><description><c>'timedout'</c> - An operation timed out.</description></item>
    /// <item><description><c>'failed'</c> - A generic failure occurred.</description></item>
    /// </list>
    /// </param>
    Task AbortAsync(string? errorCode = default);

    /// <summary>
    /// <para>Continues route's request with optional overrides.</para>
    /// <code>
    /// await page.RouteAsync("**/*", route =&gt;<br/>
    /// {<br/>
    ///     var headers = new Dictionary&lt;string, string&gt;(route.Request.Headers) { { "foo", "bar" } };<br/>
    ///     headers.Remove("origin");<br/>
    ///     route.ContinueAsync(headers);<br/>
    /// });
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ContinueAsync(RouteContinueOptions? options = default);

    /// <summary>
    /// <para>
    /// When several routes match the given pattern, they run in the order opposite to their
    /// registration. That way the last registered route can always override all the previous
    /// ones. In the example below, request will be handled by the bottom-most handler first,
    /// then it'll fall back to the previous one and in the end will be aborted by the first
    /// registered route.
    /// </para>
    /// <code>
    /// await page.RouteAsync("**/*", route =&gt; {<br/>
    ///     // Runs last.<br/>
    ///     await route.AbortAsync();<br/>
    /// });<br/>
    /// <br/>
    /// await page.RouteAsync("**/*", route =&gt; {<br/>
    ///     // Runs second.<br/>
    ///     await route.FallbackAsync();<br/>
    /// });<br/>
    /// <br/>
    /// await page.RouteAsync("**/*", route =&gt; {<br/>
    ///     // Runs first.<br/>
    ///     await route.FallbackAsync();<br/>
    /// });
    /// </code>
    /// <para>
    /// Registering multiple routes is useful when you want separate handlers to handle
    /// different kinds of requests, for example API calls vs page resources or GET requests
    /// vs POST requests as in the example below.
    /// </para>
    /// <code>
    /// // Handle GET requests.<br/>
    /// await page.RouteAsync("**/*", route =&gt; {<br/>
    ///     if (route.Request.Method != "GET") {<br/>
    ///         await route.FallbackAsync();<br/>
    ///         return;<br/>
    ///     }<br/>
    ///     // Handling GET only.<br/>
    ///     // ...<br/>
    /// });<br/>
    /// <br/>
    /// // Handle POST requests.<br/>
    /// await page.RouteAsync("**/*", route =&gt; {<br/>
    ///     if (route.Request.Method != "POST") {<br/>
    ///         await route.FallbackAsync();<br/>
    ///         return;<br/>
    ///     }<br/>
    ///     // Handling POST only.<br/>
    ///     // ...<br/>
    /// });
    /// </code>
    /// <para>
    /// One can also modify request while falling back to the subsequent handler, that way
    /// intermediate route handler can modify url, method, headers and postData of the request.
    /// </para>
    /// <code>
    /// await page.RouteAsync("**/*", route =&gt;<br/>
    /// {<br/>
    ///     var headers = new Dictionary&lt;string, string&gt;(route.Request.Headers) { { "foo", "foo-value" } };<br/>
    ///     headers.Remove("bar");<br/>
    ///     route.FallbackAsync(headers);<br/>
    /// });
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task FallbackAsync(RouteFallbackOptions? options = default);

    /// <summary>
    /// <para>Fulfills route's request with given response.</para>
    /// <para>An example of fulfilling all requests with 404 responses:</para>
    /// <code>
    /// await page.RouteAsync("**/*", route =&gt; route.FulfillAsync(<br/>
    ///     status: 404,<br/>
    ///     contentType: "text/plain",<br/>
    ///     body: "Not Found!"));
    /// </code>
    /// <para>An example of serving static file:</para>
    /// <code>await page.RouteAsync("**/xhr_endpoint", route =&gt; route.FulfillAsync(new RouteFulfillOptions { Path = "mock_data.json" }));</code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task FulfillAsync(RouteFulfillOptions? options = default);

    /// <summary><para>A request to be routed.</para></summary>
    IRequest Request { get; }
}

#nullable disable
