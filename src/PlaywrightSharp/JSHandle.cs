using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channel;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IJSHandle" />
    public class JSHandle : IChannelOwner, IJSHandle
    {
        private readonly ConnectionScope _scope;
        private readonly JSHandleChannel _channel;

        internal JSHandle(ConnectionScope scope, string guid, JSHandleInitializer toObject)
        {
            _scope = scope;
            _channel = new JSHandleChannel(guid, scope);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        Channel IChannelOwner.Channel => _channel;

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> GetJsonValueAsync<T>() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> GetPropertyAsync(string propertyName) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IDictionary<string, IJSHandle>> GetPropertiesAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task DisposeAsync() => throw new NotImplementedException();
    }
}
