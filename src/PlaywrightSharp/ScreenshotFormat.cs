namespace PlaywrightSharp
{
    /// <summary>
    /// Screenshot file format.
    /// </summary>
    /// <seealso cref="IPage.ScreenshotAsync(string, bool, Rect, bool, ScreenshotFormat?, int?, int?)"/> and <seealso cref="IElementHandle.ScreenshotAsync(string, bool, ScreenshotFormat?, int?, int?)"/>.
    public enum ScreenshotFormat
    {
        /// <summary>
        /// JPEG type.
        /// </summary>
        Jpeg,

        /// <summary>
        /// PNG type.
        /// </summary>
        Png,
    }
}
