using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IRequest"/>
    public class RequestBase : IRequest
    {
        public RequestBase()
        {
        }

        /// <inheritdoc cref="IRequest"/>
        public string Url => throw new NotImplementedException();

        /// <inheritdoc cref="IRequest"/>
        public HttpMethod Method => throw new NotImplementedException();

        /// <inheritdoc cref="IRequest"/>
        public IDictionary<string, string> Headers => throw new NotImplementedException();

        /// <inheritdoc cref="IRequest"/>
        public string PostData => throw new NotImplementedException();

        /// <inheritdoc cref="IRequest"/>
        public IFrame Frame => throw new NotImplementedException();

        /// <inheritdoc cref="IRequest"/>
        public bool IsNavigationRequest => throw new NotImplementedException();

        /// <inheritdoc cref="IRequest"/>
        public ResourceType ResourceType => throw new NotImplementedException();

        /// <inheritdoc cref="IRequest"/>
        public IRequest[] RedirectChain => throw new NotImplementedException();

        /// <inheritdoc cref="IRequest"/>
        public IResponse Response => throw new NotImplementedException();

        /// <inheritdoc cref="IRequest"/>
        public string Failure => throw new NotImplementedException();

        internal string DocumentId { get; set; }

        /// <inheritdoc cref="IRequest"/>
        public Task AbortAsync(RequestAbortErrorCode errorCode = RequestAbortErrorCode.Failed)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IRequest"/>
        public Task ContinueAsync(Payload payload = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IRequest"/>
        public Task FulfillAsync(ResponseData response)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IRequest"/>
        public Task RespondAsync(ResponseData response)
        {
            throw new NotImplementedException();
        }
    }
}
