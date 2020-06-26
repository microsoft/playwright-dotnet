namespace PlaywrightSharp.Server
{
    internal class LaunchServerResult
    {
        public BrowserServer BrowserServer { get; set; }

        public string DownloadsPath { get; set; }

        public IConnectionTransport Transport { get; set; }

        public void Deconstruct(out IBrowserServer browserServer, out string downloadsPath, out IConnectionTransport transport)
            => (browserServer, downloadsPath, transport) = (BrowserServer, DownloadsPath, Transport);
    }
}
