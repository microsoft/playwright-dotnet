using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// At every point of time, page exposes its current frame tree via the <see cref="IPage.MainFrame"/> and <see cref="IFrame.ChildFrames"/> methods.
    /// TODO
    /// </summary>
    public interface IFrame
    {
        /// <summary>
        /// Navigates to an URL
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <returns>A <see cref="Task{IResponse}"/> that completes with resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// </returns>
        /// <remarks>
        /// <see cref="IFrame.GoToAsync(string)"/> will throw an error if:
        /// * There's an SSL error (e.g. in case of self-signed certificates).
        /// * Target URL is invalid.
        /// * The timeout is exceeded during navigation.
        /// * The remote server does not respond or is unreachable.
        /// * The main resource failed to load.
        ///
        /// <see cref="IFrame.GoToAsync(string)"/> will not throw an error when any valid HTTP status code is returned by the remote server, including 404 "Not Found" and 500 "Internal Server Error".
        /// The status code for such responses can be retrieved by calling response.status().
        ///
        /// NOTE <see cref="IFrame.GoToAsync(string)"/> either throws an error or returns a main resource response.
        /// The only exceptions are navigation to about:blank or navigation to the same URL with a different hash, which would succeed and return null.
        ///
        /// NOTE Headless mode doesn't support navigation to a PDF document. See the upstream issue.
        /// </remarks>
        Task<IResponse> GoToAsync(string url);

        /// <summary>
        /// Child frames of the this frame
        /// </summary>
        IFrame[] ChildFrames { get; }
    }
}