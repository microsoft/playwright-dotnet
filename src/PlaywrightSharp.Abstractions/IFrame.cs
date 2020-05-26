using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// At every point of time, page exposes its current frame tree via the <see cref="IPage.MainFrame"/> and <see cref="IFrame.ChildFrames"/> methods.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var frame = page.Frames.FirstOrDefault(frame => frame.Name == "myframe");
    /// var text = await frame.QuerySelectorEvalAsync(".selector", "element => element.textContent");
    /// Console.WriteLine(text);
    /// ]]>
    /// </code>
    /// </example>
    public interface IFrame
    {
        /// <summary>
        /// Child frames of the this frame.
        /// </summary>
        IFrame[] ChildFrames { get; }

        /// <summary>
        /// Gets the frame's name attribute as specified in the tag.
        /// If the name is empty, returns the id attribute instead.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the frame's url.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Gets the parent <see cref="IFrame"/>, if any. Detached frames and main frames return <c>null</c>.
        /// </summary>
        IFrame ParentFrame { get; }

        /// <summary>
        /// Gets a value indicating if the frame is detached or not.
        /// </summary>
        bool Detached { get; }

        /// <summary>
        /// FrameID.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Returns page's title.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the action completed, yielding the frame's title.</returns>
        /// <seealso cref="IPage.GetTitleAsync"/>
        Task<string> GetTitleAsync();

        /// <summary>
        /// Navigates to an URL.
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="options">Extra options.</param>
        /// <returns>A <see cref="Task{IResponse}"/> that completes with resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// </returns>
        /// <remarks>
        /// <see cref="IFrame.GoToAsync(string, GoToOptions)"/> will throw an error if:
        /// * There's an SSL error (e.g. in case of self-signed certificates).
        /// * Target URL is invalid.
        /// * The timeout is exceeded during navigation.
        /// * The remote server does not respond or is unreachable.
        /// * The main resource failed to load.
        /// <para/>
        /// <see cref="IFrame.GoToAsync(string, GoToOptions)"/> will not throw an error when any valid HTTP status code is returned by the remote server, including 404 "Not Found" and 500 "Internal Server Error".
        /// The status code for such responses can be retrieved by calling response.status().
        /// <para/>
        /// NOTE <see cref="IFrame.GoToAsync(string, GoToOptions)"/> either throws an error or returns a main resource response.
        /// The only exceptions are navigation to about:blank or navigation to the same URL with a different hash, which would succeed and return null.
        /// <para/>
        /// NOTE Headless mode doesn't support navigation to a PDF document. See the upstream issue.
        /// </remarks>
        Task<IResponse> GoToAsync(string url, GoToOptions options = null);

        /// <summary>
        /// Navigates to an URL.
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="waitUntil">When to consider navigation succeeded.</param>
        /// <returns>A <see cref="Task{IResponse}"/> that completes with resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// </returns>
        /// <remarks>
        /// <see cref="IFrame.GoToAsync(string, GoToOptions)"/> will throw an error if:
        /// * There's an SSL error (e.g. in case of self-signed certificates).
        /// * Target URL is invalid.
        /// * The timeout is exceeded during navigation.
        /// * The remote server does not respond or is unreachable.
        /// * The main resource failed to load.
        /// <para/>
        /// <see cref="IFrame.GoToAsync(string, GoToOptions)"/> will not throw an error when any valid HTTP status code is returned by the remote server, including 404 "Not Found" and 500 "Internal Server Error".
        /// The status code for such responses can be retrieved by calling response.status().
        /// <para/>
        /// NOTE <see cref="IFrame.GoToAsync(string, GoToOptions)"/> either throws an error or returns a main resource response.
        /// The only exceptions are navigation to about:blank or navigation to the same URL with a different hash, which would succeed and return null.
        /// <para/>
        /// NOTE Headless mode doesn't support navigation to a PDF document. See the upstream issue.
        /// </remarks>
        Task<IResponse> GoToAsync(string url, WaitUntilNavigation waitUntil);

        /// <summary>
        /// Sets the HTML markup to the frame.
        /// </summary>
        /// <param name="html">HTML markup to assign to the page.</param>
        /// <param name="options">The navigations options.</param>
        /// <returns>A <see cref="Task"/> that completes when the javascript code executing injected the HTML finishes.</returns>
        /// <seealso cref="IPage.SetContentAsync(string, NavigationOptions)"/>
        Task SetContentAsync(string html, NavigationOptions options = null);

        /// <summary>
        /// Gets the full HTML contents of the page, including the doctype.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the html content is retrieved, yielding the HTML content.</returns>
        Task<string> GetContentAsync();

        /// <summary>
        /// Adds a <c><![CDATA[<script>]]></c> tag into the page with the desired url or content.
        /// </summary>
        /// <param name="options">add script tag options.</param>
        /// <remarks>
        /// Shortcut for <c>page.MainFrame.AddScriptTagAsync(options)</c>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the tag is added, yielding the added tag as an <see cref="IElementHandle"/> when the script's onload fires or when the script content was injected into frame.</returns>
        Task<IElementHandle> AddScriptTagAsync(AddTagOptions options);

        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync{T}(string, object[])"/>
        /// <returns>Task that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(string script, params object[] args);

        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync(string, object[])"/>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script as an row Json element.</returns>
        Task<JsonElement?> EvaluateAsync(string script, params object[] args);

        /// <summary>
        /// Executes a function that returns a <see cref="IJSHandle"/>.
        /// </summary>
        /// <param name="script">Function to be evaluated in the frame context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script as a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args);

        /// <summary>
        /// <![CDATA[
        /// This method focuses the element and triggers an input event after filling. If there's no text <input>, <textarea> or [contenteditable] element matching selector, the method throws an error.
        /// ]]>
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="text"><![CDATA[Value to fill for the <input>, <textarea> or [contenteditable] element]]></param>
        /// <param name="options">Optional waiting parameters.</param>
        /// <returns>A <see cref="Task"/> that completes when the fill message is confirmed by the browser.</returns>
        Task FillAsync(string selector, string text, WaitForSelectorOptions options = null);

        /// <summary>
        /// Waits for a selector to be added to the DOM.
        /// </summary>
        /// <param name="selector">A selector of an element to wait for.</param>
        /// <param name="options">Optional waiting parameters.</param>
        /// <returns>A <see cref="Task"/> that completes when element specified by selector string is added to DOM, yielding the <see cref="IElementHandle"/> to wait for.
        /// Resolves to `null` if waiting for `hidden: true` and selector is not found in DOM.</returns>
        Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null);

        /// <summary>
        /// Waits for a selector to be added to the DOM.
        /// </summary>
        /// <param name="selector">A selector of an element to wait for.</param>
        /// <param name="script">Function to execute when the selector is found.</param>
        /// <param name="options">Optional waiting parameters.</param>
        /// <param name="args">Arguments to be passed.</param>
        /// <returns>A <see cref="Task"/> that completes when element specified by selector string is added to DOM, yielding the <see cref="IElementHandle"/> to wait for.
        /// Resolves to `null` if waiting for `hidden: true` and selector is not found in DOM.</returns>
        Task<IJSHandle> WaitForSelectorEvaluateAsync(string selector, string script, WaitForFunctionOptions options = null, params object[] args);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="options">Wait options.</param>
        /// <param name="args">Arguments to pass to <c>script</c>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string pageFunction, WaitForFunctionOptions options = null, params object[] args);

        /// <summary>
        /// Queries frame for the selector. If there's no such element within the frame, the method will resolve to <c>null</c>.
        /// </summary>
        /// <param name="selector">Selector to query frame for.</param>
        /// <returns>A <see cref="Task"/> that completes when the selector is found (or failed), yielding the <see cref="IElementHandle"/> pointing to the frame element.</returns>
        /// <seealso cref="IPage.QuerySelectorAsync(string)"/>
        Task<IElementHandle> QuerySelectorAsync(string selector);

        /// <summary>
        /// The method runs <c>Array.from(document.querySelectorAll(selector))</c> within the page.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the javascript function finishes, yielding an array of <see cref="IElementHandle"/>.
        /// </returns>
        Task<IElementHandle[]> QuerySelectorAllAsync(string selector);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the frame and passes it as the first argument to pageFunction.
        /// </summary>
        /// <param name="selector">A selector to query frame for.</param>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task QuerySelectorAllEvaluateAsync(string selector, string script, params object[] args);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the frame and passes it as the first argument to pageFunction.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query frame for.</param>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, params object[] args);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="IPage.Mouse"/> to click in the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to click. If there are multiple elements satisfying the selector, the first will be clicked.</param>
        /// <param name="options">click options.</param>
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully clicked.</returns>
        Task ClickAsync(string selector, ClickOptions options = null);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="IPage.Mouse"/> to double click in the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to click. If there are multiple elements satisfying the selector, the first will be clicked.</param>
        /// <param name="options">click options.</param>
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully double clicked.</returns>
        Task DoubleClickAsync(string selector, ClickOptions options = null);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="IPage.Mouse"/> to triple click in the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to click. If there are multiple elements satisfying the selector, the first will be clicked.</param>
        /// <param name="options">click options.</param>
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully triple clicked.</returns>
        Task TripleClickAsync(string selector, ClickOptions options = null);

        /// <summary>
        /// This method runs document.querySelector within the page and passes it as the first argument to pageFunction.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task QuerySelectorEvaluateAsync(string selector, string script, params object[] args);

        /// <summary>
        /// This method runs document.querySelector within the page and passes it as the first argument to pageFunction.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, params object[] args);

        /// <summary>
        /// This resolves when the frame navigates to a new URL or reloads.
        /// It is useful for when you run code which will indirectly cause the page to navigate.
        /// </summary>
        /// <param name="options">navigation options.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        /// <remarks>
        /// Usage of the <c>History API</c> <see href="https://developer.mozilla.org/en-US/docs/Web/API/History_API"/> to change the URL is considered a navigation.
        /// </remarks>
        Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null, CancellationToken token = default);

        /// <summary>
        /// This resolves when the frame navigates to a new URL or reloads.
        /// It is useful for when you run code which will indirectly cause the page to navigate.
        /// </summary>
        /// <param name="waitUntil">When to consider navigation succeeded.</param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        /// <remarks>
        /// Usage of the <c>History API</c> <see href="https://developer.mozilla.org/en-US/docs/Web/API/History_API"/> to change the URL is considered a navigation.
        /// </remarks>
        Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/> and focuses it.
        /// </summary>
        /// <param name="selector">A selector to search for element to focus. If there are multiple elements satisfying the selector, the first will be focused.</param>
        /// <param name="options">Wait options.</param>
        /// <returns>A <see cref="Task"/> that completes when the the element matching <paramref name="selector"/> is successfully focused.</returns>
        Task FocusAsync(string selector, WaitForSelectorOptions options);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="IPage.Mouse"/> to hover over the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to hover. If there are multiple elements satisfying the selector, the first will be hovered.</param>
        /// <param name="options">Wait options.</param>
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully hovered.</returns>
        Task HoverAsync(string selector, WaitForSelectorOptions options = null);

        /// <summary>
        /// Sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>, and <c>keyup</c> event for each character in the text.
        /// </summary>
        /// <param name="selector">A selector of an element to type into. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="text">A text to type into a focused element.</param>
        /// <param name="options">Type options.</param>
        /// <remarks>
        /// To press a special key, like <c>Control</c> or <c>ArrowDown</c> use <see cref="IKeyboard.PressAsync(string, PressOptions)"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// await page.TypeAsync("#mytextarea", "Hello"); // Types instantly
        /// await page.TypeAsync("#mytextarea", "World", new TypeOptions { Delay = 100 }); // Types slower, like a user
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/> that completes when the type message is confirmed by the browser.</returns>
        Task TypeAsync(string selector, string text, TypeOptions options = null);

        /// <summary>
        /// Completes when the page reaches a required load state, load by default.
        /// The navigation can be in progress when it is called.
        /// If navigation is already at a required state, completes immediately.
        /// </summary>
        /// <param name="options">Extra options.</param>
        /// <returns>A <see cref="Task"/> that completes when the load is completed.</returns>
        Task WaitForLoadStateAsync(NavigationOptions options = null);

        /// <summary>
        /// Adds a <c><![CDATA[<link rel="stylesheet">]]></c> tag into the page with the desired url or a <c><![CDATA[<link rel="stylesheet">]]></c> tag with the content.
        /// </summary>
        /// <param name="options">add style tag options.</param>
        /// <returns>A <see cref="Task"/> that completes when the stylesheet's onload fires or when the CSS content was injected into frame, yieling the added <see cref="IElementHandle"/>.</returns>
        Task<IElementHandle> AddStyleTagAsync(AddTagOptions options);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectAsync(string selector, params string[] values);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectAsync(string selector, params SelectOption[] values);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectAsync(string selector, params IElementHandle[] values);
    }
}
