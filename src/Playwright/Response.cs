using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    public class Response : ChannelOwnerBase, IChannelOwner<Response>, IResponse
    {
        private readonly ResponseChannel _channel;
        private readonly ResponseInitializer _initializer;

        internal Response(IChannelOwner parent, string guid, ResponseInitializer initializer) : base(parent, guid)
        {
            _channel = new ResponseChannel(guid, parent.Connection, this);
            _initializer = initializer;
            _initializer.Request.Object.Timing = _initializer.Timing;

            Headers = new Dictionary<string, string>();
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

        public IFrame Frame => _initializer.Request.Object.Frame;

        public Dictionary<string, string> Headers { get; }

        public bool Ok => Status is 0 or >= 200 and <= 299;

        public IRequest Request => _initializer.Request.Object;

        public int Status => _initializer.Status;

        public string StatusText => _initializer.StatusText;

        public string Url => _initializer.Url;

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Response> IChannelOwner<Response>.Channel => _channel;

        public async Task<byte[]> BodyAsync() => Convert.FromBase64String(await _channel.GetBodyAsync().ConfigureAwait(false));

        public Task<string> FinishedAsync() => _channel.FinishedAsync();

        public async Task<T> JsonAsync<T>()
        {
            string content = await TextAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(content, _channel.Connection.GetDefaultJsonSerializerOptions());
        }

        public async Task<JsonDocument> JsonAsync(JsonDocumentOptions options = default)
        {
            string content = await TextAsync().ConfigureAwait(false);
            return JsonDocument.Parse(content, options);
        }

        public async Task<string> TextAsync()
        {
            byte[] content = await BodyAsync().ConfigureAwait(false);
            return Encoding.UTF8.GetString(content);
        }
    }
}
