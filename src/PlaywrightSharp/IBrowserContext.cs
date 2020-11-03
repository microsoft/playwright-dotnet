using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// BrowserContexts provide a way to operate multiple independent browser sessions.
    /// If a <see cref="IPage"/> opens another page, e.g.with a window.open call, the popup will belong to the parent page's browser context.
    /// PlaywrightSharp allows creation of "incognito" browser contexts with <seealso cref="IBrowser.NewContextAsync(BrowserContextOptions)"/> method. "Incognito" browser contexts don't write any browsing data to disk.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// // Create a new incognito browser context
    /// const context = await browser.NewContextAsync();
    /// // Create a new page inside context.
    /// const page = await context.NewPageAsync("https://example.com");
    /// // Dispose context once it's no longer needed.
    /// await context.CloseAsync();
    /// ]]>
    /// </code>
    /// </example>
    public interface IBrowserContext : IAsyncDisposable
    {
        /// <summary>
        /// Raised when Browser context gets closed.
        /// </summary>
        event EventHandler<EventArgs> Close;

        /// <summary>
        /// Raised when a new page is created in the Browser context.
        /// </summary>
        event EventHandler<PageEventArgs> Page;

        /// <summary>
        /// This setting will change the default maximum time for all the methods accepting timeout option.
        /// </summary>
        int DefaultTimeout { get; set; }

        /// <summary>
        /// Maximum navigation time in milliseconds.
        /// </summary>
        int DefaultNavigationTimeout { get; set; }

        /// <summary>
        /// Returns the browser instance of the context. If it was launched as a persistent context null gets returned.
        /// </summary>
        IBrowser Browser { get; }

        /// <summary>
        /// An array of all pages inside the browser context.
        /// </summary>
        IPage[] Pages { get; }

        /// <summary>
        /// Creates a new page in the browser context and optionally navigates it to the specified URL.
        /// </summary>
        /// <param name="url">URL to navigate after the page is created.</param>
        /// <returns>A <see cref="Task{IPage}"/> that completes when a new <see cref="IPage"/> is created, yielding the new <see cref="IPage"/>.</returns>.
        Task<IPage> NewPageAsync(string url = null);

        /// <summary>
        /// Closes the browser context. All the targets that belong to the browser context will be closed.
        /// </summary>
        /// <remarks>NOTE only incognito browser contexts can be closed.</remarks>
        /// <returns>A <see cref="Task"/> that completes when the browser context is closed.</returns>
        Task CloseAsync();

        /// <summary>
        /// Returns the context's cookies.
        /// </summary>
        /// <param name="urls">Url's to return cookies for.</param>
        /// <returns>A <see cref="Task"/> that completes when the cookies are sent by the browser, yielding a <see cref="t:NetworkCookie[]"/>.</returns>
        /// <remarks>
        /// If no URLs are specified, this method returns cookies for the current page URL.
        /// If URLs are specified, only cookies for those URLs are returned.
        /// </remarks>
        Task<IEnumerable<NetworkCookie>> GetCookiesAsync(params string[] urls);

        /// <summary>
        /// Adds a script which would be evaluated in one of the following scenarios:
        /// * Whenever a page is created in the browser context or is navigated.
        /// * Whenever a child frame is attached or navigated in any page in the browser context.In this case, the script is evaluated in the context of the newly attached frame.
        /// </summary>
        /// <param name="script">Script to be evaluated in all pages in the browser context or script path.</param>
        /// <param name="arg">Optional argument to pass to script .</param>
        /// <param name="path">Gets or sets the path to the JavaScript file to be injected into frame. If its a relative path, then it is resolved relative to <see cref="System.IO.Directory.GetCurrentDirectory"/>.</param>
        /// <param name="content">Gets or sets the raw JavaScript content to be injected into frame.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task AddInitScriptAsync(string script = null, object[] arg = null, string path = null, string content = null);

        /// <summary>
        /// Clears all of the current cookies and then sets the cookies for the context.
        /// </summary>
        /// <param name="cookies">Cookies to set.</param>
        /// <returns>A <see cref="Task"/> that completes when the cookies are set.</returns>
        Task AddCookiesAsync(IEnumerable<SetNetworkCookieParam> cookies);

        /// <summary>
        /// Clears all of the current cookies and then sets the cookies for the context.
        /// </summary>
        /// <param name="cookies">Cookies to set.</param>
        /// <returns>A <see cref="Task"/> that completes when the cookies are set.</returns>
        Task AddCookiesAsync(params SetNetworkCookieParam[] cookies);

        /// <summary>
        /// Clears the context's cookies.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the cookies are cleared.</returns>
        Task ClearCookiesAsync();

        /// <summary>
        /// Grants permissions to an URL.
        /// </summary>
        /// <param name="permissions">An array of permissions to grant.</param>
        /// <param name="origin">The origin to grant permissions to, e.g. "https://example.com".</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task GrantPermissionsAsync(ContextPermission[] permissions, string origin = null);

        /// <summary>
        /// Grants permissions to an URL.
        /// </summary>
        /// <param name="permission">Permission to grant.</param>
        /// <param name="origin">The origin to grant permissions to, e.g. "https://example.com".</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task GrantPermissionsAsync(ContextPermission permission, string origin = null);

        /// <summary>
        /// Sets the page's geolocation.
        /// </summary>
        /// <param name="latitude">Latitude between -90 and 90.</param>
        /// <param name="longitude">Longitude between -180 and 180.</param>
        /// <param name="accuracy">Optional non-negative accuracy value.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task SetGeolocationAsync(decimal latitude, decimal longitude, decimal accuracy = 0);

        /// <summary>
        /// Sets the page's geolocation.
        /// </summary>
        /// <param name="geolocation">Geolocation.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task SetGeolocationAsync(Geolocation geolocation);

        /// <summary>
        /// Clears all permission overrides for the browser context.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task ClearPermissionsAsync();

        /// <summary>
        /// Waits for event to fire and passes its value into the predicate function.
        /// </summary>
        /// <param name="e">Event to wait for.</param>
        /// <param name="predicate">Receives the event data and resolves when the waiting should resolve.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <typeparam name="T">Return type.</typeparam>
        /// <returns>A <see cref="Task"/> that completes when the predicate returns truthy value. Yielding the information of the event.</returns>
        Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> e, Func<T, bool> predicate = null, int? timeout = null)
            where T : EventArgs;

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightBinding"/> in C# and returns a <see cref="Task"/> which resolves when <paramref name="playwrightBinding"/> completes.
        /// </summary>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightBinding">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightBinding"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync(string, Action{BindingSource})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync(string name, Action<BindingSource> playwrightBinding);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightBinding"/> in C# and returns a <see cref="Task"/> which resolves when <paramref name="playwrightBinding"/> completes.
        /// </summary>
        /// <typeparam name="T">The parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightBinding">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightBinding"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync(string, Action{BindingSource})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> playwrightBinding);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightBinding"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightBinding"/>.
        /// </summary>
        /// <typeparam name="TResult">The result of <paramref name="playwrightBinding"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightBinding">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightBinding"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{TResult}(string, Func{BindingSource, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> playwrightBinding);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightBinding"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightBinding"/>.
        /// </summary>
        /// <typeparam name="T">The parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightBinding"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightBinding">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightBinding"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{T, TResult}(string, Func{BindingSource, T, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> playwrightBinding);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightBinding"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightBinding"/>.
        /// </summary>
        /// <typeparam name="TResult">The result of <paramref name="playwrightBinding"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightBinding">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightBinding"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{T, TResult}(string, Func{BindingSource, T, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> playwrightBinding);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightBinding"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightBinding"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightBinding"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightBinding">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightBinding"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{T1, T2, TResult}(string, Func{BindingSource, T1, T2, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> playwrightBinding);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightBinding"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightBinding"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <typeparam name="T3">The third parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightBinding"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightBinding">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightBinding"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{T1, T2, T3, TResult}(string, Func{BindingSource, T1, T2, T3, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> playwrightBinding);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightBinding"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightBinding"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <typeparam name="T3">The third parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <typeparam name="T4">The fourth parameter of <paramref name="playwrightBinding"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightBinding"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightBinding">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightBinding"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{T1, T2, T3, T4, TResult}(string, Func{BindingSource, T1, T2, T3, T4, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> playwrightBinding);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves when <paramref name="playwrightFunction"/> completes.
        /// </summary>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync(string, Action)"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync(string name, Action playwrightFunction);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves when <paramref name="playwrightFunction"/> completes.
        /// </summary>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <typeparam name="T">The parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync(string, Action)"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T>(string name, Action<T> playwrightFunction);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightFunction"/>.
        /// </summary>
        /// <typeparam name="TResult">The result of <paramref name="playwrightFunction"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{TResult}(string, Func{TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<TResult>(string name, Func<TResult> playwrightFunction);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightFunction"/>.
        /// </summary>
        /// <typeparam name="T">The parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightFunction"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T, TResult}(string, Func{T, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> playwrightFunction);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightFunction"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightFunction"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T1, T2, TResult}(string, Func{T1, T2, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> playwrightFunction);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightFunction"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T3">The third parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightFunction"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T1, T2, T3, TResult}(string, Func{T1, T2, T3, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> playwrightFunction);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightFunction"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T3">The third parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T4">The fourth parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightFunction"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T1, T2, T3, T4, TResult}(string, Func{T1, T2, T3, T4, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> playwrightFunction);

        /// <summary>
        /// Provide credentials for http authentication <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication"/>.
        /// </summary>
        /// <param name="httpCredentials">The credentials.</param>
        /// <returns>A <see cref="Task"/> that completes when the credentials are set.</returns>
        /// <remarks>
        /// To disable authentication, pass <c>null</c>.
        /// </remarks>
        Task SetHttpCredentialsAsync(Credentials httpCredentials);

        /// <summary>
        /// Routing provides the capability to modify network requests that are made by any page in the browser context.
        /// Once route is enabled, every request matching the url pattern will stall unless it's continued, fulfilled or aborted.
        /// </summary>
        /// <param name="url">A glob pattern to match while routing.</param>
        /// <param name="handler">Handler function to route the request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task RouteAsync(string url, Action<Route, IRequest> handler);

        /// <summary>
        /// Routing provides the capability to modify network requests that are made by a page.
        /// Once route is enabled, every request matching the url pattern will stall unless it's continued, fulfilled or aborted.
        /// </summary>
        /// <param name="url">A regex to match while routing.</param>
        /// <param name="handler">Handler function to route the request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task RouteAsync(Regex url, Action<Route, IRequest> handler);

        /// <summary>
        /// Routing provides the capability to modify network requests that are made by a page.
        /// Once route is enabled, every request matching the url pattern will stall unless it's continued, fulfilled or aborted.
        /// </summary>
        /// <param name="url">A fucntion that evaluate the URL match.</param>
        /// <param name="handler">Handler function to route the request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task RouteAsync(Func<string, bool> url, Action<Route, IRequest> handler);

        /// <summary>
        /// Removes a route created with <see cref="IBrowserContext.RouteAsync(string, Action{Route, IRequest})"/>. When handler is not specified, removes all routes for the url.
        /// </summary>
        /// <param name="url">A glob pattern used to match while routing.</param>
        /// <param name="handler">Handler function used to route a request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task UnrouteAsync(string url, Action<Route, IRequest> handler = null);

        /// <summary>
        /// Removes a route created with <see cref="IBrowserContext.RouteAsync(string, Action{Route, IRequest})"/>. When handler is not specified, removes all routes for the url.
        /// </summary>
        /// <param name="url">A glob pattern used to match while routing.</param>
        /// <param name="handler">Handler function used to route a request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task UnrouteAsync(Regex url, Action<Route, IRequest> handler = null);

        /// <summary>
        /// Removes a route created with <see cref="IBrowserContext.RouteAsync(string, Action{Route, IRequest})"/>. When handler is not specified, removes all routes for the url.
        /// </summary>
        /// <param name="url">A function used to match while routing.</param>
        /// <param name="handler">Handler function used to route a request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task UnrouteAsync(Func<string, bool> url, Action<Route, IRequest> handler = null);

        /// <summary>
        /// Set offline mode for the context.
        /// </summary>
        /// <returns>A<see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        /// <param name="offline">When <c>true</c> enables offline mode for the page.</param>
        Task SetOfflineAsync(bool offline);

        /// <summary>
        /// Sets extra HTTP headers that will be sent with every request in every page in the context.
        /// </summary>
        /// <param name="headers">Additional http headers to be sent with every request.</param>
        /// <returns>A <see cref="Task"/> that completes when the headers are set.</returns>
        Task SetExtraHttpHeadersAsync(Dictionary<string, string> headers);
    }
}
