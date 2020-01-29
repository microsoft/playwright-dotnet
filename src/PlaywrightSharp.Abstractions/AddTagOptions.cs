namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IPage.AddScriptTagAsync(AddTagOptions)"/> options.
    /// </summary>
    public class AddTagOptions
    {
        /// <summary>
        /// Raw JavaScript content to be injected into frame
        /// </summary>
        public string Content { get; set; }
    }
}