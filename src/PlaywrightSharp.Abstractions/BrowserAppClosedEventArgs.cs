using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Raised when the <see cref="IBrowserServer"/> gets closed.
    /// </summary>
    public class BrowserAppClosedEventArgs : EventArgs
    {
        /// <summary>
        /// Process exit code.
        /// </summary>
        public int ExitCode { get; set; }
    }
}
