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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>BrowserContexts provide a way to operate multiple independent browser sessions.</para>
/// <para>
/// If a page opens another page, e.g. with a <c>window.open</c> call, the popup will
/// belong to the parent page's browser context.
/// </para>
/// <para>
/// Playwright allows creating "incognito" browser contexts with <see cref="IBrowser.NewContextAsync"/>
/// method. "Incognito" browser contexts don't write any browsing data to disk.
/// </para>
/// <code>
/// using var playwright = await Playwright.CreateAsync();<br/>
/// var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });<br/>
/// // Create a new incognito browser context<br/>
/// var context = await browser.NewContextAsync();<br/>
/// // Create a new page inside context.<br/>
/// var page = await context.NewPageAsync();<br/>
/// await page.GotoAsync("https://bing.com");<br/>
/// // Dispose context once it is no longer needed.<br/>
/// await context.CloseAsync();
/// </code>
/// </summary>
public partial interface IBrowserContext
{
    /// <summary>
    /// <para>
    /// Emitted when Browser context gets closed. This might happen because of one of the
    /// following:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Browser context is closed.</description></item>
    /// <item><description>Browser application is closed or crashed.</description></item>
    /// <item><description>The <see cref="IBrowser.CloseAsync"/> method was called.</description></item>
    /// </list>
    /// </summary>
    event EventHandler<IBrowserContext> Close;

    /// <summary>
    /// <para>
    /// The event is emitted when a new Page is created in the BrowserContext. The page
    /// may still be loading. The event will also fire for popup pages. See also <see cref="IPage.Popup"/>
    /// to receive events about popups relevant to a specific page.
    /// </para>
    /// <para>
    /// The earliest moment that page is available is when it has navigated to the initial
    /// url. For example, when opening a popup with <c>window.open('http://example.com')</c>,
    /// this event will fire when the network request to "http://example.com" is done and
    /// its response has started loading in the popup.
    /// </para>
    /// <code>
    /// var popup = await context.RunAndWaitForPageAsync(async =&gt;<br/>
    /// {<br/>
    ///     await page.Locator("a").ClickAsync();<br/>
    /// });<br/>
    /// Console.WriteLine(await popup.EvaluateAsync&lt;string&gt;("location.href"));
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="IPage.WaitForLoadStateAsync"/> to wait until the page gets to a particular
    /// state (you should not need it in most cases).
    /// </para>
    /// </remarks>
    event EventHandler<IPage> Page;

    /// <summary>
    /// <para>
    /// Emitted when a request is issued from any pages created through this context. The
    /// <see cref="request"/> object is read-only. To only listen for requests from a particular
    /// page, use <see cref="IPage.Request"/>.
    /// </para>
    /// <para>
    /// In order to intercept and mutate requests, see <see cref="IBrowserContext.RouteAsync"/>
    /// or <see cref="IPage.RouteAsync"/>.
    /// </para>
    /// </summary>
    event EventHandler<IRequest> Request;

    /// <summary>
    /// <para>
    /// Emitted when a request fails, for example by timing out. To only listen for failed
    /// requests from a particular page, use <see cref="IPage.RequestFailed"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// HTTP Error responses, such as 404 or 503, are still successful responses from HTTP
    /// standpoint, so request will complete with <see cref="IBrowserContext.RequestFinished"/>
    /// event and not with <see cref="IBrowserContext.RequestFailed"/>.
    /// </para>
    /// </remarks>
    event EventHandler<IRequest> RequestFailed;

    /// <summary>
    /// <para>
    /// Emitted when a request finishes successfully after downloading the response body.
    /// For a successful response, the sequence of events is <c>request</c>, <c>response</c>
    /// and <c>requestfinished</c>. To listen for successful requests from a particular
    /// page, use <see cref="IPage.RequestFinished"/>.
    /// </para>
    /// </summary>
    event EventHandler<IRequest> RequestFinished;

    /// <summary>
    /// <para>
    /// Emitted when <see cref="response"/> status and headers are received for a request.
    /// For a successful response, the sequence of events is <c>request</c>, <c>response</c>
    /// and <c>requestfinished</c>. To listen for response events from a particular page,
    /// use <see cref="IPage.Response"/>.
    /// </para>
    /// </summary>
    event EventHandler<IResponse> Response;

