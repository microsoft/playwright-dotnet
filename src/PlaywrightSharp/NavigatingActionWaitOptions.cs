namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IPage.FillAsync(string, string, NavigatingActionWaitOptions)"/>, <see cref="IFrame.FillAsync(string, string, NavigatingActionWaitOptions)"/>.
    /// </summary>
    public class NavigatingActionWaitOptions
    {
        /// <summary>
        /// Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.
        /// </summary>
        public bool NoWaitAfter { get; set; }
    }
}
