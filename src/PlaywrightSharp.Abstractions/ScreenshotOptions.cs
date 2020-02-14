namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IPage.ScreenshotAsync(ScreenshotOptions)"/> options.
    /// </summary>
    public class ScreenshotOptions
    {
        /// <summary>
        /// When <c>true</c>, takes a screenshot of the full scrollable page. Defaults to <c>false</c>.
        /// </summary>
        public bool FullPage { get; set; }
    }
}
