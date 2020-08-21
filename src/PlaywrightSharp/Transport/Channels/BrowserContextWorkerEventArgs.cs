using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserContextWorkerEventArgs : EventArgs
    {
        public WorkerChannel WorkerChannel { get; set; }
    }
}
