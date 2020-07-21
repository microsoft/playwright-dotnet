using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IResponse" />
    public class Response : IChannelOwner<Response>, IResponse
    {
        private readonly ConnectionScope _scope;
        private readonly ResponseChannel _channel;
        private readonly ResponseInitializer _initializer;

        internal Response(ConnectionScope scope, string guid, ResponseInitializer initializer)
        {
            _scope = scope;
            _channel = new ResponseChannel(guid, scope, this);
            _initializer = initializer;
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Response> IChannelOwner<Response>.Channel => _channel;

        /// <inheritdoc />
        public HttpStatusCode Status => _initializer.Status;

        /// <inheritdoc />
        public string StatusText { get; }

        /// <inheritdoc />
        public IFrame Frame { get; }

        /// <inheritdoc />
        public string Url { get; }

        /// <inheritdoc />
        public IDictionary<string, string> Headers { get; }

        /// <inheritdoc />
        public bool Ok => Status == HttpStatusCode.OK;

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
