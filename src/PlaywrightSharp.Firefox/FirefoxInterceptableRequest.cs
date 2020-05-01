using System;
using System.Collections.Generic;
using System.Linq;
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
        {
            throw new System.NotImplementedException();
        }

        public Task ContinueAsync(Payload payload = null)
        {
            throw new System.NotImplementedException();
        }

        public Task FulfillAsync(ResponseData response)
        {
            throw new System.NotImplementedException();
        }
    }
}
