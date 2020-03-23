using System.Linq;
using System.Threading.Tasks;

namespace PlaywrightSharp.Firefox
{
    internal class FirefoxBrowserContext : IBrowserContextDelegate
    {
        private readonly string _browserContextId;
        private readonly FirefoxConnection _connection;
        private readonly BrowserContextOptions _browserContextOptions;
        private readonly FirefoxBrowser _browser;

        public FirefoxBrowserContext(string browserContextId, FirefoxConnection connection, BrowserContextOptions browserContextOptions, FirefoxBrowser browser)
        {
            _browserContextId = browserContextId;
            _connection = connection;
            _browserContextOptions = browserContextOptions;
            _browser = browser;
        }

        public BrowserContext BrowserContext { get; set; }

        public async Task<IPage[]> GetPagesAsync()
        {
            var targets = _browser.GetAllTargets()
                .Where(target => target.BrowserContext == BrowserContext && target.Type == TargetType.Page);
            var pages = await Task.WhenAll(targets.Select(target => target.GetPageAsync())).ConfigureAwait(false);
            return pages.Where(page => page != null).ToArray();
        }

        public Task<IPage> NewPage()
        {
            throw new System.NotImplementedException();
        }

        public Task SetGeolocationAsync(GeolocationOption geolocation)
        {
            throw new System.NotImplementedException();
        }

        public Task SetPermissionsAsync(string origin, params ContextPermission[] permissions)
        {
            throw new System.NotImplementedException();
        }
    }
}
