using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Helper;
using PlaywrightSharp.Firefox.Protocol.Network;

namespace PlaywrightSharp.Firefox
{
    internal class FirefoxRequest : IRequestDelegate
    {
        private readonly FirefoxSession _session;

        public FirefoxRequest(FirefoxSession session, Frame frame, Request[] redirectChain, NetworkRequestWillBeSentFirefoxEvent payload)
        {
            Id = payload.RequestId;
            _session = session;

            var headers = payload.Headers.ToDictionary(header => header.Name.ToLower(), header => header.Value);

            Request = new Request(payload.IsIntercepted == true ? this : null, frame, redirectChain, payload.NavigationId, payload.Url, payload.GetResourceType(), new HttpMethod(payload.Method), payload.PostData, headers);
        }

        public Request Request { get; }

        internal string Id { get; }

        public Task AbortAsync(RequestAbortErrorCode errorCode = RequestAbortErrorCode.Failed)
        {
            throw new System.NotImplementedException();
        }

        public Task ContinueAsync(Payload payload = null)
            => _session.SendAsync(new NetworkResumeInterceptedRequestRequest
            {
                RequestId = Id,
                Method = payload?.Method?.Method,
                Headers = payload?.Headers?.ToHeadersArray(),
                PostData = payload?.PostData,
            });

        public Task FulfillAsync(ResponseData response)
        {
            var responseHeaders = response.Headers;
            if (!string.IsNullOrEmpty(response.ContentType))
            {
                responseHeaders["content-type"] = response.ContentType;
            }

            if (response.Body.Length > 0 && !responseHeaders.ContainsKey("content-length"))
            {
                responseHeaders["content-length"] = response.Body.Length.ToString();
            }

            return _session.SendAsync(new NetworkFulfillInterceptedRequestRequest
            {
                RequestId = Id,
                Status = (int)response.Status,
                StatusText = response.Status.ToStatusText(),
                Headers = responseHeaders.ToHeadersArray(),
                Base64Body = Convert.ToBase64String(response.BodyData),
            });
        }
    }
}
