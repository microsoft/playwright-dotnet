using PlaywrightSharp.Firefox;

namespace PlaywrightSharp.ProtocolTypesGenerator.Firefox
{
    internal class FirefoxBrowserProtocolTypesGenerator : IBrowserProtocolTypesGenerator
    {
        public IProtocolTypesGenerator ProtocolTypesGenerator { get; } = new FirefoxProtocolTypesGenerator();

        public IBrowserFetcher BrowserFetcher { get; } = new FirefoxBrowserType().CreateBrowserFetcher();
    }
}
