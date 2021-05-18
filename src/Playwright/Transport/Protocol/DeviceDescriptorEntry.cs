namespace Microsoft.Playwright.Transport.Protocol
{
    internal class DeviceDescriptorEntry
    {
        public string Name { get; set; }

        public BrowserNewContextOptions Descriptor { get; set; }
    }
}
