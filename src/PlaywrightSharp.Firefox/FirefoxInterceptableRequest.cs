using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Helper;
using PlaywrightSharp.Firefox.Protocol.Network;

namespace PlaywrightSharp.Firefox
{
    internal class FirefoxInterceptableRequest : IRequestDelegate
    {
        private readonly FirefoxSession _session;

        public FirefoxInterceptableRequest(FirefoxSession session, Frame frame, List<Request> redirectChain, NetworkRequestWillBeSentFirefoxEvent payload)
        {
            Id = payload.RequestId;
            _session = session;

            var headers = payload.Headers.ToDictionary(header => header.Name.ToLower(), header => header.Value, StringComparer.InvariantCultureIgnoreCase);

            Request = new Request(payload.IsIntercepted == true ? this : null, frame, redirectChain, payload.NavigationId, payload.Url, payload.GetResourceType(), new HttpMethod(payload.Method), payload.PostData, headers);
        }

        public Request Request { get; }

        internal string Id { get; }

        public Task AbortAsync(RequestAbortErrorCode errorCode = RequestAbortErrorCode.Failed)
            => _session.SendAsync(new NetworkAbortInterceptedRequestRequest
            {
                RequestId = Id,
                ErrorCode = errorCode.ToString().ToLower(),
            });

        public async Task ContinueAsync(Payload payload = null)
        {
            try
            {
                await _session.SendAsync(new NetworkResumeInterceptedRequestRequest
                {
                    RequestId = Id,
                    Method = payload?.Method?.Method,
                    Headers = payload?.Headers?.ToHeadersArray(),
                    PostData = payload?.PostData,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

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
                Status = (int)(response.Status ?? HttpStatusCode.OK),
                StatusText = (response.Status ?? HttpStatusCode.OK).ToStatusText(),
                Headers = responseHeaders.ToHeadersArray(),
                Base64Body = Convert.ToBase64String(response.BodyData),
            });
        }
    }
}