    /// <summary>
    /// <para>
    /// Adds cookies into this browser context. All pages within this context will have
    /// these cookies installed. Cookies can be obtained via <see cref="IBrowserContext.CookiesAsync"/>.
    /// </para>
    /// <code>await context.AddCookiesAsync(new[] { cookie1, cookie2 });</code>
    /// </summary>
    /// <param name="cookies">
    /// </param>
    Task AddCookiesAsync(IEnumerable<Cookie> cookies);

    /// <summary>
    /// <para>Adds a script which would be evaluated in one of the following scenarios:</para>
    /// <list type="bullet">
    /// <item><description>Whenever a page is created in the browser context or is navigated.</description></item>
    /// <item><description>
    /// Whenever a child frame is attached or navigated in any page in the browser context.
    /// In this case, the script is evaluated in the context of the newly attached frame.
    /// </description></item>
    /// </list>
    /// <para>
    /// The script is evaluated after the document was created but before any of its scripts
    /// were run. This is useful to amend the JavaScript environment, e.g. to seed <c>Math.random</c>.
    /// </para>
    /// <para>An example of overriding <c>Math.random</c> before the page loads:</para>
    /// <code>await context.AddInitScriptAsync(new BrowserContextAddInitScriptOptions { ScriptPath = "preload.js" });</code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order of evaluation of multiple scripts installed via <see cref="IBrowserContext.AddInitScriptAsync"/>
    /// and <see cref="IPage.AddInitScriptAsync"/> is not defined.
    /// </para>
    /// </remarks>
    /// <param name="script">Script to be evaluated in all pages in the browser context.</param>
    /// <param name="scriptPath">Instead of specifying <paramref name="script"/>, gives the file name to load from.</param>
    Task AddInitScriptAsync(string? script = default, string? scriptPath = default);

    /// <summary>
    /// <para>
    /// Returns the browser instance of the context. If it was launched as a persistent
    /// context null gets returned.
    /// </para>
    /// </summary>
    IBrowser? Browser { get; }

    /// <summary><para>Clears context cookies.</para></summary>
    Task ClearCookiesAsync();

    /// <summary>
    /// <para>Clears all permission overrides for the browser context.</para>
    /// <code>
    /// var context = await browser.NewContextAsync();<br/>
    /// await context.GrantPermissionsAsync(new[] { "clipboard-read" });<br/>
    /// // Alternatively, you can use the helper class ContextPermissions<br/>
    /// //  to specify the permissions...<br/>
    /// // do stuff ...<br/>
    /// await context.ClearPermissionsAsync();
    /// </code>
    /// </summary>
    Task ClearPermissionsAsync();

    /// <summary>
    /// <para>
    /// Closes the browser context. All the pages that belong to the browser context will
    /// be closed.
    /// </para>
    /// </summary>
    /// <remarks><para>The default browser context cannot be closed.</para></remarks>
    Task CloseAsync();

    /// <summary>
    /// <para>
    /// If no URLs are specified, this method returns all cookies. If URLs are specified,
    /// only cookies that affect those URLs are returned.
    /// </para>
    /// </summary>
    /// <param name="urls">Optional list of URLs.</param>
    Task<IReadOnlyList<BrowserContextCookiesResult>> CookiesAsync(IEnumerable<string>? urls = default);

