using PlaywrightSharp.Firefox;

namespace PlaywrightSharp.ProtocolTypesGenerator.Firefox
{
    internal class FirefoxBrowserProtocolTypesGenerator : IBrowserProtocolTypesGenerator
    {
        public ProtocolTypesGeneratorBase ProtocolTypesGenerator { get; } = new FirefoxProtocolTypesGenerator();

        public IBrowserFetcher BrowserFetcher { get; } = new FirefoxBrowserType().CreateBrowserFetcher();
    }
}
