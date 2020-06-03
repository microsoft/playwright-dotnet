namespace PlaywrightSharp
{
    /// <summary>
    /// Optional waiting parameters.
    /// </summary>
    /// <seealso cref="IPage.WaitForSelectorAsync(string, WaitForSelectorOptions)"/>
    /// <seealso cref="IFrame.WaitForSelectorAsync(string, WaitForSelectorOptions)"/>
    public class WaitForSelectorOptions
    {
        /// <summary>
        /// Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Wait for element to become visible (visible), hidden (hidden), present in dom (any) or do not wait at all (nowait). Defaults to <see cref="WaitForOption.Visible"/>.
        /// </summary>
        public WaitForOption WaitFor { get; set; }
    }
}
