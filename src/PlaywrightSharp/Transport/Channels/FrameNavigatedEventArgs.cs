using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class FrameNavigatedEventArgs : EventArgs
    {
        public FrameChannel Frame { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }
    }
}
