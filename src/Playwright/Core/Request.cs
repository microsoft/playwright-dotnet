using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal class Request : ChannelOwnerBase, IChannelOwner<Request>, IRequest
    {
        private readonly RequestChannel _channel;
        private readonly RequestInitializer _initializer;

        internal Request(IChannelOwner parent, string guid, RequestInitializer initializer) : base(parent, guid)
        {
            // TODO: Consider using a mapper between RequestInitiliazer and this object
            _channel = new(guid, parent.Connection, this);
            _initializer = initializer;
            RedirectedFrom = _initializer.RedirectedFrom;
            PostDataBuffer = _initializer.PostData != null ? Convert.FromBase64String(_initializer.PostData) : null;
            Timing = new();

            if (RedirectedFrom != null)
            {
                _initializer.RedirectedFrom.RedirectedTo = this;
            }

            Headers = new();
            foreach (var kv in initializer.Headers)
            {
                var name = kv.Name.ToLower();

                // There are case-sensitive dupes :/
                if (!Headers.ContainsKey(name))
                {
                    Headers.Add(kv.Name.ToLower(), kv.Value);
                }
            }
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Request> IChannelOwner<Request>.Channel => _channel;

        public string Failure { get; internal set; }

        public IFrame Frame => _initializer.Frame;

        public Dictionary<string, string> Headers { get; }

        public bool IsNavigationRequest => _initializer.IsNavigationRequest;

        public string Method => _initializer.Method;

        public string PostData => PostDataBuffer == null ? null : Encoding.UTF8.GetString(PostDataBuffer);

        public byte[] PostDataBuffer { get; }

        public IRequest RedirectedFrom { get; }

        public IRequest RedirectedTo { get; internal set; }

        public string ResourceType => _initializer.ResourceType;

        public RequestTimingResult Timing { get; internal set; }

        public string Url => _initializer.Url;

        internal Request FinalRequest => RedirectedTo != null ? ((Request)RedirectedTo).FinalRequest : this;

        public async Task<IResponse> ResponseAsync() => (await _channel.GetResponseAsync().ConfigureAwait(false))?.Object;

        public JsonElement? PostDataJSON()
        {
            if (PostData == null)
            {
                return null;
            }

            string content = PostData;
            string contentType = string.Empty;
            Headers.TryGetValue("content-type", out contentType);
            if (contentType == "application/x-www-form-urlencoded")
            {
                var parsed = HttpUtility.ParseQueryString(PostData);
                var dictionary = new Dictionary<string, string>();

                foreach (string key in parsed.Keys)
                {
                    dictionary[key] = parsed[key];
                }

                content = JsonSerializer.Serialize(dictionary);
            }

            if (content == null)
            {
                return null;
            }

            return JsonDocument.Parse(content).RootElement;
        }
    }
}
