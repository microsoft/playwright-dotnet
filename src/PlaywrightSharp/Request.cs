using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IRequest" />
    public class Request : ChannelOwnerBase, IChannelOwner<Request>, IRequest
    {
        private readonly RequestChannel _channel;
        private readonly RequestInitializer _initializer;

        internal Request(IChannelOwner parent, string guid, RequestInitializer initializer) : base(parent, guid)
        {
            _channel = new RequestChannel(guid, parent.Connection, this);
            _initializer = initializer;
            RedirectedFrom = _initializer.RedirectedFrom?.Object;
            PostDataBuffer = _initializer.PostData != null ? Convert.FromBase64String(_initializer.PostData) : null;
            Timing = new ResourceTiming();

            if (RedirectedFrom != null)
            {
                _initializer.RedirectedFrom.Object.RedirectedTo = this;
            }

            if (initializer.Headers != null)
            {
                foreach (var kv in initializer.Headers)
                {
                    Headers[kv.Name] = kv.Value;
                }
            }
        }

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Request> IChannelOwner<Request>.Channel => _channel;

        /// <inheritdoc />
        public ResourceTiming Timing { get; internal set; }

        /// <inheritdoc />
        public string Url => _initializer.Url;

        /// <inheritdoc />
        public HttpMethod Method => _initializer.Method;

        /// <inheritdoc />
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public string PostData => PostDataBuffer == null ? null : Encoding.UTF8.GetString(PostDataBuffer);

        /// <inheritdoc />
        public byte[] PostDataBuffer { get; }

        /// <inheritdoc />
        public IFrame Frame => _initializer.Frame;

        /// <inheritdoc />
        public bool IsNavigationRequest => _initializer.IsNavigationRequest;

        /// <inheritdoc />
        public ResourceType ResourceType => _initializer.ResourceType;

        /// <inheritdoc />
        public string Failure { get; internal set; }

        /// <inheritdoc />
        public IRequest RedirectedFrom { get; }

        /// <inheritdoc />
        public IRequest RedirectedTo { get; internal set; }

        internal Request FinalRequest => RedirectedTo != null ? ((Request)RedirectedTo).FinalRequest : this;

        /// <inheritdoc />
        public async Task<IResponse> GetResponseAsync() => (await _channel.GetResponseAsync().ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public JsonDocument GetPostDataJson(JsonDocumentOptions options = default)
        {
            string content = GetRequestForJson();

            if (content == null)
            {
                return null;
            }

            return JsonDocument.Parse(content, options);
        }

        private string GetRequestForJson()
        {
            if (PostData == null)
            {
                return null;
            }

            if (Headers.TryGetValue("content-type", out string contentType) && string.IsNullOrEmpty(contentType))
            {
                return null;
            }

            if (contentType == "application/x-www-form-urlencoded")
            {
                var parsed = HttpUtility.ParseQueryString(PostData);
                var dictionary = new Dictionary<string, string>();

                foreach (string key in parsed.Keys)
                {
                    dictionary[key] = parsed[key];
                }

                return JsonSerializer.Serialize(dictionary);
            }

            return PostData;
        }
    }
}
