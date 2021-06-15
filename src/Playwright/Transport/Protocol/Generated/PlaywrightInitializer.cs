using System.Collections.Generic;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class PlaywrightInitializer
    {
        public Core.BrowserType Chromium { get; set; }

        public Core.BrowserType Firefox { get; set; }

        public Core.BrowserType Webkit { get; set; }

        public List<DeviceDescriptorEntry> DeviceDescriptors { get; set; }

        public Core.Selectors Selectors { get; set; }

        public Core.Browser PreLaunchedBrowser { get; set; }
    }
}
