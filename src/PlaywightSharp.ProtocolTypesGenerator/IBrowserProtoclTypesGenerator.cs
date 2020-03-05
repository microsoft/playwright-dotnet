namespace PlaywrightSharp.ProtocolTypesGenerator
{
    internal interface IBrowserProtocolTypesGenerator
    {
        IProtocolTypesGenerator ProtocolTypesGenerator { get; }
        IBrowserFetcher BrowserFetcher { get; }
    }
}
