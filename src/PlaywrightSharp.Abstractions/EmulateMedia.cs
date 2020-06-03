namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IPage.EmulateMediaAsync(EmulateMedia)"/>.
    /// </summary>
    public class EmulateMedia
    {
        /// <summary>
        /// Changes the CSS media type of the page. Passing null disables CSS media emulation.
        /// </summary>
        public MediaType? Media { get; set; }

        /// <summary>
        /// Emulates 'prefers-colors-scheme' media feature.
        /// </summary>
        public ColorScheme? ColorScheme { get; set; }
    }
}
