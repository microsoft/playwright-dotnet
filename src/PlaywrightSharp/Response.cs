using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IResponse" />
    public class Response : IChannelOwner, IResponse
    {
        internal Response(PlaywrightClient clilent, Channel channel, ResponseInitializer initializer)
        {
            throw new NotImplementedException();
        }

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
