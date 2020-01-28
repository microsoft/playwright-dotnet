using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Page provides methods to interact with a single tab or extension background page in Chromium. One Browser instance might have multiple Page instances.
    /// </summary>
    /// <example>
    /// This example creates a page and navigates it to a URL:
    /// <code>
    /// <![CDATA[
    /// var context = await browser.NewContextAsync();
    /// const page = await context.NewPageAsync("https://example.com");
    /// await browser.CloseAsync();
    /// ]]>
    /// </code>
    /// </example>
    public interface IPage
    {
        /// <summary>
        /// Get the browser context that the page belongs to.
        /// </summary>
        IBrowserContext BrowserContext { get; }

        /// <summary>
        /// Navigates to an url
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout. </param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="WaitUntilNavigation.Load"/>. Given an array of <see cref="WaitUntilNavigation"/>, navigation is considered to be successful after all events have been fired</param>
        /// <returns>Task which resolves to the main resource response. In case of multiple redirects, the navigation will resolve with the response of the last redirect</returns>
        /// <seealso TODOcref="GoToAsync(string, NavigationOptions)"/>
        Task<Response> GoToAsync(string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null);

        /// <summary>
        /// Executes a script in browser context
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso TODOcref="EvaluateFunctionAsync{T}(string, object[])"/>
        /// <returns>Task which resolves to script return value</returns>
        Task<object> EvaluateExpressionAsync(string script);

        /// <summary>
        /// Executes a script in browser context
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso TODOcref="EvaluateFunctionAsync{T}(string, object[])"/>
        /// <returns>Task which resolves to script return value</returns>
        Task<T> EvaluateExpressionAsync<T>(string script);
    }
}
