using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Fetch;
using PlaywrightSharp.Chromium.Protocol.Network;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumInterceptableRequest : IRequestDelegate
    {
        private readonly ChromiumSession _client;

        public ChromiumInterceptableRequest(
            ChromiumSession client,
            Frame frame,
            string interceptionId,
            string documentId,
            bool requestInterceptionEnabled,
            NetworkRequestWillBeSentChromiumEvent e,
            List<Request> redirectChain)
        {
            _client = client;
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

        public async Task ContinueAsync(Payload payload = null)
        {
            try
            {
                payload ??= new Payload();

                await _client.SendAsync(new FetchContinueRequestRequest
                {
                    RequestId = InterceptionId,
                    Headers = payload.Headers?.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray(),
                    Method = payload.Method?.ToString(),
                    PostData = payload.PostData,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public async Task FulfillAsync(ResponseData response)
        {
            var responseHeaders = response.Headers?.ToDictionary(header => header.Key.ToLower(), header => header.Value)
                ?? new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(response.ContentType))
            {
                responseHeaders["content-type"] = response.ContentType;
            }

            if (response.BodyData != null && !responseHeaders.ContainsKey("content-length"))
            {
                responseHeaders["content-length"] = response.BodyData.Length.ToString();
            }

            try
            {
                await _client.SendAsync(new FetchFulfillRequestRequest
                {
                    RequestId = InterceptionId!,
                    ResponseCode = (int)(response.Status ?? HttpStatusCode.OK),
                    ResponsePhrase = ReasonPhrases.GetReasonPhrase((int)(response.Status ?? HttpStatusCode.OK)),
                    ResponseHeaders = responseHeaders.Select(kv => new HeaderEntry { Name = kv.Key.ToLower(), Value = kv.Value }).ToArray(),
                    Body = response.BodyData,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}
