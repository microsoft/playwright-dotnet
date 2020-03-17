namespace PlaywrightSharp.Firefox
{
    internal class FirefoxBrowserContext : IBrowserContextDelegate
    {
        private string _browserContextId;
        private FirefoxConnection _connection;
        private BrowserContextOptions _browserContextOptions;

        public FirefoxBrowserContext(string browserContextId, FirefoxConnection connection, BrowserContextOptions browserContextOptions)
        {
            _browserContextId = browserContextId;
            _connection = connection;
            _browserContextOptions = browserContextOptions;
        }
    }
}
