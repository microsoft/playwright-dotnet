using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Request delegate interface.
    /// </summary>
    internal interface IRequestDelegate
    {
        /// <summary>
        /// Aborts request. To use this, request interception should be enabled with <see cref="IPage.SetRequestInterceptionAsync(bool)"/>.
        /// Exception is immediately thrown if the request interception is not enabled.
        /// </summary>
        /// <param name="errorCode">Optional error code. Defaults to <see cref="RequestAbortErrorCode.Failed"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task AbortAsync(RequestAbortErrorCode errorCode = RequestAbortErrorCode.Failed);

        /// <summary>
        /// Continues request with optional request overrides. To use this, request interception should be enabled with <see cref="IPage.SetRequestInterceptionAsync(bool)"/>. Exception is immediately thrown if the request interception is not enabled.
        /// If the URL is set it won't perform a redirect. The request will be silently forwarded to the new url. For example, the address bar will show the original url.
        /// </summary>
        /// <param name="payload">Optional request overwrites.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task ContinueAsync(Payload payload = null);

        /// <summary>
        /// Fulfills request with given response. To use this, request interception should be enabled with <see cref="IPage.SetRequestInterceptionAsync(bool)"/>. Exception is thrown if request interception is not enabled.
        /// </summary>
        /// <param name="response">Response that will fulfill this request.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task FulfillAsync(ResponseData response);
    }
}
