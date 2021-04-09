using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    public partial interface IPage
    {
        /// <summary><para>Returns frame with matching URL.</para></summary>
        /// <param name="urlString">
        /// A glob pattern, regex pattern or predicate receiving frame's <c>url</c> as a <see
        /// cref="URL"/> object.
        /// </param>
        /// <returns>Matching frame.</returns>
        IFrame FrameByUrl(string urlString);

        /// <summary><para>Returns frame with matching URL.</para></summary>
        /// <param name="urlRegex">
        /// A glob pattern, regex pattern or predicate receiving frame's <c>url</c> as a <see
        /// cref="URL"/> object.
        /// </param>
        /// <returns>Matching frame.</returns>
        IFrame FrameByUrl(Regex urlRegex);

        /// <summary><para>Returns frame with matching URL.</para></summary>
        /// <param name="urlFunc">
        /// A glob pattern, regex pattern or predicate receiving frame's <c>url</c> as a <see
        /// cref="URL"/> object.
        /// </param>
        /// <returns>Matching frame.</returns>
        IFrame FrameByUrl(Func<string, bool> urlFunc);

        /// <summary>
        /// Setup media emulation.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task EmulateMediaAsync();

        /// <summary>
        /// Setup media emulation.
        /// </summary>
        /// <param name="colorScheme">
        /// Emulates <c>'prefers-colors-scheme'</c> media feature, supported values are <c>'light'</c>,
        /// <c>'dark'</c>, <c>'no-preference'</c>. Passing <c>null</c> disables color scheme
        /// emulation.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task EmulateMediaAsync(ColorScheme colorScheme);

        /// <summary>
        /// <para>Returns the return value of <paramref name="expression"/>.</para>
        /// <para>
        /// If the function passed to the <see cref="IFrame.EvaluateAsync"/> returns a <see
        /// cref="Promise"/>, then <see cref="IFrame.EvaluateAsync"/> would wait for the promise
        /// to resolve and return its value.
        /// </para>
        /// <para>
        /// If the function passed to the <see cref="IFrame.EvaluateAsync"/> returns a non-<see
        /// cref="Serializable"/> value, then <see cref="IFrame.EvaluateAsync"/> returns <c>undefined</c>.
        /// Playwright also supports transferring some additional values that are not serializable
        /// by <c>JSON</c>: <c>-0</c>, <c>NaN</c>, <c>Infinity</c>, <c>-Infinity</c>.
        /// </para>
        /// <para>A string can also be passed in instead of a function.</para>
        /// <para>
        /// <see cref="IElementHandle"/> instances can be passed as an argument to the <see
        /// cref="IFrame.EvaluateAsync"/>.
        /// </para>
        /// </summary>
        /// <param name="expression">
        /// JavaScript expression to be evaluated in the browser context. If it looks like a
        /// function declaration, it is interpreted as a function. Otherwise, evaluated as an
        /// expression.
        /// </param>
        /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
        /// <returns>A <see cref="Task"/> that will resolve when the evaluate function is executed by the browser.</returns>
        public Task<JsonElement?> EvaluateAsync(string expression, object arg = default);

        /// <summary>
        /// <para>Returns the return value of <paramref name="expression"/>.</para>
        /// <para>
        /// The method finds all elements matching the specified selector within the frame and
        /// passes an array of matched elements as a first argument to <paramref name="expression"/>.
        /// See <a href="./selectors.md">Working with selectors</a> for more details.
        /// </para>
        /// <para>
        /// If <paramref name="expression"/> returns a <see cref="Promise"/>, then <see cref="IFrame.EvalOnSelectorAllAsync"/>
        /// would wait for the promise to resolve and return its value.
        /// </para>
        /// <para>Examples.</para>
        /// </summary>
        /// <param name="selector">
        /// A selector to query for. See <a href="./selectors.md">working with selectors</a>
        /// for more details.
        /// </param>
        /// <param name="expression">
        /// JavaScript expression to be evaluated in the browser context. If it looks like a
        /// function declaration, it is interpreted as a function. Otherwise, evaluated as an
        /// expression.
        /// </param>
        /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
        /// <returns>A <see cref="Task"/> that will resolve when the evaluate function is executed by the browser.</returns>
        public Task EvalOnSelectorAllAsync(string selector, string expression, object arg);

        /// <summary>
        /// <para>Returns the return value of <paramref name="expression"/>.</para>
        /// <para>
        /// The method finds an element matching the specified selector within the frame and
        /// passes it as a first argument to <paramref name="expression"/>. See <a href="./selectors.md">Working
        /// with selectors</a> for more details. If no elements match the selector, the method
        /// throws an error.
        /// </para>
        /// <para>
        /// If <paramref name="expression"/> returns a <see cref="Promise"/>, then <see cref="IFrame.EvalOnSelectorAsync"/>
        /// would wait for the promise to resolve and return its value.
        /// </para>
        /// <para>Examples.</para>
        /// </summary>
        /// <param name="selector">
        /// A selector to query for. See <a href="./selectors.md">working with selectors</a>
        /// for more details.
        /// </param>
        /// <param name="expression">
        /// JavaScript expression to be evaluated in the browser context. If it looks like a
        /// function declaration, it is interpreted as a function. Otherwise, evaluated as an
        /// expression.
        /// </param>
        /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
        /// <returns>A <see cref="Task"/> that will resolve when the evaluate function is executed by the browser.</returns>
        Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = default);

        /// <summary>
        /// <para>
        /// Waits for the frame navigation and returns the main resource response. In case of
        /// multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage,
        /// the navigation will resolve with <c>null</c>.
        /// </para>
        /// <para>
        /// This method waits for the frame to navigate to a new URL. It is useful for when
        /// you run code which will indirectly cause the frame to navigate. Consider this example.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// Usage of the <a href="https://developer.mozilla.org/en-US/docs/Web/API/History_API">History
        /// API</a> to change the URL is considered a navigation.
        /// </para>
        /// </remarks>
        /// <param name="waitUntil">
        /// When to consider operation succeeded, defaults to <c>load</c>. Events can be either.
        /// <list type="bullet">
        /// <item><description>
        /// <c>'domcontentloaded'</c> - consider operation to be finished when the <c>DOMContentLoaded</c>
        /// event is fired.
        /// </description></item>
        /// <item><description>
        /// <c>'load'</c> - consider operation to be finished when the <c>load</c> event is
        /// fired.
        /// </description></item>
        /// <item><description>
        /// <c>'networkidle'</c> - consider operation to be finished when there are no network
        /// connections for at least <c>500</c> ms.
        /// </description></item>
        /// </list>
        /// </param>
        /// <param name="timeout">
        /// Maximum operation time in mi3lliseconds, defaults to 30 seconds, pass <c>0</c> to
        /// disable timeout. The default value can be changed by using the <see cref="IBrowserContext.SetDefaultNavigationTimeout"/>,
        /// <see cref="IBrowserContext.SetDefaultTimeout"/>, <see cref="IPage.SetDefaultNavigationTimeout"/>
        /// or <see cref="IPage.SetDefaultTimeout"/> methods.
        /// </param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        Task<IResponse> WaitForNavigationAsync(WaitUntilState waitUntil, float? timeout = default);

        /// <summary>
        /// <para>
        /// Waits for the frame navigation and returns the main resource response. In case of
        /// multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage,
        /// the navigation will resolve with <c>null</c>.
        /// </para>
        /// <para>
        /// This method waits for the frame to navigate to a new URL. It is useful for when
        /// you run code which will indirectly cause the frame to navigate. Consider this example.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// Usage of the <a href="https://developer.mozilla.org/en-US/docs/Web/API/History_API">History
        /// API</a> to change the URL is considered a navigation.
        /// </para>
        /// </remarks>
        /// <param name="urlString">
        /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
        /// while waiting for the navigation.
        /// </param>
        /// <param name="waitUntil">
        /// When to consider operation succeeded, defaults to <c>load</c>. Events can be either:
        /// <list type="bullet">
        /// <item><description>
        /// <c>'domcontentloaded'</c> - consider operation to be finished when the <c>DOMContentLoaded</c>
        /// event is fired.
        /// </description></item>
        /// <item><description>
        /// <c>'load'</c> - consider operation to be finished when the <c>load</c> event is
        /// fired.
        /// </description></item>
        /// <item><description>
        /// <c>'networkidle'</c> - consider operation to be finished when there are no network
        /// connections for at least <c>500</c> ms.
        /// </description></item>
        /// </list>
        /// </param>
        /// <param name="timeout">
        /// Maximum operation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to
        /// disable timeout. The default value can be changed by using the <see cref="IBrowserContext.SetDefaultNavigationTimeout"/>,
        /// <see cref="IBrowserContext.SetDefaultTimeout"/>, <see cref="IPage.SetDefaultNavigationTimeout"/>
        /// or <see cref="IPage.SetDefaultTimeout"/> methods.
        /// </param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        Task<IResponse> WaitForNavigationAsync(string urlString, WaitUntilState waitUntil = default, float? timeout = default);

        /// <summary>
        /// <para>
        /// Waits for the frame navigation and returns the main resource response. In case of
        /// multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage,
        /// the navigation will resolve with <c>null</c>.
        /// </para>
        /// <para>
        /// This method waits for the frame to navigate to a new URL. It is useful for when
        /// you run code which will indirectly cause the frame to navigate. Consider this example.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// Usage of the <a href="https://developer.mozilla.org/en-US/docs/Web/API/History_API">History
        /// API</a> to change the URL is considered a navigation.
        /// </para>
        /// </remarks>
        /// <param name="urlRegex">
        /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
        /// while waiting for the navigation.
        /// </param>
        /// <param name="waitUntil">
        /// When to consider operation succeeded, defaults to <c>load</c>. Events can be either:
        /// <list type="bullet">
        /// <item><description>
        /// <c>'domcontentloaded'</c> - consider operation to be finished when the <c>DOMContentLoaded</c>
        /// event is fired.
        /// </description></item>
        /// <item><description>
        /// <c>'load'</c> - consider operation to be finished when the <c>load</c> event is
        /// fired.
        /// </description></item>
        /// <item><description>
        /// <c>'networkidle'</c> - consider operation to be finished when there are no network
        /// connections for at least <c>500</c> ms.
        /// </description></item>
        /// </list>
        /// </param>
        /// <param name="timeout">
        /// Maximum operation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to
        /// disable timeout. The default value can be changed by using the <see cref="IBrowserContext.SetDefaultNavigationTimeout"/>,
        /// <see cref="IBrowserContext.SetDefaultTimeout"/>, <see cref="IPage.SetDefaultNavigationTimeout"/>
        /// or <see cref="IPage.SetDefaultTimeout"/> methods.
        /// </param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        Task<IResponse> WaitForNavigationAsync(Regex urlRegex, WaitUntilState waitUntil = default, float? timeout = default);

        /// <summary>
        /// <para>
        /// Waits for the frame navigation and returns the main resource response. In case of
        /// multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage,
        /// the navigation will resolve with <c>null</c>.
        /// </para>
        /// <para>
        /// This method waits for the frame to navigate to a new URL. It is useful for when
        /// you run code which will indirectly cause the frame to navigate. Consider this example.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// Usage of the <a href="https://developer.mozilla.org/en-US/docs/Web/API/History_API">History
        /// API</a> to change the URL is considered a navigation.
        /// </para>
        /// </remarks>
        /// <param name="urlFunc">
        /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
        /// while waiting for the navigation.
        /// </param>
        /// <param name="waitUntil">
        /// When to consider operation succeeded, defaults to <c>load</c>. Events can be either:
        /// <list type="bullet">
        /// <item><description>
        /// <c>'domcontentloaded'</c> - consider operation to be finished when the <c>DOMContentLoaded</c>
        /// event is fired.
        /// </description></item>
        /// <item><description>
        /// <c>'load'</c> - consider operation to be finished when the <c>load</c> event is
        /// fired.
        /// </description></item>
        /// <item><description>
        /// <c>'networkidle'</c> - consider operation to be finished when there are no network
        /// connections for at least <c>500</c> ms.
        /// </description></item>
        /// </list>
        /// </param>
        /// <param name="timeout">
        /// Maximum operation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to
        /// disable timeout. The default value can be changed by using the <see cref="IBrowserContext.SetDefaultNavigationTimeout"/>,
        /// <see cref="IBrowserContext.SetDefaultTimeout"/>, <see cref="IPage.SetDefaultNavigationTimeout"/>
        /// or <see cref="IPage.SetDefaultTimeout"/> methods.
        /// </param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        Task<IResponse> WaitForNavigationAsync(Func<string, bool> urlFunc, WaitUntilState waitUntil = default, float? timeout = default);

        /// <inheritdoc cref="SetInputFilesAsync(string, IEnumerable{FilePayload}, bool?, float?)"/>
        Task SetInputFilesAsync(string selector, string files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SetInputFilesAsync(string, IEnumerable{FilePayload}, bool?, float?)"/>
        Task SetInputFilesAsync(string selector, IEnumerable<string> files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SetInputFilesAsync(string, IEnumerable{FilePayload}, bool?, float?)"/>
        Task SetInputFilesAsync(string selector, FilePayload files, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, string values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<string> values, bool? noWaitAfter = default, float? timeout = default);

        /// <summary>
        /// <para>Returns the array of option values that have been successfully selected.</para>
        /// <para>
        /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
        /// been selected. If there's no <c>&lt;select&gt;</c> element matching <paramref name="selector"/>,
        /// the method throws an error.
        /// </para>
        /// <para>Will wait until all specified options are present in the <c>&lt;select&gt;</c> element.</para>
        /// </summary>
        /// <param name="selector">
        /// A selector to query for. See <a href="./selectors.md">working with selectors</a>
        /// for more details.
        /// </param>
        /// <param name="noWaitAfter">
        /// Actions that initiate navigations are waiting for these navigations to happen and
        /// for pages to start loading. You can opt out of waiting via setting this flag. You
        /// would only need this option in the exceptional cases such as navigating to inaccessible
        /// pages. Defaults to <c>false</c>.
        /// </param>
        /// <param name="timeout">
        /// Maximum time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.SetDefaultTimeout"/>
        /// or <see cref="IPage.SetDefaultTimeout"/> methods.
        /// </param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IElementHandle values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, SelectOptionValue values, bool? noWaitAfter = default, float? timeout = default);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, params string[] values);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, params SelectOptionValue[] values);

        /// <inheritdoc cref="SelectOptionAsync(string, IEnumerable{SelectOptionValue}, bool?, float?)" />
        Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, params IElementHandle[] values);

        /// <summary><para>Waits for the matching request and returns it.</para></summary>
        /// <param name="urlOrPredicateString">Request URL string, regex or predicate receiving <see cref="IRequest"/> object.</param>
        /// <param name="timeout">
        /// Maximum wait time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable
        /// the timeout. The default value can be changed by using the <see cref="IPage.SetDefaultTimeout"/>
        /// method.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the request was made (or timeout), yielding the matching <see cref="IRequest"/>.</returns>
        Task<IRequest> WaitForRequestAsync(string urlOrPredicateString, float? timeout = default);

        /// <summary><para>Waits for the matching request and returns it.</para></summary>
        /// <param name="urlOrPredicateRegex">Request URL string, regex or predicate receiving <see cref="IRequest"/> object.</param>
        /// <param name="timeout">
        /// Maximum wait time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable
        /// the timeout. The default value can be changed by using the <see cref="IPage.SetDefaultTimeout"/>
        /// method.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the request was made (or timeout), yielding the matching <see cref="IRequest"/>.</returns>
        Task<IRequest> WaitForRequestAsync(Regex urlOrPredicateRegex, float? timeout = default);

        /// <summary><para>Waits for the matching request and returns it.</para></summary>
        /// <param name="urlOrPredicateFunc">Request URL string, regex or predicate receiving <see cref="IRequest"/> object.</param>
        /// <param name="timeout">
        /// Maximum wait time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable
        /// the timeout. The default value can be changed by using the <see cref="IPage.SetDefaultTimeout"/>
        /// method.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the request was made (or timeout), yielding the matching <see cref="IRequest"/>.</returns>
        Task<IRequest> WaitForRequestAsync(Func<IRequest, bool> urlOrPredicateFunc, float? timeout = default);

        /// <summary>
        /// <para>
        /// Waits for event to fire and passes its value into the predicate function. Returns
        /// when the predicate returns truthy value. Will throw an error if the page is closed
        /// before the event is fired. Returns the event data value.
        /// </para>
        /// </summary>
        /// <param name="pageEvent">Event name, same one typically passed into <c>*.on(event)</c>.</param>
        /// <param name="predicate">Receives the event data and resolves when the waiting should resolve.</param>
        /// <param name="timeout">
        /// Maximum time to wait for in milliseconds. Defaults to <c>30000</c> (30 seconds).
        /// Pass <c>0</c> to disable timeout. The default value can be changed by using the
        /// <see cref="IBrowserContext.SetDefaultTimeout"/>.
        /// </param>
        /// <typeparam name = "T" > Resulting event args.</typeparam>
        /// <returns>A <see cref="Task"/> that completes when the predicate returns truthy value. Yielding the information of the event.</returns>
        Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> pageEvent, Func<T, bool> predicate = default, float? timeout = default);
    }
}