    /// <summary>
    /// <para>
    /// The method adds a function called <paramref name="name"/> on the <c>window</c> object
    /// of every frame in every page in the context. When called, the function executes
    /// <paramref name="callback"/> and returns a <see cref="Task"/> which resolves to the
    /// return value of <paramref name="callback"/>. If the <paramref name="callback"/>
    /// returns a <see cref="Promise"/>, it will be awaited.
    /// </para>
    /// <para>
    /// The first argument of the <paramref name="callback"/> function contains information
    /// about the caller: <c>{ browserContext: BrowserContext, page: Page, frame: Frame
    /// }</c>.
    /// </para>
    /// <para>See <see cref="IPage.ExposeBindingAsync"/> for page-only version.</para>
    /// <para>An example of exposing page URL to all frames in all pages in the context:</para>
    /// <code>
    /// using Microsoft.Playwright;<br/>
    /// <br/>
    /// using var playwright = await Playwright.CreateAsync();<br/>
    /// var browser = await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });<br/>
    /// var context = await browser.NewContextAsync();<br/>
    /// <br/>
    /// await context.ExposeBindingAsync("pageURL", source =&gt; source.Page.Url);<br/>
    /// var page = await context.NewPageAsync();<br/>
    /// await page.SetContentAsync("&lt;script&gt;\n" +<br/>
    /// "  async function onClick() {\n" +<br/>
    /// "    document.querySelector('div').textContent = await window.pageURL();\n" +<br/>
    /// "  }\n" +<br/>
    /// "&lt;/script&gt;\n" +<br/>
    /// "&lt;button onclick=\"onClick()\"&gt;Click me&lt;/button&gt;\n" +<br/>
    /// "&lt;div&gt;&lt;/div&gt;");<br/>
    /// await page.GetByRole("button").ClickAsync();
    /// </code>
    /// <para>An example of passing an element handle:</para>
    /// <code>
    /// var result = new TaskCompletionSource&lt;string&gt;();<br/>
    /// var page = await Context.NewPageAsync();<br/>
    /// await Context.ExposeBindingAsync("clicked", async (BindingSource _, IJSHandle t) =&gt;<br/>
    /// {<br/>
    ///     return result.TrySetResult(await t.AsElement().TextContentAsync());<br/>
    /// });<br/>
    /// <br/>
    /// await page.SetContentAsync("&lt;script&gt;\n" +<br/>
    ///   "  document.addEventListener('click', event =&gt; window.clicked(event.target));\n" +<br/>
    ///   "&lt;/script&gt;\n" +<br/>
    ///   "&lt;div&gt;Click me&lt;/div&gt;\n" +<br/>
    ///   "&lt;div&gt;Or click me&lt;/div&gt;\n");<br/>
    /// <br/>
    /// await page.ClickAsync("div");<br/>
    /// // Note: it makes sense to await the result here, because otherwise, the context<br/>
    /// //  gets closed and the binding function will throw an exception.<br/>
    /// Assert.AreEqual("Click me", await result.Task);
    /// </code>
    /// </summary>
    /// <param name="name">Name of the function on the window object.</param>
    /// <param name="callback">Callback function that will be called in the Playwright's context.</param>
    /// <param name="options">Call options</param>
    Task ExposeBindingAsync(string name, Action callback, BrowserContextExposeBindingOptions? options = default);

    /// <summary>
    /// <para>
    /// The method adds a function called <paramref name="name"/> on the <c>window</c> object
    /// of every frame in every page in the context. When called, the function executes
    /// <paramref name="callback"/> and returns a <see cref="Task"/> which resolves to the
    /// return value of <paramref name="callback"/>.
    /// </para>
    /// <para>If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.</para>
    /// <para>See <see cref="IPage.ExposeFunctionAsync"/> for page-only version.</para>
    /// <para>An example of adding a <c>sha256</c> function to all pages in the context:</para>
    /// <code>
    /// using Microsoft.Playwright;<br/>
    /// using System;<br/>
    /// using System.Security.Cryptography;<br/>
    /// using System.Threading.Tasks;<br/>
    /// <br/>
    /// class BrowserContextExamples<br/>
    /// {<br/>
    ///     public static async Task Main()<br/>
    ///     {<br/>
    ///         using var playwright = await Playwright.CreateAsync();<br/>
    ///         var browser = await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });<br/>
    ///         var context = await browser.NewContextAsync();<br/>
    /// <br/>
    ///         await context.ExposeFunctionAsync("sha256", (string input) =&gt;<br/>
    ///         {<br/>
    ///             return Convert.ToBase64String(<br/>
    ///                 SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));<br/>
    ///         });<br/>
    /// <br/>
    ///         var page = await context.NewPageAsync();<br/>
    ///         await page.SetContentAsync("&lt;script&gt;\n" +<br/>
    ///         "  async function onClick() {\n" +<br/>
    ///         "    document.querySelector('div').textContent = await window.sha256('PLAYWRIGHT');\n" +<br/>
    ///         "  }\n" +<br/>
    ///         "&lt;/script&gt;\n" +<br/>
    ///         "&lt;button onclick=\"onClick()\"&gt;Click me&lt;/button&gt;\n" +<br/>
    ///         "&lt;div&gt;&lt;/div&gt;");<br/>
    /// <br/>
    ///         await page.GetByRole("button").ClickAsync();<br/>
    ///         Console.WriteLine(await page.TextContentAsync("div"));<br/>
    ///     }<br/>
    /// }
    /// </code>
    /// </summary>
    /// <param name="name">Name of the function on the window object.</param>
    /// <param name="callback">Callback function that will be called in the Playwright's context.</param>
    Task ExposeFunctionAsync(string name, Action callback);

