using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
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
        public IDictionary<string, string> Headers => _initializer.Headers;

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

        /// <inheritdoc />
        public async Task<IResponse> GetResponseAsync() => (await _channel.GetResponseAsync().ConfigureAwait(false))?.Object;
    }
}
