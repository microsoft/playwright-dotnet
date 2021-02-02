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
 *
 *
 * ------------------------------------------------------------------------------ 
 * <auto-generated> 
 * This code was generated by a tool at:
 * /utils/doclint/generateDotnetApi.js
 * 
 * Changes to this file may cause incorrect behavior and will be lost if 
 * the code is regenerated. 
 * </auto-generated> 
 * ------------------------------------------------------------------------------
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
	/// <seealso cref="[EventEmitter]"/>
	/// BrowserContexts provide a way to operate multiple independent browser sessions.
	/// If a page opens another page, e.g. with a `window.open` call, the popup will belong to the parent page's browser context.
	/// Playwright allows creation of "incognito" browser contexts with `browser.newContext()` method. "Incognito" browser contexts
	/// don't write any browsing data to disk.
	/// </summary>
	public interface IBrowserContext
	{
		event EventHandler<IBrowserContext> OnClose;
		event EventHandler<IPage> OnPage;
		/// <summary>
		/// Adds cookies into this browser context. All pages within this context will have these cookies installed. Cookies can be obtained
		/// via <see cref="IBrowserContext.CookiesAsync"/>.
		/// </summary>
		Task AddCookiesAsync(BrowserContextCookies[] cookies);
		/// <summary>
		/// Adds a script which would be evaluated in one of the following scenarios:
		/// <list>
		/// <item><description>Whenever a page is created in the browser context or is navigated.</description></item>
		/// <item><description>Whenever a child frame is attached or navigated in any page in the browser context. In this case, the script is evaluated in the context of the newly attached frame.</description>
		/// </item>
		/// </list>
		/// The script is evaluated after the document was created but before any of its scripts were run. This is useful to amend the
		/// JavaScript environment, e.g. to seed `Math.random`.
		/// An example of overriding `Math.random` before the page loads:
		/// </summary>
		Task AddInitScriptAsync();
		/// <summary>
		/// Returns the browser instance of the context. If it was launched as a persistent context null gets returned.
		/// </summary>
		IBrowser GetBrowser();
		/// <summary>
		/// Clears context cookies.
		/// </summary>
		Task ClearCookiesAsync();
		/// <summary>
		/// Clears all permission overrides for the browser context.
		/// </summary>
		Task ClearPermissionsAsync();
		/// <summary>
		/// Closes the browser context. All the pages that belong to the browser context will be closed.
		/// </summary>
		Task CloseAsync();
		/// <summary>
		/// If no URLs are specified, this method returns all cookies. If URLs are specified, only cookies that affect those URLs are
		/// returned.
		/// </summary>
		Task<BrowserContextCookiesResult[]> CookiesAsync(string[] urls);
		/// <summary>
		/// The method adds a function called {PARAM} on the `window` object of every frame in every page in the context. When called,
		/// the function executes {PARAM} and returns a [Promise] which resolves to the return value of {PARAM}. If the {PARAM} returns a [Promise], it will be awaited.
		/// The first argument of the {PARAM} function contains information about the caller: `{ browserContext: BrowserContext, page:
		/// Page, frame: Frame }`.
		/// See <see cref="IPage.ExposeBindingAsync"/> for page-only version.
		/// An example of exposing page URL to all frames in all pages in the context:
		/// An example of passing an element handle:
		/// </summary>
		Task ExposeBindingAsync(string name, Action callback, bool handle);
		/// <summary>
		/// The method adds a function called {PARAM} on the `window` object of every frame in every page in the context. When called,
		/// the function executes {PARAM} and returns a [Promise] which resolves to the return value of {PARAM}.
		/// If the {PARAM} returns a [Promise], it will be awaited.
		/// See <see cref="IPage.ExposeFunctionAsync"/> for page-only version.
		/// An example of adding an `md5` function to all pages in the context:
		/// </summary>
		Task ExposeFunctionAsync(string name, Action callback);
		/// <summary>
		/// Grants specified permissions to the browser context. Only grants corresponding permissions to the given origin if specified.
		/// </summary>
		Task GrantPermissionsAsync(string[] permissions, string origin);
		/// <summary>
		/// Creates a new page in the browser context.
		/// </summary>
		Task<IPage> GetNewPageAsync();
		/// <summary>
		/// Returns all open pages in the context. Non visible pages, such as `"background_page"`, will not be listed here. You can find
		/// them using <see cref="IChromiumBrowserContext.BackgroundPages"/>.
		/// </summary>
		dynamic GetPages();
		/// <summary>
		/// Routing provides the capability to modify network requests that are made by any page in the browser context. Once route is
		/// enabled, every request matching the url pattern will stall unless it's continued, fulfilled or aborted.
		/// An example of a naïve handler that aborts all image requests:
		/// or the same snippet using a regex pattern instead:
		/// Page routes (set up with <see cref="IPage.RouteAsync"/>) take precedence over browser context routes when request matches
		/// both handlers.
		/// </summary>
		Task RouteAsync(Union url, Action handler);
		/// <summary>
		/// This setting will change the default maximum navigation time for the following methods and related shortcuts:
		/// <list>
		/// <item><description><see cref="IPage.GoBackAsync"/></description></item>
		/// <item><description><see cref="IPage.GoForwardAsync"/></description></item>
		/// <item><description><see cref="IPage.GotoAsync"/></description></item>
		/// <item><description><see cref="IPage.ReloadAsync"/></description></item>
		/// <item><description><see cref="IPage.SetContentAsync"/></description></item>
		/// <item><description><see cref="IPage.WaitForNavigationAsync"/></description></item>
		/// </summary>
		void SetDefaultNavigationTimeout(float timeout);
		/// <summary>
		/// This setting will change the default maximum time for all the methods accepting {PARAM} option.
		/// </summary>
		void SetDefaultTimeout(float timeout);
		/// <summary>
		/// The extra HTTP headers will be sent with every request initiated by any page in the context. These headers are merged with
		/// page-specific extra HTTP headers set with <see cref="IPage.SetExtraHTTPHeadersAsync"/>. If page overrides a particular header,
		/// page-specific header value will be used instead of the browser context header value.
		/// </summary>
		Task SetExtraHTTPHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers);
		/// <summary>
		/// Sets the context's geolocation. Passing `null` or `undefined` emulates position unavailable.
		/// </summary>
		Task SetGeolocationAsync(BrowserContextGeolocation geolocation);
		Task SetOfflineAsync(bool offline);
		/// <summary>
		/// Returns storage state for this browser context, contains current cookies and local storage snapshot.
		/// </summary>
		Task<BrowserContextStorageStateResult> StorageStateAsync(string path);
		/// <summary>
		/// Removes a route created with <see cref="IBrowserContext.RouteAsync"/>. When {PARAM} is not specified, removes all routes
		/// for the {PARAM}.
		/// </summary>
		Task UnrouteAsync(Union url, Action handler);
		/// <summary>
		/// Waits for event to fire and passes its value into the predicate function. Returns when the predicate returns truthy value.
		/// Will throw an error if the context closes before the event is fired. Returns the event data value.
		/// </summary>
		Task<T> WaitForEventAsync<T>(string @event);
	}
}