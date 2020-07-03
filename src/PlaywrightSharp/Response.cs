using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channel;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IResponse" />
    public class Response : IChannelOwner, IResponse
    {
        private readonly ConnectionScope _scope;
        private readonly ResponseChannel _channel;

        internal Response(ConnectionScope scope, string guid, ResponseInitializer initializer)
        {
            _scope = scope;
            _channel = new ResponseChannel(guid, scope);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        Channel IChannelOwner.Channel => _channel;

        /// <inheritdoc />
        public HttpStatusCode Status { get; }

        /// <inheritdoc />
        public string StatusText { get; }

        /// <inheritdoc />
        public IFrame Frame { get; }

        /// <inheritdoc />
        public string Url { get; }

        /// <inheritdoc />
        public IDictionary<string, string> Headers { get; }

        /// <inheritdoc />
        public bool Ok { get; }

        /// <inheritdoc />
        public IRequest Request { get; }

        /// <inheritdoc />
        public Task<string> GetTextAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<JsonDocument> GetJsonAsync(JsonDocumentOptions options = default) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> GetJsonAsync<T>(JsonSerializerOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<byte[]> GetBufferAsync() => throw new NotImplementedException();
    }
}
