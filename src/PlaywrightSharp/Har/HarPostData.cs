using System;
using System.Collections.Generic;

namespace PlaywrightSharp.Har
{
    /// <summary>
    /// HAR POST Data.
    /// </summary>
    public class HarPostData
    {
        /// <summary>
        /// Mime Type.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Post Params.
        /// </summary>
        public IEnumerable<HarPostDataParam> Params { get; set; } = Array.Empty<HarPostDataParam>();
    }
}
