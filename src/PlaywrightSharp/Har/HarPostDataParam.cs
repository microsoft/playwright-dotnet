namespace PlaywrightSharp.Har
{
    /// <summary>
    /// POST data param.
    /// </summary>
    public class HarPostDataParam
    {
        /// <summary>
        /// Param Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Param value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Filename.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Content type.
        /// </summary>
        public string ContentType { get; set; }
    }
}