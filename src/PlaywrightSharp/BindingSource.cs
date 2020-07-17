namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IBrowserContext.ExposeBindingAsync(string, System.Action{BindingSource})"/> caller information.
    /// </summary>
    public class BindingSource
    {
        /// <summary>
        /// Browser Context.
        /// </summary>
        public BrowserContext Context { get; set; }

        /// <summary>
        /// Page.
        /// </summary>
        public Page Page { get; set; }

        /// <summary>
        /// Frame.
        /// </summary>
        public Frame Frame { get; set; }
    }
}