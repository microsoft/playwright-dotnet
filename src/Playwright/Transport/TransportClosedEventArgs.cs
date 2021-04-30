using System;

namespace Microsoft.Playwright.Transport
{
    /// <summary>
    /// See <see cref="IConnectionTransport.TransportClosed"/>.
    /// </summary>
    public class TransportClosedEventArgs : EventArgs
    {
        /// <summary>
        /// Close reason.
        /// </summary>
        public string CloseReason { get; set; }
    }
}
