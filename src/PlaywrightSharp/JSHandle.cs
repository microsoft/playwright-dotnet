using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IJSHandle" />
    public class JSHandle : IChannelOwner, IJSHandle
    {
        internal JSHandle(PlaywrightClient client, Channel channel, JSHandleInitializer toObject)
        {
            throw new NotImplementedException();
        }

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
