using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channel;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserContext" />
    public class BrowserContext : IChannelOwner, IBrowserContext
    {
        private readonly ConnectionScope _scope;
        private readonly BrowserContextChannel _channel;

        internal BrowserContext(ConnectionScope scope, string guid, BrowserContextInitializer initializer)
        {
            _scope = scope;
            _channel = new BrowserContextChannel(guid, scope);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        Channel IChannelOwner.Channel => _channel;

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
