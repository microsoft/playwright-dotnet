namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IPage.GoToAsync(string, GoToOptions)"/> and <see cref="IFrame.GoToAsync(string, GoToOptions)"/>.
    /// </summary>
    public class GoToOptions : NavigationOptions
    {
        /// <summary>
        /// Referer header value. If provided it will take prefrence over the referer header value set by <see cref="IPage.SetExtraHttpHeadersAsync(System.Collections.Generic.IReadOnlyDictionary{string, string})"/>.
        /// </summary>
        /// <value>The referer.</value>
        public string Referer { get; set; }
    }
}
