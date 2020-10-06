using System.Collections.Generic;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport.Protocol
{
    internal class PlaywrightInitializer
    {
        public ChromiumBrowserType Chromium { get; set; }

        public BrowserType Webkit { get; set; }

        public BrowserType Firefox { get; set; }

        public IEnumerable<DeviceDescriptorEntry> DeviceDescriptors { get; set; }

        public SelectorsChannel Selectors { get; set; }
    }
}
