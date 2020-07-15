using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class ChannelMessageEventArgs : EventArgs
    {
        public string Method { get; set; }

        public CreateObjectInfo Params { get; set; }
    }
}
