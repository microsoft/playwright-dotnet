using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Browser descriptor.
    /// </summary>
    public class BrowserDescriptor
    {
        /// <summary>
        /// Browser.
        /// </summary>
        public Browser Browser { get; set; }

        /// <summary>
        /// Browser Revision.
        /// </summary>
        public string Revision { get; set; }
    }
}
