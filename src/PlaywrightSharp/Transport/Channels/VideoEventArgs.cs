using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class VideoEventArgs : EventArgs
    {
        public string RelativePath { get; set; }
    }
}
