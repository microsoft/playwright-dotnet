using System;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserServer"/>
    public class BrowserServer : IBrowserServer, IChannelOwner<BrowserServer>
    {
        private readonly ConnectionScope _scope;
        private readonly BrowserServerInitializer _initializer;
        private readonly BrowserServerChannel _channel;

        internal BrowserServer(ConnectionScope scope, string guid, BrowserServerInitializer initializer)
        {
            _scope = scope;
            _initializer = initializer;
            _channel = new BrowserServerChannel(guid, scope, this);
            _channel.Closed += (sender, e) => Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public event EventHandler Closed;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        Channel<BrowserServer> IChannelOwner<BrowserServer>.Channel => _channel;

        /// <inheritdoc/>
        public int ProcessId => _initializer.Pid;

        /// <inheritdoc/>
        public string WebSocketEndpoint => _initializer.WsEndpoint;

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

        /// <inheritdoc/>
        public Task CloseAsync() => _channel.CloseAsync();

        /// <inheritdoc/>
        public Task KillAsync() => _channel.KillAsync();
    }
}
