using System;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class WorkerChannelEventArgs : EventArgs
    {
        public WorkerChannel WorkerChannel { get; set; }
    }
}
