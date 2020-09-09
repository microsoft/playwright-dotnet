using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IRequest" />
    public class Request : IChannelOwner<Request>, IRequest
    {
        private readonly ConnectionScope _scope;
        private readonly RequestChannel _channel;
        private readonly RequestInitializer _initializer;
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal Request(ConnectionScope scope, string guid, RequestInitializer initializer)
        {
            _scope = scope;
            _channel = new RequestChannel(guid, scope, this);
            _initializer = initializer;
            RedirectedFrom = _initializer.RedirectedFrom?.Object;

            if (RedirectedFrom != null)
            {
                _initializer.RedirectedFrom.Object.RedirectedTo = this;
            }

            if (initializer.Headers != null)
            {
                foreach (var kv in initializer.Headers)
                {
                    _headers[kv.Name] = kv.Value;
                }
            }
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Request> IChannelOwner<Request>.Channel => _channel;

        /// <inheritdoc />
        public string Url => _initializer.Url;

        /// <inheritdoc />
        public HttpMethod Method => _initializer.Method;

        /// <inheritdoc />
        public IDictionary<string, string> Headers => _headers;

        /// <inheritdoc />
        public string PostData => _initializer.PostData;

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
        public JsonDocument GetJsonAsync(JsonDocumentOptions options = default)
        {
            string content = GetRequestForJson();

            if (content == null)
            {
                return null;
            }

            return JsonDocument.Parse(content, options);
        }

        /// <inheritdoc />
        public T GetJsonAsync<T>(JsonSerializerOptions options = null)
        {
            string content = GetRequestForJson();

            if (content == null)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(content, options ?? _channel.Scope.Connection.GetDefaultJsonSerializerOptions());
        }

        private string GetRequestForJson()
        {
            if (_initializer.PostData == null)
            {
                return null;
            }

            if (Headers.TryGetValue("content-type", out string contentType) && string.IsNullOrEmpty(contentType))
            {
                return null;
            }

            if (contentType == "application/x-www-form-urlencoded")
            {
                var parsed = HttpUtility.ParseQueryString(_initializer.PostData);
                var dictionary = new Dictionary<string, string>();

                foreach (string key in parsed.Keys)
                {
                    dictionary[key] = parsed[key];
                }

                return JsonSerializer.Serialize(dictionary);
            }

            return _initializer.PostData;
        }
    }
}
