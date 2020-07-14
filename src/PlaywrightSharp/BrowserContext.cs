using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserContext" />
    public class BrowserContext : IChannelOwner<BrowserContext>, IBrowserContext
    {
        private readonly ConnectionScope _scope;
        private readonly BrowserContextChannel _channel;

        internal BrowserContext(ConnectionScope scope, string guid, BrowserContextInitializer initializer)
        {
            _scope = scope.CreateChild(guid);
            _channel = new BrowserContextChannel(guid, scope, this);

            _channel.Closed += (sender, e) => Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Closed;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        Channel<BrowserContext> IChannelOwner<BrowserContext>.Channel => _channel;

        /// <inheritdoc />
        public BrowserContextOptions Options { get; }

        /// <inheritdoc />
        public IPage[] Pages => PagesList.ToArray();

        internal Page OwnerPage { get; set; }

        internal List<Page> PagesList { get; } = new List<Page>();

        /// <inheritdoc />
        public async Task<IPage> NewPageAsync(string url = null)
            => (await _channel.NewPageAsync(url).ConfigureAwait(false)).Object;

        /// <inheritdoc />
        public Task CloseAsync() => throw new NotImplementedException();

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
