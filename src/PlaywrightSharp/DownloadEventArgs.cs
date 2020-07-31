using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="IPage.Download"/>.
    /// </summary>
    public class DownloadEventArgs : EventArgs
    {
        /// <summary>
        /// Download info.
        /// </summary>
        public Download Download { get; internal set; }
    }
}