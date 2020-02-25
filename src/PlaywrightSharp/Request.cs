using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IRequest"/>
    public class Request : IRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Request"/> class.
        /// </summary>
        public Request()
        {
        }

        /// <inheritdoc cref="IRequest"/>
        public string Url => null;

        /// <inheritdoc cref="IRequest"/>
        public HttpMethod Method => null;

        /// <inheritdoc cref="IRequest"/>
        public IDictionary<string, string> Headers => null;

        /// <inheritdoc cref="IRequest"/>
        public string PostData => null;

        /// <inheritdoc cref="IRequest"/>
        public IFrame Frame => null;

        /// <inheritdoc cref="IRequest"/>
        public bool IsNavigationRequest => false;

        /// <inheritdoc cref="IRequest"/>
        public ResourceType ResourceType => ResourceType.Document;

        /// <inheritdoc cref="IRequest"/>
        public IRequest[] RedirectChain => null;

        /// <inheritdoc cref="IRequest"/>
        public IResponse Response => null;

        /// <inheritdoc cref="IRequest"/>
        public string Failure => null;

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
