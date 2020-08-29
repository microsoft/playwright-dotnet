using System;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc/>
    public class CDPSession : IChannelOwner<CDPSession>, ICDPSession
    {
        private readonly ConnectionScope _scope;
        private readonly CDPSessionChannel _channel;
        private readonly CDPSessionInitializer _initializer;

        internal CDPSession(ConnectionScope scope, string guid, CDPSessionInitializer initializer)
        {
            _scope = scope.CreateChild(guid);
            _channel = new CDPSessionChannel(guid, scope, this);
            _initializer = initializer;

            _channel.CDPEvent += (sender, e) => MessageReceived?.Invoke(this, e);
            _channel.Disconnected += (sender, e) => _scope.Dispose();
        }

        /// <inheritdoc/>
        public event EventHandler<CDPEventArgs> MessageReceived;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

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
            return result == null ? default : result.Value.ToObject<T>(_scope.Connection.GetDefaultJsonSerializerOptions());
        }

        /// <inheritdoc/>
        public Task DetachAsync() => _channel.DetachAsync();
    }
}
