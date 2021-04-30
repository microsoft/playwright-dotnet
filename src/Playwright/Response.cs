using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    /// <inheritdoc cref="IResponse" />
    public class Response : ChannelOwnerBase, IChannelOwner<Response>, IResponse
    {
        private readonly ResponseChannel _channel;
        private readonly ResponseInitializer _initializer;
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal Response(IChannelOwner parent, string guid, ResponseInitializer initializer) : base(parent, guid)
        {
            _channel = new ResponseChannel(guid, parent.Connection, this);
            _initializer = initializer;
            _initializer.Request.Object.Timing = _initializer.Timing;

            if (initializer.Headers != null)
            {
                foreach (var kv in initializer.Headers)
                {
                    _headers[kv.Name] = kv.Value;
                }
            }
        }

        /// <inheritdoc />
        public IFrame Frame => _initializer.Request.Object.Frame;

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<string, string>> Headers => _headers;

        /// <inheritdoc />
        public bool Ok => Status is 0 or >= 200 and <= 299;

        /// <inheritdoc />
        public IRequest Request => _initializer.Request.Object;

        /// <inheritdoc />
        public int Status => _initializer.Status;

        /// <inheritdoc />
        public string StatusText => _initializer.StatusText;

        /// <inheritdoc />
        public string Url => _initializer.Url;

        /// <inheritdoc/>
        public HttpStatusCode StatusCode => (HttpStatusCode)this.Status;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Response> IChannelOwner<Response>.Channel => _channel;

        /// <inheritdoc />
        public async Task<byte[]> GetBodyAsync() => Convert.FromBase64String(await _channel.GetBodyAsync().ConfigureAwait(false));

        /// <inheritdoc />
        public Task<string> GetFinishedAsync() => _channel.FinishedAsync();

        /// <inheritdoc />
        public async Task<T> GetJsonAsync<T>()
        {
            string content = await GetTextAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(content, _channel.Connection.GetDefaultJsonSerializerOptions());
        }

        /// <inheritdoc/>
        public async Task<JsonDocument> GetJsonAsync(JsonDocumentOptions options = default)
        {
            string content = await GetTextAsync().ConfigureAwait(false);
            return JsonDocument.Parse(content, options);
        }

        /// <inheritdoc />
        public async Task<string> GetTextAsync()
        {
            byte[] content = await GetBodyAsync().ConfigureAwait(false);
            return Encoding.UTF8.GetString(content);
        }
    }
}
