namespace PlaywrightSharp
{
    /// <summary>
    /// Target type.
    /// </summary>
    /// <seealso cref="ITarget.Type"/>
    public enum TargetType
    {
        /// <summary>
        /// The other.
        /// </summary>
        Other,

        /// <summary>
        /// Target type page.
        /// </summary>
        Page,

        /// <summary>
        /// Target type service worker.
        /// </summary>
        ServiceWorker,

        /// <summary>
        /// Target type browser.
        /// </summary>
        Browser,

        /// <summary>
        /// Target type background page.
        /// </summary>
        BackgroundPage,

        /// <summary>
        /// Target type worker.
        /// </summary>
        Worker,

        /// <summary>
        /// Target type javascript.
        /// </summary>
        Javascript,

        /// <summary>
        /// Target type network.
        /// </summary>
        Network,

        /// <summary>
        /// Target type Deprecation.
        /// </summary>
        Deprecation,

        /// <summary>
        /// Target type security.
        /// </summary>
        Security,

        /// <summary>
        /// Target type recommendation.
        /// </summary>
        Recommendation,

        /// <summary>
        /// Target type shared worker.
        /// </summary>
        SharedWorker,

        /// <summary>
        /// Target type iFrame.
        /// </summary>
        IFrame,
    }
}
