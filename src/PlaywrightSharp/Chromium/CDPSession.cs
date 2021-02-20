using System;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc/>
    public class CDPSession : ChannelOwnerBase, IChannelOwner<CDPSession>, ICDPSession
    {
        private readonly Connection _connection;
        private readonly CDPSessionChannel _channel;

        internal CDPSession(IChannelOwner parent, string guid) : base(parent, guid)
        {
            _connection = parent.Connection;
            _channel = new CDPSessionChannel(guid, parent.Connection, this);

            _channel.CDPEvent += (sender, e) => MessageReceived?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public event EventHandler<CDPEventArgs> MessageReceived;

        /// <inheritdoc/>
        Connection IChannelOwner.Connection => _connection;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<CDPSession> IChannelOwner<CDPSession>.Channel => _channel;

        /// <inheritdoc/>
        public Task<JsonElement?> SendAsync(string method, object args = null) => _channel.SendAsync(method, args);

        /// <inheritdoc/>
        public async Task<T> SendAsync<T>(string method, object args = null)
        {
            var result = await _channel.SendAsync(method, args).ConfigureAwait(false);
            return result == null ? default : result.Value.ToObject<T>(_connection.DefaultJsonSerializerOptions);
        }

        /// <inheritdoc/>
        public Task DetachAsync() => _channel.DetachAsync();
    }
}
