using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
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
        public string StatusText => _initializer.StatusText;

        /// <inheritdoc />
        public IFrame Frame => _initializer.Frame.Object;

        /// <inheritdoc />
        public string Url => _initializer.Url;

        /// <inheritdoc />
        public IDictionary<string, string> Headers => _initializer.Headers;

        /// <inheritdoc />
        public bool Ok => Status == HttpStatusCode.OK;

        /// <inheritdoc />
        public IRequest Request => _initializer.Request.Object;

        /// <inheritdoc />
        public async Task<string> GetTextAsync()
        {
            byte[] content = await GetBodyAsync().ConfigureAwait(false);
            return Encoding.UTF8.GetString(content);
        }

        /// <inheritdoc />
        public Task<JsonDocument> GetJsonAsync(JsonDocumentOptions options = default) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> GetJsonAsync<T>(JsonSerializerOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public async Task<byte[]> GetBodyAsync() => Convert.FromBase64String(await _channel.GetBodyAsync().ConfigureAwait(false));
    }
}
