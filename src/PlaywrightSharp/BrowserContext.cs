using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserContext" />
    public class BrowserContext : IChannelOwner, IBrowserContext
    {
        internal BrowserContext(PlaywrightClient client, Channel channel, BrowserContextInitializer initializer)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public BrowserContextOptions Options { get; }

        /// <inheritdoc />
        public Task<IPage> NewPageAsync(string url = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task CloseAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IPage[]> GetPagesAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IEnumerable<NetworkCookie>> GetCookiesAsync(params string[] urls) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetCookiesAsync(params SetNetworkCookieParam[] cookies) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ClearCookiesAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetPermissionsAsync(string origin, params ContextPermission[] permissions) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetGeolocationAsync(GeolocationOption geolocation) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ClearPermissionsAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public IEnumerable<IPage> GetExistingPages() => throw new NotImplementedException();
    }
}
