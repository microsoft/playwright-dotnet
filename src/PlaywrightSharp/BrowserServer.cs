using System;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserServer"/>
    public class BrowserServer : ChannelOwnerBase, IBrowserServer, IChannelOwner<BrowserServer>
    {
        private readonly BrowserServerInitializer _initializer;
        private readonly BrowserServerChannel _channel;

        internal BrowserServer(IChannelOwner parent, string guid, BrowserServerInitializer initializer) : base(parent, guid)
        {
            _initializer = initializer;
            _channel = new BrowserServerChannel(guid, parent.Connection, this);
            _channel.Closed += (sender, e) => Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public event EventHandler Closed;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<BrowserServer> IChannelOwner<BrowserServer>.Channel => _channel;

        /// <inheritdoc/>
        public int ProcessId => _initializer.Pid;

        /// <inheritdoc/>
        public string WSEndpoint => _initializer.WsEndpoint;

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

        /// <inheritdoc/>
        public Task CloseAsync() => _channel.CloseAsync();

        /// <inheritdoc/>
        public Task KillAsync() => _channel.KillAsync();
    }
}
