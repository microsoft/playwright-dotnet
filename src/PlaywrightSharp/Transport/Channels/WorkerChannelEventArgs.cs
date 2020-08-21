using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class WorkerChannelEventArgs : EventArgs
    {
        public WorkerChannel WorkerChannel { get; set; }
    }
}
