namespace PlaywrightSharp.Chromium
{
    internal class ChromiumConnection
    {
        private readonly ITransport _transport;

        public ChromiumConnection(ITransport transport)
        {
            _transport = transport;
        }

        public ChromiumSession RootSession { get; set; }
    }
}
