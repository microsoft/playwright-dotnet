using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IRequest" />
    public class Request : IChannelOwner<Request>, IRequest
    {
        private readonly ConnectionScope _scope;
        private readonly RequestChannel _channel;

        internal Request(ConnectionScope scope, string guid, RequestInitializer initializer)
        {
            _scope = scope;
            _channel = new RequestChannel(guid, scope, this);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        Channel<Request> IChannelOwner<Request>.Channel => _channel;

        /// <inheritdoc />
        public string Url { get; }

        /// <inheritdoc />
        public HttpMethod Method { get; }

        /// <inheritdoc />
        public IDictionary<string, string> Headers { get; }

        /// <inheritdoc />
        public string PostData { get; }

        /// <inheritdoc />
        public IFrame Frame { get; }

        /// <inheritdoc />
        public bool IsNavigationRequest { get; }

        /// <inheritdoc />
        public ResourceType ResourceType { get; }

        /// <inheritdoc />
        public IRequest[] RedirectChain { get; }

        /// <inheritdoc />
        public IResponse Response { get; }

        /// <inheritdoc />
        public string Failure { get; }

        /// <inheritdoc />
        public Task ContinueAsync(Payload payload = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task FulfillAsync(ResponseData response) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task AbortAsync(RequestAbortErrorCode errorCode = RequestAbortErrorCode.Failed) => throw new NotImplementedException();
    }
}
