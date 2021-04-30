namespace Microsoft.Playwright.Transport.Protocol
{
    internal class BrowserServerInitializer
    {
        public int Pid { get; set; }

        public string WsEndpoint { get; set; }
    }
}
