namespace PlaywrightSharp
{
    /// <summary>
    /// JS Coverage report. See <seealso cref="ICoverage.StopJSCoverageAsync"/>.
    /// </summary>
    public class JSCoverageEntry
    {
        /// <summary>
        /// Script URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Script Source.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// V8-specific coverage format.
        /// </summary>
        public JSCoverageFunction[] Functions { get; set; }
    }
}
