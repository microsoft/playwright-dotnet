using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class PreviewUpdatedEventArgs : EventArgs
    {
        public string Preview { get; set; }
    }
}
