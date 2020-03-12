using PlaywrightSharp.Chromium;

namespace PlaywrightSharp.ProtocolTypesGenerator.Chromium
{
    internal class ChromiumBrowserProtocolTypesGenerator : IBrowserProtocolTypesGenerator
    {
        public IProtocolTypesGenerator ProtocolTypesGenerator { get; } = new ChromiumProtocolTypesGenerator();

        public IBrowserFetcher BrowserFetcher { get; } = new ChromiumBrowserType().CreateBrowserFetcher();
    }
}
