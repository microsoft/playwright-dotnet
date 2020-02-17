namespace PlaywrightSharp.Chromium
{
    internal class ChromiumConnection
    {
        private readonly IConnectionTransport _transport;

        public ChromiumConnection(IConnectionTransport transport)
        {
            _transport = transport;
        }

        public ChromiumSession RootSession { get; set; }
    }
}
