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

        /// <summary>
        /// Specifies clipping region of the page.
        /// </summary>
        public Clip Clip { get; set; }

        /// <summary>
        /// Hides default white background and allows capturing screenshots with transparency. Defaults to <c>false</c>.
        /// </summary>
        public bool OmitBackground { get; set; }

        /// <summary>
        /// Specify screenshot type, can be either jpeg or png. Defaults to 'png'.
        /// </summary>
        /// <value>The type.</value>
        public ScreenshotFormat? Type { get; set; }

        /// <summary>
        /// The quality of the image, between 0-100. Not applicable to png images.
        /// </summary>
        public int? Quality { get; set; }
    }
}
