using System.Collections.Generic;

namespace PlaywrightSharp.Har
{
    /// <summary>
    /// HAR Log.
    /// </summary>
    public class HarLog
    {
        /// <summary>
        /// Version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Creator.
        /// </summary>
        public HarCreator Creator { get; set; }

        /// <summary>
        /// Browser.
        /// </summary>
        public HarBrowser Browser { get; set; }

        /// <summary>
        /// Page list.
        /// </summary>
        public IEnumerable<HarPage> Pages { get; set; }

        /// <summary>
        /// Entries.
        /// </summary>
        public IEnumerable<HarEntry> Entries { get; set; }
    }
}
