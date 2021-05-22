using System;

namespace Microsoft.Playwright.Transport
{
    internal class TransportClosedEventArgs : EventArgs
    {
        /// <summary>
        /// Close reason.
        /// </summary>
        public string CloseReason { get; set; }
    }
}
