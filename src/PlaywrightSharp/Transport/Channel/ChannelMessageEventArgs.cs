using System;

namespace PlaywrightSharp.Transport.Channel
{
    internal class ChannelMessageEventArgs : EventArgs
    {
        public string Method { get; set; }

        public PlaywrightSharpServerParams Params { get; set; }
    }
}
