namespace PlaywrightSharp
{
    /// <summary>
    /// Optional waiting parameters.
    /// </summary>
    /// <seealso cref="IPage.WaitForRequestAsync(string, WaitForOptions)"/>
    public class WaitForOptions
    {
        /// <summary>
        /// Maximum time to wait for in milliseconds. Pass 0 to disable timeout.
        /// The default value can be changed by setting the <see cref="IPage.DefaultTimeout"/> property.
        /// </summary>
        public int? Timeout { get; set; }
    }
}
