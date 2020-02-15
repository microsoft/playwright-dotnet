namespace PlaywrightSharp
{
    /// <summary>
    /// Target class.
    /// </summary>
    public interface ITarget
    {
        /// <summary>
        /// Gets the URL.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Gets the type. It will be <see cref="ITarget.Type"/>.
        /// Can be `"page"`, `"background_page"`, `"service_worker"`, `"shared_worker"`, `"browser"` or `"other"`.
        /// </summary>
        /// <value>The type.</value>
        TargetType Type { get; }
    }
}
