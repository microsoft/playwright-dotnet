namespace PlaywrightSharp
{
    /// <summary>
    /// See <seealso cref="BrowserContextOptions.RecordVideo"/>.
    /// </summary>
    public class RecordVideoOptions
    {
        /// <summary>
        /// Path to the directory to put videos into.
        /// </summary>
        public string Dir { get; set; }

        /// <summary>
        /// Optional dimensions of the recorded videos. If not specified the size will be equal to viewport.
        /// If viewport is not configured explicitly the video size defaults to 1280x720.
        /// Actual picture of each page will be scaled down if necessary to fit the specified size.
        /// </summary>
        public ViewportSize Size { get; set; }
    }
}