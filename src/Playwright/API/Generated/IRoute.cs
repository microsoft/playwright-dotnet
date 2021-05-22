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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    /// <summary>
    /// <para>
    /// Whenever a network route is set up with <see cref="IPage.RouteAsync"/> or <see cref="IBrowserContext.RouteAsync"/>,
    /// the <c>Route</c> object allows to handle the route.
    /// </para>
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
        Task AbortAsync(string errorCode = default);

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
        Task ContinueAsync(RouteContinueOptions options = default);

        /// <summary>
        /// <para>Fulfills route's request with given response.</para>
        /// <para>An example of fulfilling all requests with 404 responses:</para>
        /// <code>
        /// await page.RouteAsync("**/*", route =&gt; route.FulfillAsync(<br/>
        ///     status: 404,<br/>
        ///     contentType: "text/plain", <br/>
        ///     body: "Not Found!"));
        /// </code>
        /// <para>An example of serving static file:</para>
        /// <code>await page.RouteAsync("**/xhr_endpoint", route =&gt; route.FulfillAsync(new RouteFulfillOptions { Path = "mock_data.json" }));</code>
        /// </summary>
        /// <param name="options">Call options</param>
        Task FulfillAsync(RouteFulfillOptions options = default);

        /// <summary><para>A request to be routed.</para></summary>
        IRequest Request { get; }
    }
}
