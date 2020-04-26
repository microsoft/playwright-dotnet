using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Protocol;
using PlaywrightSharp.Firefox.Protocol.Network;

namespace PlaywrightSharp.Firefox
{
    internal class FirefoxNetworkManager
    {
        private readonly FirefoxSession _session;
        private readonly Page _page;
        private readonly ConcurrentDictionary<string, FirefoxRequest> _requests = new ConcurrentDictionary<string, FirefoxRequest>();

        public FirefoxNetworkManager(FirefoxSession session, Page page)
        {
            _session = session;
            _page = page;

            _session.MessageReceived += OnMessageReceived;
        }

        public void Dispose() => _session.MessageReceived -= OnMessageReceived;

        internal Task SetRequestInterception(bool enabled)
            => _session.SendAsync(new NetworkSetRequestInterceptionRequest { Enabled = enabled });

        private void OnMessageReceived(object sender, IFirefoxEvent e)
        {
            switch (e)
            {
                case NetworkRequestWillBeSentFirefoxEvent requestWillBeSent:
                    OnRequestWillBeSent(requestWillBeSent);
                    break;
                case NetworkResponseReceivedFirefoxEvent responseReceived:
                    OnResponseReceived(responseReceived);
                    break;
                case NetworkRequestFinishedFirefoxEvent requestFinished:
                    OnRequestFinished(requestFinished);
                    break;
                case NetworkRequestFailedFirefoxEvent requestFailed:
                    OnRequestFailed(requestFailed);
                    break;
            }
        }

        private void OnRequestWillBeSent(NetworkRequestWillBeSentFirefoxEvent e)
        {
            _requests.TryGetValue(e.RedirectedFrom ?? string.Empty, out var redirectId);
            var frame = (redirectId != null ? redirectId.Request.Frame : (e.FrameId != null ? _page.FrameManager.Frames[e.FrameId] : null)) as Frame;
            if (frame == null)
            {
                return;
            }

            var redirectChain = new List<Request>();
            if (redirectId != null)
            {
                redirectChain = new List<Request>(redirectId.Request.RedirectChain);
                redirectChain.Add(redirectId.Request);
                _requests.TryRemove(redirectId.Id, out var _);
            }

            var request = new FirefoxRequest(_session, frame, redirectChain, e);
            _requests[request.Id] = request;
            _page.FrameManager.RequestStarted(request.Request);
        }

        private void OnResponseReceived(NetworkResponseReceivedFirefoxEvent e)
        {
            if (!_requests.TryGetValue(e.RequestId, out var request))
            {
                return;
            }

            Func<Task<byte[]>> getResponseBody = async () =>
            {
                var response = await _session.SendAsync(new NetworkGetResponseBodyRequest
                {
                    RequestId = request.Id,
                }).ConfigureAwait(false);
                if (response.Evicted == true)
                {
                    throw new PlaywrightSharpException($"Response body for {request.Request.Method} {request.Request.Url} was evicted!");
                }

                return Convert.FromBase64String(response.Base64Body);
            };
            var headers = e.Headers.ToDictionary(header => header.Name.ToLower(), header => header.Value);
            var response = new Response(request.Request, (HttpStatusCode)e.Status, e.StatusText, headers, getResponseBody);
            _page.FrameManager.RequestReceivedResponse(response);
        }

        private void OnRequestFinished(NetworkRequestFinishedFirefoxEvent e)
        {
            if (!_requests.TryGetValue(e.RequestId, out var request))
            {
                return;
            }

            var response = request.Request.Response;

            // Keep redirected requests in the map for future reference in redirectChain.
            bool isRedirected = (int)response.Status >= 300 && (int)response.Status <= 399;
            if (isRedirected)
            {
                response.RequestFinished(new PlaywrightSharpException("Response body is unavailable for redirect responses"));
            }
            else
            {
                _requests.TryRemove(request.Id, out var _);
                response.RequestFinished();
            }

            _page.FrameManager.RequestFinished(request.Request);
        }

        private void OnRequestFailed(NetworkRequestFailedFirefoxEvent e)
        {
            if (!_requests.TryRemove(e.RequestId, out var request))
            {
                return;
            }

            var response = request.Request.Response;
            response?.RequestFinished();

            request.Request.Failure = e.ErrorCode;
            _page.FrameManager.RequestFailed(request.Request, e.ErrorCode == "NS_BINDING_ABORTED");
        }
    }
}
