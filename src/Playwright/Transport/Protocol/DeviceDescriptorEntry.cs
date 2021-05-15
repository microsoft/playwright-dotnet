namespace Microsoft.Playwright.Transport.Protocol
{
    internal class DeviceDescriptorEntry
    {
        public string Name { get; set; }

        public BrowserContextOptions Descriptor { get; set; }
    }
}
