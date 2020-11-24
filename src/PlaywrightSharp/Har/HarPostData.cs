namespace PlaywrightSharp.Har
{
    /// <summary>
    /// HAR POST Data.
    /// </summary>
    public class HarPostData
    {
        /// <summary>
        /// Mime Type.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Post Params.
        /// </summary>
        public (string Name, string Value)[] Params { get; set; } = System.Array.Empty<(string Name, string Value)>();
    }
}
