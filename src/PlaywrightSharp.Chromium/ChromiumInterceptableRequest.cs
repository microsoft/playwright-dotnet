using System;
using System.Net.Http;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Network;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumInterceptableRequest : IRequestDelegate
    {
        public ChromiumInterceptableRequest(
            ChromiumSession client,
            Frame frame,
            string interceptionId,
            string documentId,
            bool requestInterceptionEnabled,
            NetworkRequestWillBeSentChromiumEvent e,
            Request[] redirectChain)
        {
            RequestId = e.RequestId;
            InterceptionId = interceptionId;
            Request = new Request(
                this,
                frame,
                redirectChain,
                documentId,
                e.Request.Url,
                (e.Type ?? Protocol.Network.ResourceType.Other).ToPlaywrightResourceType(),
                new HttpMethod(e.Request.Method),
                e.Request.PostData,
                e.Request.Headers);
        }

        public string InterceptionId { get; }

        public string RequestId { get; }

        public Request Request { get; }

        public Task AbortAsync(RequestAbortErrorCode errorCode = RequestAbortErrorCode.Failed) => throw new NotImplementedException();

        public Task ContinueAsync(Payload payload = null) => throw new NotImplementedException();

        public Task FulfillAsync(ResponseData response) => throw new NotImplementedException();
    }
}
