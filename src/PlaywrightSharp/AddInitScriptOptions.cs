namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IPage.AddInitScriptAsync(AddInitScriptOptions, object[])"/> and <see cref="IPage.AddInitScriptAsync(AddInitScriptOptions, object[])"/>.
    /// </summary>
    public class AddInitScriptOptions
    {
        /// <summary>
        /// Gets or sets the path to the JavaScript file to be injected into frame. If its a relative path, then it is resolved relative to <see cref="System.IO.Directory.GetCurrentDirectory"/>.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the raw JavaScript content to be injected into frame.
        /// </summary>
        public string Content { get; set; }
    }
}
