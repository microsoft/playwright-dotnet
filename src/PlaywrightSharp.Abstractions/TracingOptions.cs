using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Tracing options used on <see cref="IBrowser.StartTracingAsync(TracingOptions)"/>.
    /// </summary>
    public class TracingOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether Tracing should captures screenshots in the trace.
        /// </summary>
        public bool Screenshots { get; set; }

        /// <summary>
        /// A path to write the trace file to.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Specify custom categories to use instead of default.
        /// </summary>
        public List<string> Categories { get; set; }
    }
}
