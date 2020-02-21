namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IPage.AddScriptTagAsync(AddTagOptions)"/> options.
    /// </summary>
    public class AddTagOptions
    {
        /// <summary>
        /// Gets or sets the url of a script to be added.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the path to the JavaScript file to be injected into frame. If its a relative path, then it is resolved relative to <see cref="System.IO.Directory.GetCurrentDirectory"/>.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the raw JavaScript content to be injected into frame.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the script type. Use <c>module</c> in order to load a Javascript ES6 module.
        /// </summary>
        /// <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/script"/>
        public string Type { get; set; }
    }
}
