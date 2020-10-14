using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport.Protocol
{
    internal class PageInitializer
    {
        public FrameChannel MainFrame { get; set; }

        public ViewportSize ViewportSize { get; set; }

        public bool IsClosed { get; set; }
    }
}
