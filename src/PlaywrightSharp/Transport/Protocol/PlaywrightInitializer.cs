using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport.Protocol
{
    internal class PlaywrightInitializer
    {
        public BrowserType Chromium { get; set; }

        public BrowserType Webkit { get; set; }

        public BrowserType Firefox { get; set; }

        public SelectorsChannel Selectors { get; set; }
    }
}