    /// <summary>
    /// <para>
    /// Grants specified permissions to the browser context. Only grants corresponding permissions
    /// to the given origin if specified.
    /// </para>
    /// </summary>
    /// <param name="permissions">
    /// A permission or an array of permissions to grant. Permissions can be one of the
    /// following values:
    /// <list type="bullet">
    /// <item><description><c>'geolocation'</c></description></item>
    /// <item><description><c>'midi'</c></description></item>
    /// <item><description><c>'midi-sysex'</c> (system-exclusive midi)</description></item>
    /// <item><description><c>'notifications'</c></description></item>
    /// <item><description><c>'camera'</c></description></item>
    /// <item><description><c>'microphone'</c></description></item>
    /// <item><description><c>'background-sync'</c></description></item>
    /// <item><description><c>'ambient-light-sensor'</c></description></item>
    /// <item><description><c>'accelerometer'</c></description></item>
    /// <item><description><c>'gyroscope'</c></description></item>
    /// <item><description><c>'magnetometer'</c></description></item>
    /// <item><description><c>'accessibility-events'</c></description></item>
    /// <item><description><c>'clipboard-read'</c></description></item>
    /// <item><description><c>'clipboard-write'</c></description></item>
    /// <item><description><c>'payment-handler'</c></description></item>
    /// </list>
    /// </param>
    /// <param name="options">Call options</param>
    Task GrantPermissionsAsync(IEnumerable<string> permissions, BrowserContextGrantPermissionsOptions? options = default);

    /// <summary><para>Creates a new page in the browser context.</para></summary>
    Task<IPage> NewPageAsync();

    /// <summary><para>Returns all open pages in the context.</para></summary>
    IReadOnlyList<IPage> Pages { get; }

    /// <summary>
    /// <para>
    /// API testing helper associated with this context. Requests made with this API will
    /// use context cookies.
    /// </para>
    /// </summary>
    public IAPIRequestContext APIRequest { get; }

    /// <summary>
    /// <para>
    /// Routing provides the capability to modify network requests that are made by any
    /// page in the browser context. Once route is enabled, every request matching the url
    /// pattern will stall unless it's continued, fulfilled or aborted.
    /// </para>
    /// <para>An example of a naive handler that aborts all image requests:</para>
    /// <code>
    /// var context = await browser.NewContextAsync();<br/>
    /// var page = await context.NewPageAsync();<br/>
    /// await context.RouteAsync("**/*.{png,jpg,jpeg}", r =&gt; r.AbortAsync());<br/>
    /// await page.GotoAsync("https://theverge.com");<br/>
    /// await browser.CloseAsync();
    /// </code>
    /// <para>or the same snippet using a regex pattern instead:</para>
    /// <code>
    /// var context = await browser.NewContextAsync();<br/>
    /// var page = await context.NewPageAsync();<br/>
    /// await context.RouteAsync(new Regex("(\\.png$)|(\\.jpg$)"), r =&gt; r.AbortAsync());<br/>
    /// await page.GotoAsync("https://theverge.com");<br/>
    /// await browser.CloseAsync();
    /// </code>
    /// <para>
    /// It is possible to examine the request to decide the route action. For example, mocking
    /// all requests that contain some post data, and leaving all other requests as is:
    /// </para>
    /// <code>
    /// await page.RouteAsync("/api/**", async r =&gt;<br/>
    /// {<br/>
    ///     if (r.Request.PostData.Contains("my-string"))<br/>
    ///         await r.FulfillAsync(body: "mocked-data");<br/>
    ///     else<br/>
    ///         await r.ContinueAsync();<br/>
    /// });
    /// </code>
    /// <para>
    /// Page routes (set up with <see cref="IPage.RouteAsync"/>) take precedence over browser
    /// context routes when request matches both handlers.
    /// </para>
    /// <para>To remove a route with its handler you can use <see cref="IBrowserContext.UnrouteAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IBrowserContext.RouteAsync"/> will not intercept requests intercepted
    /// by Service Worker. See <a href="https://github.com/microsoft/playwright/issues/1090">this</a>
    /// issue. We recommend disabling Service Workers when using request interception by
    /// setting <paramref name="Browser.newContext.serviceWorkers"/> to <c>'block'</c>.
    /// </para>
    /// <para>Enabling routing disables http cache.</para>
    /// </remarks>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while routing. When a <paramref name="baseURL"/> via the context options was provided
    /// and the passed URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="handler">handler function to route the request.</param>
    /// <param name="options">Call options</param>
    Task RouteAsync(string url, Action<IRoute> handler, BrowserContextRouteOptions? options = default);

