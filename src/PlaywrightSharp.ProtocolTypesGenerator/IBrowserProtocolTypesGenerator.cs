namespace PlaywrightSharp.ProtocolTypesGenerator
{
    internal interface IBrowserProtocolTypesGenerator
    {
        ProtocolTypesGeneratorBase ProtocolTypesGenerator { get; }

        IBrowserFetcher BrowserFetcher { get; }
    }
}
