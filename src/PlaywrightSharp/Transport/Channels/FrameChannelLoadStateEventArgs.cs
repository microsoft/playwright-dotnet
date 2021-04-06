using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class FrameChannelLoadStateEventArgs : EventArgs
    {
        public LoadState? Add { get; set; }

        public LoadState? Remove { get; set; }
    }
}
