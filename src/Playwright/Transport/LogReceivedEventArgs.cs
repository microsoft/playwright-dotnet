using System;

namespace Microsoft.Playwright.Transport
{
    internal class LogReceivedEventArgs : EventArgs
    {
        public LogReceivedEventArgs(string message) => Message = message;

        public string Message { get; }
    }
}
