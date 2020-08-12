using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class FileChooserChannelEventArgs : EventArgs
    {
        public ElementHandleChannel Element { get; set; }

        public bool IsMultiple { get; set; }
    }
}
