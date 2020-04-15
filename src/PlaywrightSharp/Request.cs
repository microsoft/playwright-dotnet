using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IRequest"/>
    public class Request : IRequest
    {
        private readonly IRequestDelegate _delegate;
        private readonly TaskCompletionSource<Response> _waitForResponseTsc;
        private readonly TaskCompletionSource<Response> _waitForFinishedTsc;
        private readonly Task<Response> _waitForResponse;
        private bool _interceptionHandled = false;

        internal Request(IRequestDelegate requestDelegate, Frame frame, Request[] redirectChain, string documentId, string url, ResourceType resourceType, HttpMethod method, string postData, IDictionary<string, string> headers)
        {
            if (url.StartsWith("data:"))
            {
                throw new PlaywrightSharpException("Data urls should not fire requests");
            }

            _delegate = requestDelegate;
            Frame = frame;
            RedirectChain = redirectChain;
            FinalRequest = this;
            foreach (var request in redirectChain)
            {
                request.FinalRequest = this;
            }

            DocumentId = documentId;
            Url = StripFragmentFromUrl(url);
            ResourceType = resourceType;
            Method = method;
            PostData = postData;
            Headers = headers;

            _waitForResponseTsc = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waitForResponse = _waitForResponseTsc.Task;
            _waitForFinishedTsc = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);

            IsFavicon = Url.EndsWith("/favicon.ico");
        }

        /// <inheritdoc cref="IRequest.Url"/>
        public string Url { get; }

        /// <inheritdoc cref="IRequest.Method"/>
        public HttpMethod Method { get; }

        /// <inheritdoc cref="IRequest.Headers"/>
        public IDictionary<string, string> Headers { get; }

        /// <inheritdoc cref="IRequest.PostData"/>
        public string PostData { get; }

        /// <inheritdoc cref="IRequest.Frame"/>
        IFrame IRequest.Frame => Frame;

        /// <inheritdoc cref="IRequest.IsNavigationRequest"/>
        public bool IsNavigationRequest { get; }

        /// <inheritdoc cref="IRequest.ResourceType"/>
        public ResourceType ResourceType { get; }

        /// <inheritdoc cref="IRequest.RedirectChain"/>
        IRequest[] IRequest.RedirectChain => RedirectChain;

        /// <inheritdoc cref="IRequest.Response"/>
        IResponse IRequest.Response => Response;

        /// <inheritdoc cref="IRequest.Failure"/>
        public string Failure { get; }

        internal bool IsFavicon { get; }

        internal string DocumentId { get; set; }

        internal Request FinalRequest { get; private set; }

        internal Response Response { get; private set; }

        internal Frame Frame { get; }

        internal Request[] RedirectChain { get; }

        internal Task<Response> WaitForFinished => _waitForFinishedTsc.Task;

        /// <inheritdoc cref="IRequest.AbortAsync(RequestAbortErrorCode)"/>
        public Task AbortAsync(RequestAbortErrorCode errorCode = RequestAbortErrorCode.Failed)
        {
            if (_delegate == null)
            {
                throw new PlaywrightSharpException("Request Interception is not enabled!");
            }

            if (_interceptionHandled)
            {
                throw new PlaywrightSharpException("Request is already handled!");
            }

            _interceptionHandled = true;
            return _delegate.AbortAsync(errorCode);
        }

        /// <inheritdoc cref="IRequest.ContinueAsync(Payload)"/>
        public Task ContinueAsync(Payload payload = null)
        {
            if (_delegate == null)
            {
                throw new PlaywrightSharpException("Request Interception is not enabled!");
            }

            if (_interceptionHandled)
            {
                throw new PlaywrightSharpException("Request is already handled!");
            }

            return _delegate.ContinueAsync(payload);
        }

        /// <inheritdoc cref="IRequest.FulfillAsync(ResponseData)"/>
        public Task FulfillAsync(ResponseData response)
        {
            if (_delegate == null)
            {
                throw new PlaywrightSharpException("Request Interception is not enabled!");
            }

            if (_interceptionHandled)
            {
                throw new PlaywrightSharpException("Request is already handled!");
            }

            _interceptionHandled = true;
            return _delegate.FulfillAsync(response);
        }

        internal async Task<Response> WaitForResponseAsync()
        {
            var response = await _waitForResponse.ConfigureAwait(false);
            await response.Finished.ConfigureAwait(false);
            return response;
        }

        internal void SetResponse(Response response)
        {
            Response = response;
            _waitForResponseTsc.TrySetResult(response);
            response.Finished.ContinueWith(
                _ =>
            {
                return _waitForFinishedTsc.TrySetResult(response);
            }, TaskScheduler.Default);
        }

        private string StripFragmentFromUrl(string url)
        {
            int hashIndex = url.IndexOf("#");
            if (hashIndex == -1)
            {
                return url;
            }

            return url.Substring(0, hashIndex);
        }
    }
}