    /// <summary>
    /// <para>
    /// Routing provides the capability to modify network requests that are made by any
    /// page in the browser context. Once route is enabled, every request matching the url
    /// pattern will stall unless it's continued, fulfilled or aborted.
    /// </para>
    /// <para>An example of a naive handler that aborts all image requests:</para>
    /// <code>
    /// var context = await browser.NewContextAsync();<br/>
    /// var page = await context.NewPageAsync();<br/>
    /// await context.RouteAsync("**/*.{png,jpg,jpeg}", r =&gt; r.AbortAsync());<br/>
    /// await page.GotoAsync("https://theverge.com");<br/>
    /// await browser.CloseAsync();
    /// </code>
    /// <para>or the same snippet using a regex pattern instead:</para>
    /// <code>
    /// var context = await browser.NewContextAsync();<br/>
    /// var page = await context.NewPageAsync();<br/>
    /// await context.RouteAsync(new Regex("(\\.png$)|(\\.jpg$)"), r =&gt; r.AbortAsync());<br/>
    /// await page.GotoAsync("https://theverge.com");<br/>
    /// await browser.CloseAsync();
    /// </code>
    /// <para>
    /// It is possible to examine the request to decide the route action. For example, mocking
    /// all requests that contain some post data, and leaving all other requests as is:
    /// </para>
    /// <code>
    /// await page.RouteAsync("/api/**", async r =&gt;<br/>
    /// {<br/>
    ///     if (r.Request.PostData.Contains("my-string"))<br/>
    ///         await r.FulfillAsync(body: "mocked-data");<br/>
    ///     else<br/>
    ///         await r.ContinueAsync();<br/>
    /// });
    /// </code>
    /// <para>
    /// Page routes (set up with <see cref="IPage.RouteAsync"/>) take precedence over browser
    /// context routes when request matches both handlers.
    /// </para>
    /// <para>To remove a route with its handler you can use <see cref="IBrowserContext.UnrouteAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IBrowserContext.RouteAsync"/> will not intercept requests intercepted
    /// by Service Worker. See <a href="https://github.com/microsoft/playwright/issues/1090">this</a>
    /// issue. We recommend disabling Service Workers when using request interception by
    /// setting <paramref name="Browser.newContext.serviceWorkers"/> to <c>'block'</c>.
    /// </para>
    /// <para>Enabling routing disables http cache.</para>
    /// </remarks>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while routing. When a <paramref name="baseURL"/> via the context options was provided
    /// and the passed URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="handler">handler function to route the request.</param>
    /// <param name="options">Call options</param>
    Task RouteAsync(Regex url, Action<IRoute> handler, BrowserContextRouteOptions? options = default);

