using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Whenever the page sends a request, the following events are emitted by an <see cref="IPage"/>.
    /// <see cref="IPage.Request"/> emitted when the request is issued by the page.
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// URL of the request.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Gets or sets the HTTP method.
        /// </summary>
        HttpMethod Method { get; }

        /// <summary>
        /// Gets or sets the HTTP headers.
        /// </summary>
        IDictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets or sets the post data.
        /// </summary>
        object PostData { get; }

        /// <summary>
        /// An <see cref="IFrame"/> that initiated this request, or null if navigating to error pages.
        /// </summary>
        IFrame Frame { get; }

        /// <summary>
        /// Gets whether this request is driving frame's navigation.
        /// </summary>
        bool IsNavigationRequest { get; }

        /// <summary>
        /// Gets or sets the type of the resource.
        /// </summary>
        ResourceType ResourceType { get; }

        /// <summary>
        /// A redirectChain is a chain of requests initiated to fetch a resource.
        /// If there are no redirects and the request was successful, the chain will be empty.
        /// If a server responds with at least a single redirect, then the chain will contain all the requests that were redirected.
        /// redirectChain is shared between all the requests of the same chain.
        /// </summary>
        /// <example>
        /// For example, if the website http://example.com has a single redirect to https://example.com, then the chain will contain one request:
        /// <code>
        /// var response = await page.GoToAsync("http://example.com");
        /// var chain = response.Request.RedirectChain;
        /// Console.WriteLine(chain.Length); // 1
        /// Console.WriteLine(chain[0].Url); // 'http://example.com'
        /// </code>
        /// If the website https://google.com has no redirects, then the chain will be empty:
        /// <code>
        /// var response = await page.GoToAsync("https://google.com");
        /// var chain = response.Request.RedirectChain;
        /// Console.WriteLine(chain.Length); // 0
        /// </code>
        /// </example>
        /// <value>The redirect chain.</value>
        public IRequest[] RedirectChain { get; }

        /// <summary>
        /// Responsed attached to the request.
        /// </summary>
        public IResponse Response { get; internal set; }

        /// <summary>
        /// Gets or sets the failure.
        /// </summary>
        string Failure { get; }

        /// <summary>
        /// Continues request with optional request overrides. To use this, request interception should be enabled with <see cref="IPage.SetRequestInterceptionAsync(bool)"/>. Exception is immediately thrown if the request interception is not enabled.
        /// If the URL is set it won't perform a redirect. The request will be silently forwarded to the new url. For example, the address bar will show the original url.
        /// </summary>
        /// <param name="overrides">Optional request overwrites.</param>
        /// <returns>Task.</returns>
        Task ContinueAsync(Payload overrides = null);

        /// <summary>
        /// Fulfills request with given response. To use this, request interception should be enabled with <see cref="IPage.SetRequestInterceptionAsync(bool)"/>. Exception is thrown if request interception is not enabled.
        /// </summary>
        /// <param name="response">Response that will fulfill this request.</param>
        /// <returns>Task.</returns>
        Task RespondAsync(ResponseData response);

        /// <summary>
        /// Aborts request. To use this, request interception should be enabled with <see cref="IPage.SetRequestInterceptionAsync(bool)"/>.
        /// Exception is immediately thrown if the request interception is not enabled.
        /// </summary>
        /// <param name="errorCode">Optional error code. Defaults to <see cref="RequestAbortErrorCode.Failed"/>.</param>
        /// <returns>Task.</returns>
        Task AbortAsync(RequestAbortErrorCode errorCode = RequestAbortErrorCode.Failed);
    }
}
