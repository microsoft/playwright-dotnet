namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IPage.AddScriptTagAsync(AddTagOptions)"/> options.
    /// </summary>
    public class AddTagOptions
    {
        /// <summary>
        /// Raw JavaScript content to be injected into frame.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Url of a script to be added.
        /// </summary>
        public string Url { get; set; }
    }
}