    /// <summary>
    /// <para>
    /// Routing provides the capability to modify network requests that are made by any
    /// page in the browser context. Once route is enabled, every request matching the url
    /// pattern will stall unless it's continued, fulfilled or aborted.
    /// </para>
    /// <para>An example of a naive handler that aborts all image requests:</para>
    /// <code>
    /// var context = await browser.NewContextAsync();<br/>
    /// var page = await context.NewPageAsync();<br/>
    /// await context.RouteAsync("**/*.{png,jpg,jpeg}", r =&gt; r.AbortAsync());<br/>
    /// await page.GotoAsync("https://theverge.com");<br/>
    /// await browser.CloseAsync();
    /// </code>
    /// <para>or the same snippet using a regex pattern instead:</para>
    /// <code>
    /// var context = await browser.NewContextAsync();<br/>
    /// var page = await context.NewPageAsync();<br/>
    /// await context.RouteAsync(new Regex("(\\.png$)|(\\.jpg$)"), r =&gt; r.AbortAsync());<br/>
    /// await page.GotoAsync("https://theverge.com");<br/>
    /// await browser.CloseAsync();
    /// </code>
    /// <para>
    /// It is possible to examine the request to decide the route action. For example, mocking
    /// all requests that contain some post data, and leaving all other requests as is:
    /// </para>
    /// <code>
    /// await page.RouteAsync("/api/**", async r =&gt;<br/>
    /// {<br/>
    ///     if (r.Request.PostData.Contains("my-string"))<br/>
    ///         await r.FulfillAsync(body: "mocked-data");<br/>
    ///     else<br/>
    ///         await r.ContinueAsync();<br/>
    /// });
    /// </code>
    /// <para>
    /// Page routes (set up with <see cref="IPage.RouteAsync"/>) take precedence over browser
    /// context routes when request matches both handlers.
    /// </para>
    /// <para>To remove a route with its handler you can use <see cref="IBrowserContext.UnrouteAsync"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IBrowserContext.RouteAsync"/> will not intercept requests intercepted
    /// by Service Worker. See <a href="https://github.com/microsoft/playwright/issues/1090">this</a>
    /// issue. We recommend disabling Service Workers when using request interception by
    /// setting <paramref name="Browser.newContext.serviceWorkers"/> to <c>'block'</c>.
    /// </para>
    /// <para>Enabling routing disables http cache.</para>
    /// </remarks>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while routing. When a <paramref name="baseURL"/> via the context options was provided
    /// and the passed URL is a path, it gets merged via the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>new
    /// URL()</c></a> constructor.
    /// </param>
    /// <param name="handler">handler function to route the request.</param>
    /// <param name="options">Call options</param>
    Task RouteAsync(Func<string, bool> url, Action<IRoute> handler, BrowserContextRouteOptions? options = default);

    /// <summary>
    /// <para>
    /// If specified the network requests that are made in the context will be served from
    /// the HAR file. Read more about <a href="https://playwright.dev/dotnet/docs/network#replaying-from-har">Replaying
    /// from HAR</a>.
    /// </para>
    /// <para>
    /// Playwright will not serve requests intercepted by Service Worker from the HAR file.
    /// See <a href="https://github.com/microsoft/playwright/issues/1090">this</a> issue.
    /// We recommend disabling Service Workers when using request interception by setting
    /// <paramref name="Browser.newContext.serviceWorkers"/> to <c>'block'</c>.
    /// </para>
    /// </summary>
    /// <param name="har">
    /// Path to a <a href="http://www.softwareishard.com/blog/har-12-spec">HAR</a> file
    /// with prerecorded network data. If <c>path</c> is a relative path, then it is resolved
    /// relative to the current working directory.
    /// </param>
    /// <param name="options">Call options</param>
    Task RouteFromHARAsync(string har, BrowserContextRouteFromHAROptions? options = default);

    /// <summary>
    /// <para>
    /// This setting will change the default maximum navigation time for the following methods
    /// and related shortcuts:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="IPage.GoBackAsync"/></description></item>
    /// <item><description><see cref="IPage.GoForwardAsync"/></description></item>
    /// <item><description><see cref="IPage.GotoAsync"/></description></item>
    /// <item><description><see cref="IPage.ReloadAsync"/></description></item>
    /// <item><description><see cref="IPage.SetContentAsync"/></description></item>
    /// <item><description><see cref="IPage.RunAndWaitForNavigationAsync"/></description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IPage.SetDefaultNavigationTimeout"/> and <see cref="IPage.SetDefaultTimeout"/>
    /// take priority over <see cref="IBrowserContext.SetDefaultNavigationTimeout"/>.
    /// </para>
    /// </remarks>
    /// <param name="timeout">Maximum navigation time in milliseconds</param>
    void SetDefaultNavigationTimeout(float timeout);

    /// <summary>
    /// <para>
    /// This setting will change the default maximum time for all the methods accepting
    /// <paramref name="timeout"/> option.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IPage.SetDefaultNavigationTimeout"/>, <see cref="IPage.SetDefaultTimeout"/>
    /// and <see cref="IBrowserContext.SetDefaultNavigationTimeout"/> take priority over
    /// <see cref="IBrowserContext.SetDefaultTimeout"/>.
    /// </para>
    /// </remarks>
    /// <param name="timeout">Maximum time in milliseconds</param>
    void SetDefaultTimeout(float timeout);

