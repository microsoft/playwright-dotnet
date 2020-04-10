using PlaywrightSharp.Webkit;

namespace PlaywrightSharp.ProtocolTypesGenerator.Webkit
{
    internal class WebkitBrowserProtocolTypesGenerator : IBrowserProtocolTypesGenerator
    {
        public ProtocolTypesGeneratorBase ProtocolTypesGenerator { get; } = new WebkitProtocolTypesGenerator();

        public IBrowserFetcher BrowserFetcher { get; } = new WebkitBrowserType().CreateBrowserFetcher();
    }
}
