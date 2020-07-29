namespace PlaywrightSharp
{
    /// <summary>
    /// Chéck options. See <see cref="IPage.CheckAsync(string, CheckOptions)"/>.
    /// </summary>
    public class CheckOptions
    {
        /// <summary>
        /// Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// Whether to pass the accionability checks.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.
        /// </summary>
        public bool NoWaitAfter { get; set; }
    }
}
