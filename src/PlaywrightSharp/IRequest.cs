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
        /// When the server responds with a redirect, Playwright creates a new <see cref="IRequest"/> object.
        /// The two requests are connected by <see cref="IRequest.RedirectFrom"/> and <see cref="IRequest.RedirectTo"/> methods.
        /// When multiple server redirects has happened, it is possible to construct the whole redirect chain by repeatedly calling <see cref="RedirectFrom"/>.
        /// </summary>
        IRequest RedirectFrom { get; }

        /// <summary>
        /// When the server responds with a redirect, Playwright creates a new <see cref="IRequest"/> object.
        /// The two requests are connected by <see cref="IRequest.RedirectFrom"/> and <see cref="IRequest.RedirectTo"/> methods.
        /// When multiple server redirects has happened, it is possible to construct the whole redirect chain by repeatedly calling <see cref="RedirectFrom"/>..
        /// </summary>
        IRequest RedirectTo { get; }

        /// <summary>
        /// Gets or sets the failure.
        /// </summary>
        string Failure { get; }

        /// <summary>
        /// Responsed attached to the request.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the response is resolved.</returns>
        Task<IResponse> GetResponseAsync();
    }
}
