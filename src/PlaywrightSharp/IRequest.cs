using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
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
        string PostData { get; }

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
        IRequest[] RedirectChain { get; }

        /// <summary>
        /// Gets or sets the failure.
        /// </summary>
        string Failure { get; }

        /// <summary>
        /// Responsed attached to the request.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the response is resolved.</returns>
        Task<IResponse> GetResponseAsync();

        /// <summary>
        /// Returns the parsed request's body for form-urlencoded and JSON as a fallback if any.
        /// </summary>
        /// <param name="options">Parser options.</param>
        /// <returns>A <see cref="Task"/> that completes when the json body is parsed, yielding a <see cref="JsonDocument"/> representation of request body.</returns>
        JsonDocument GetJsonAsync(JsonDocumentOptions options = default);

        /// <summary>
        /// Returns the parsed request's body for form-urlencoded and JSON as a fallback if any.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="options">Parser options.</param>
        /// <returns>A <see cref="Task"/> that completes when the json body is parsed, yielding a <typeparamref name="T"/> representation of request body.</returns>
        T GetJsonAsync<T>(JsonSerializerOptions options = null);
    }
}
