namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IPage.CloseAsync(PageCloseOptions)"/>.
    /// </summary>
    public class PageCloseOptions
    {
        /// <summary>
        /// Defaults to <c>false</c>. Whether to run the beforeunload page handlers.
        /// </summary>
        /// <see href="https://developer.mozilla.org/en-US/docs/Web/Events/beforeunload"/>
        public bool RunBeforeUnload { get; set; }
    }
}