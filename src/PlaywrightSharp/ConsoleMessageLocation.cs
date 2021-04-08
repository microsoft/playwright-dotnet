using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Console message location.
    /// </summary>
    internal class ConsoleMessageLocation
    {
        /// <summary>
        /// URL of the resource if known.
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// 0-based line number in the resource if known.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 0-based column number in the resource if known.
        /// </summary>
        public int ColumnNumber { get; set; }
    }
}
