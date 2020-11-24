namespace PlaywrightSharp.Har
{
    /// <summary>
    /// Resulting model of the file generated using <see cref="BrowserContextOptions.RecordHar"/>.
    /// </summary>
    public class HarResult
    {
        /// <summary>
        /// Log.
        /// </summary>
        public HarLog Log { get; set; }
    }
}
