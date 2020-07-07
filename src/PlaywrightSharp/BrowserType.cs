using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserType" />
    public class BrowserType : IChannelOwner, IBrowserType
    {
        /// <summary>
        /// Browser type Chromium.
        /// </summary>
        public const string Chromium = "chromium";

        /// <summary>
        /// Browser type Firefox.
        /// </summary>
        public const string Firefox = "firefox";

        /// <summary>
        /// Browser type WebKit.
        /// </summary>
        public const string Webkit = "webkit";

        private readonly BrowserTypeInitializer _initializer;
        private readonly BrowserTypeChannel _channel;
        private readonly ConnectionScope _scope;

        internal BrowserType(ConnectionScope scope, string guid, BrowserTypeInitializer initializer)
        {
            _scope = scope;
            _initializer = initializer;
            _channel = new BrowserTypeChannel(guid, scope);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        Channel IChannelOwner.Channel => _channel;

        /// <inheritdoc />
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices { get; }

        /// <inheritdoc />
        public string ExecutablePath => _initializer.ExecutablePath;

        /// <inheritdoc />
        public string Name => _initializer.Name;

        /// <inheritdoc />
        public IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null) => throw new System.NotImplementedException();

        /// <inheritdoc />
        public async Task<IBrowser> LaunchAsync(LaunchOptions options = null)
            => (await _channel.LaunchAsync(options).ConfigureAwait(false)).Object as IBrowser;

        /// <inheritdoc />
        public string[] GetDefaultArgs(BrowserArgOptions options = null) => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IBrowser> ConnectAsync(ConnectOptions options = null) => throw new System.NotImplementedException();
    }
}
