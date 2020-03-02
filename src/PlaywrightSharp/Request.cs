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

        /// <inheritdoc cref="IRequest.Url"/>
        public string Url => null;

        /// <inheritdoc cref="IRequest.Method"/>
        public HttpMethod Method => null;

        /// <inheritdoc cref="IRequest.Headers"/>
        public IDictionary<string, string> Headers => null;

        /// <inheritdoc cref="IRequest.PostData"/>
        public string PostData => null;

        /// <inheritdoc cref="IRequest.Frame"/>
        public IFrame Frame => null;

        /// <inheritdoc cref="IRequest.IsNavigationRequest"/>
        public bool IsNavigationRequest => false;

        /// <inheritdoc cref="IRequest.ResourceType"/>
        public ResourceType ResourceType => ResourceType.Document;

        /// <inheritdoc cref="IRequest.RedirectChain"/>
        public IRequest[] RedirectChain => null;

        /// <inheritdoc cref="IRequest.Response"/>
        public IResponse Response => null;

        /// <inheritdoc cref="IRequest.Failure"/>
        public string Failure => null;

        internal string DocumentId { get; set; }

        /// <inheritdoc cref="IRequest.AbortAsync(RequestAbortErrorCode)"/>
        public Task AbortAsync(RequestAbortErrorCode errorCode = RequestAbortErrorCode.Failed)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IRequest.ContinueAsync(Payload)"/>
        public Task ContinueAsync(Payload payload = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IRequest.FulfillAsync(ResponseData)"/>
        public Task FulfillAsync(ResponseData response)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IRequest.RespondAsync(ResponseData)"/>
        public Task RespondAsync(ResponseData response)
        {
            throw new NotImplementedException();
        }
    }
}
