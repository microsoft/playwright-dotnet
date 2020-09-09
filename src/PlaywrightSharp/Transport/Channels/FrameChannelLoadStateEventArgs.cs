using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class FrameChannelLoadStateEventArgs : EventArgs
    {
        public LifecycleEvent? Add { get; set; }

        public LifecycleEvent? Remove { get; set; }
    }
}
