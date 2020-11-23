namespace PlaywrightSharp
{
    /// <summary>
    /// See <seealso cref="BrowserContextOptions.RecordHar"/>.
    /// </summary>
    public class RecordHarOptions
    {
        /// <summary>
        /// Path on the filesystem to write the HAR file to.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Optional setting to control whether to omit request content from the HAR. Defaults to false.
        /// </summary>
        public bool OmitContent { get; set; }
    }
}
