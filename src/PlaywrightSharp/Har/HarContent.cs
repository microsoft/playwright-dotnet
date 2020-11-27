namespace PlaywrightSharp.Har
{
    /// <summary>
    /// HAR Content.
    /// </summary>
    public class HarContent
    {
        /// <summary>
        /// Encoding.
        /// </summary>
        public string Encoding { get; set; }

        /// <summary>
        /// Mime Type.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Size.
        /// </summary>
        public decimal Size { get; set; }

        /// <summary>
        /// Compression.
        /// </summary>
        public decimal? Compression { get; set; }
    }
}
