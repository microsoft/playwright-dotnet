namespace PlaywrightSharp
{
    /// <summary>
    /// JS Coverage function info. See <see cref="JSCoverageEntry.Functions"/>.
    /// </summary>
    public class JSCoverageFunction
    {
        /// <summary>
        /// Function Name.
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Ranges.
        /// </summary>
        public JSCoverageEntryRange[] Ranges { get; set; }
    }
}
