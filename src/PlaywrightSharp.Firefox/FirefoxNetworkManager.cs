using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            _requests.TryGetValue(e.RedirectedFrom ?? string.Empty, out var redirectd);
            var frame = (redirectd != null ? redirectd.Request.Frame : (e.FrameId != null ? _page.FrameManager.Frames[e.FrameId] : null)) as Frame;
            if (frame == null)
            {
                return;
            }

            var redirectChain = new List<Request>();
            if (redirectd != null)
            {
                redirectChain = new List<Request>(redirectd.Request.RawRedirectChain);
                redirectChain.Add(redirectd.Request);
                _requests.TryRemove(redirectd.Id, out var _);
            }

            var request = new FirefoxRequest(_session, frame, redirectChain.ToArray(), e);
            _requests[request.Id] = request;
            _page.FrameManager.RequestStarted(request.Request);
        }

        private void OnResponseReceived(NetworkResponseReceivedFirefoxEvent e)
        {
            throw new NotImplementedException();
        }

        private void OnRequestFinished(NetworkRequestFinishedFirefoxEvent e)
        {
            throw new NotImplementedException();
        }

        private void OnRequestFailed(NetworkRequestFailedFirefoxEvent e)
        {
            throw new NotImplementedException();
        }
    }
}
