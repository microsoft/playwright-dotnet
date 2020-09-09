using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="Frame.Navigated"/>.
    /// </summary>
    public class FrameNavigatedEventArgs : EventArgs
    {
        /// <summary>
        /// Frame name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Frame URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Navigation Error.
        /// </summary>
        public string Error { get; set; }

        internal NavigateDocument NewDocument { get; set; }
    }
}
