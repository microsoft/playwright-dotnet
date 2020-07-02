using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IRequest" />
    public class Request : IChannelOwner, IRequest
    {
        internal Request(PlaywrightClient client, Channel channel, RequestInitializer initializer)
        {
            throw new NotImplementedException();
        }

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
