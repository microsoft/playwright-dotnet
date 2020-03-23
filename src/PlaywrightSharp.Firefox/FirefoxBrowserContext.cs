using System.Threading.Tasks;

namespace PlaywrightSharp.Firefox
{
    internal class FirefoxBrowserContext : IBrowserContextDelegate
    {
        private readonly string _browserContextId;
        private readonly FirefoxConnection _connection;
        private readonly BrowserContextOptions _browserContextOptions;

        public FirefoxBrowserContext(string browserContextId, FirefoxConnection connection, BrowserContextOptions browserContextOptions)
        {
            _browserContextId = browserContextId;
            _connection = connection;
            _browserContextOptions = browserContextOptions;
        }

        public BrowserContext BrowserContext { get; set; }

        public Task<IPage[]> GetPagesAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<IPage> NewPage()
        {
            throw new System.NotImplementedException();
        }

        public Task SetGeolocationAsync(GeolocationOption geolocation)
        {
            throw new System.NotImplementedException();
        }

        public Task CloseAsync() => throw new System.NotImplementedException();

        public Task SetPermissionsAsync(string origin, params ContextPermission[] permissions)
        {
            throw new System.NotImplementedException();
        }
    }
}
