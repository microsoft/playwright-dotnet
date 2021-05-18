using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    public class Request : ChannelOwnerBase, IChannelOwner<Request>, IRequest
    {
        private readonly RequestChannel _channel;
        private readonly RequestInitializer _initializer;

        internal Request(IChannelOwner parent, string guid, RequestInitializer initializer) : base(parent, guid)
        {
            // TODO: Consider using a mapper between RequestInitiliazer and this object
            _channel = new RequestChannel(guid, parent.Connection, this);
            _initializer = initializer;
            RedirectedFrom = _initializer.RedirectedFrom?.Object;
            PostDataBuffer = _initializer.PostData != null ? Convert.FromBase64String(_initializer.PostData) : null;
            Timing = new RequestTimingResult();

            if (RedirectedFrom != null)
            {
                _initializer.RedirectedFrom.Object.RedirectedTo = this;
            }

            if (initializer.Headers != null)
            {
                Headers = initializer.Headers.Select(x => new KeyValuePair<string, string>(x.Name, x.Value)).ToArray();
            }
            else
            {
                Headers = Array.Empty<KeyValuePair<string, string>>();
            }
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Request> IChannelOwner<Request>.Channel => _channel;

        public string Failure { get; internal set; }

        public IFrame Frame => _initializer.Frame;

        public IEnumerable<KeyValuePair<string, string>> Headers { get; }

        public bool IsNavigationRequest => _initializer.IsNavigationRequest;

        public string Method => _initializer.Method.Method;

        public string PostData => PostDataBuffer == null ? null : Encoding.UTF8.GetString(PostDataBuffer);

        public byte[] PostDataBuffer { get; }

        public IRequest RedirectedFrom { get; }

        public IRequest RedirectedTo { get; internal set; }

        public string ResourceType => _initializer.ResourceType;

        public RequestTimingResult Timing { get; internal set; }

        public string Url => _initializer.Url;

        internal Request FinalRequest => RedirectedTo != null ? ((Request)RedirectedTo).FinalRequest : this;

        public async Task<IResponse> ResponseAsync() => (await _channel.GetResponseAsync().ConfigureAwait(false))?.Object;

        public JsonDocument PostDataJSON()
        {
            if (PostData == null)
            {
                return null;
            }

            string content = PostData;

            if ("application/x-www-form-urlencoded".Equals(this.GetHeaderValue("content-type"), StringComparison.OrdinalIgnoreCase))
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

            return JsonDocument.Parse(content);
        }
    }
}