    /// <summary>
    /// <para>
    /// The extra HTTP headers will be sent with every request initiated by any page in
    /// the context. These headers are merged with page-specific extra HTTP headers set
    /// with <see cref="IPage.SetExtraHTTPHeadersAsync"/>. If page overrides a particular
    /// header, page-specific header value will be used instead of the browser context header
    /// value.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IBrowserContext.SetExtraHTTPHeadersAsync"/> does not guarantee the order
    /// of headers in the outgoing requests.
    /// </para>
    /// </remarks>
    /// <param name="headers">
    /// An object containing additional HTTP headers to be sent with every request. All
    /// header values must be strings.
    /// </param>
    Task SetExtraHTTPHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers);

    /// <summary>
    /// <para>
    /// Sets the context's geolocation. Passing <c>null</c> or <c>undefined</c> emulates
    /// position unavailable.
    /// </para>
    /// <code>
    /// await context.SetGeolocationAsync(new Geolocation()<br/>
    /// {<br/>
    ///     Latitude = 59.95f,<br/>
    ///     Longitude = 30.31667f<br/>
    /// });
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Consider using <see cref="IBrowserContext.GrantPermissionsAsync"/> to grant permissions
    /// for the browser context pages to read its geolocation.
    /// </para>
    /// </remarks>
    /// <param name="geolocation">
    /// </param>
    Task SetGeolocationAsync(Geolocation? geolocation);

    /// <param name="offline">Whether to emulate network being offline for the browser context.</param>
    Task SetOfflineAsync(bool offline);

    /// <summary>
    /// <para>
    /// Returns storage state for this browser context, contains current cookies and local
    /// storage snapshot.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<string> StorageStateAsync(BrowserContextStorageStateOptions? options = default);

    public ITracing Tracing { get; }

    /// <summary>
    /// <para>
    /// Removes a route created with <see cref="IBrowserContext.RouteAsync"/>. When <paramref
    /// name="handler"/> is not specified, removes all routes for the <paramref name="url"/>.
    /// </para>
    /// </summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> used to register
    /// a routing with <see cref="IBrowserContext.RouteAsync"/>.
    /// </param>
    /// <param name="handler">Optional handler function used to register a routing with <see cref="IBrowserContext.RouteAsync"/>.</param>
    Task UnrouteAsync(string url, Action<IRoute>? handler = default);

    /// <summary>
    /// <para>
    /// Removes a route created with <see cref="IBrowserContext.RouteAsync"/>. When <paramref
    /// name="handler"/> is not specified, removes all routes for the <paramref name="url"/>.
    /// </para>
    /// </summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> used to register
    /// a routing with <see cref="IBrowserContext.RouteAsync"/>.
    /// </param>
    /// <param name="handler">Optional handler function used to register a routing with <see cref="IBrowserContext.RouteAsync"/>.</param>
    Task UnrouteAsync(Regex url, Action<IRoute>? handler = default);

    /// <summary>
    /// <para>
    /// Removes a route created with <see cref="IBrowserContext.RouteAsync"/>. When <paramref
    /// name="handler"/> is not specified, removes all routes for the <paramref name="url"/>.
    /// </para>
    /// </summary>
    /// <param name="url">
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> used to register
    /// a routing with <see cref="IBrowserContext.RouteAsync"/>.
    /// </param>
    /// <param name="handler">Optional handler function used to register a routing with <see cref="IBrowserContext.RouteAsync"/>.</param>
    Task UnrouteAsync(Func<string, bool> url, Action<IRoute>? handler = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a new <see cref="IPage"/> to be created in the context.
    /// If predicate is provided, it passes <see cref="IPage"/> value into the <c>predicate</c>
    /// function and waits for <c>predicate(event)</c> to return a truthy value. Will throw
    /// an error if the context closes before new <see cref="IPage"/> is created.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IPage> WaitForPageAsync(BrowserContextWaitForPageOptions? options = default);

    /// <summary>
    /// <para>
    /// Performs action and waits for a new <see cref="IPage"/> to be created in the context.
    /// If predicate is provided, it passes <see cref="IPage"/> value into the <c>predicate</c>
    /// function and waits for <c>predicate(event)</c> to return a truthy value. Will throw
    /// an error if the context closes before new <see cref="IPage"/> is created.
    /// </para>
    /// </summary>
    /// <param name="action">Action that triggers the event.</param>
    /// <param name="options">Call options</param>
    Task<IPage> RunAndWaitForPageAsync(Func<Task> action, BrowserContextRunAndWaitForPageOptions? options = default);
}

#nullable disable
