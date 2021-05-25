using System.Collections.Generic;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class PlaywrightInitializer
    {
        public BrowserType Chromium { get; set; }

        public BrowserType Firefox { get; set; }

        public BrowserType Webkit { get; set; }

        public List<DeviceDescriptorEntry> DeviceDescriptors { get; set; }

        public Selectors Selectors { get; set; }

        public Browser PreLaunchedBrowser { get; set; }
    }
}
