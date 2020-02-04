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
        /// An <see cref="IFrame"/> that initiated this request, or null if navigating to error pages.
        /// </summary>
        public IFrame Frame { get; }
    }
}
