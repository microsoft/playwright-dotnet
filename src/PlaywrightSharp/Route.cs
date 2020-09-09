using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <summary>
    /// Whenever a network route is set up with <see cref="IPage.RouteAsync(string, Action{Route, IRequest})"/> or <see cref="IBrowserContext.RouteAsync(string, Action{Route, IRequest})"/> the Route object allows to handle the route.
    /// </summary>
    public class Route : IChannelOwner<Route>
    {
        private readonly ConnectionScope _scope;
        private readonly RouteChannel _channel;
        private readonly RouteInitializer _initializer;

        internal Route(ConnectionScope scope, string guid, RouteInitializer initializer)
        {
            _scope = scope;
            _channel = new RouteChannel(guid, scope, this);
            _initializer = initializer;
        }

        /// <summary>
        /// A request to be routed.
        /// </summary>
        public IRequest Request => _initializer.Request;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Route> IChannelOwner<Route>.Channel => _channel;

        /// <summary>
        /// Fulfills route's request with given response.
        /// </summary>
        /// <param name="response">Response that will fulfill this route's request.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was sent.</returns>
        public Task FulfillAsync(RouteFilfillResponse response)
        {
            var normalized = NormalizeFulfillParameters(response ?? new RouteFilfillResponse());
            return _channel.FulfillAsync(normalized);
        }

        /// <summary>
        /// Aborts the route's request.
        /// </summary>
        /// <param name="errorCode">Optional error code.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was sent.</returns>
        public Task AbortAsync(RequestAbortErrorCode errorCode = RequestAbortErrorCode.Failed) => _channel.AbortAsync(errorCode);

        /// <summary>
        /// Continues route's request with optional overrides.
        /// </summary>
        /// <param name="overrides">Optional request overrides.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was sent.</returns>
        public Task ContinueAsync(RouteContinueOverrides overrides = null) => _channel.ContinueAsync(overrides);

        private NormalizedFulfillResponse NormalizeFulfillParameters(RouteFilfillResponse response)
        {
            string body = string.Empty;
            bool isBase64 = false;
            int length = 0;

            if (!string.IsNullOrEmpty(response.Path))
            {
                byte[] content = File.ReadAllBytes(response.Path);
                body = Convert.ToBase64String(content);
                isBase64 = true;
                length = body.Length;
            }
            else if (!string.IsNullOrEmpty(response.Body))
            {
                body = response.Body;
                isBase64 = false;
                length = body.Length;
            }
            else if (response.BodyContent != null)
            {
                body = Convert.ToBase64String(response.BodyContent);
                isBase64 = true;
                length = body.Length;
            }

            var headers = new Dictionary<string, string>();

            if (response.Headers != null)
            {
                foreach (var header in response.Headers)
                {
                    headers[header.Key.ToLower()] = header.Value;
                }
            }

            if (!string.IsNullOrEmpty(response.ContentType))
            {
                headers["content-type'"] = response.ContentType;
            }
            else if (!string.IsNullOrEmpty(response.Path))
            {
                headers["content-type"] = response.Path.GetContentType();
            }

            if (length > 0 && !headers.ContainsKey("content-length"))
            {
                headers["content-length"] = length.ToString();
            }

            return new NormalizedFulfillResponse
            {
                Status = (int)(response.Status ?? HttpStatusCode.OK),
                Headers = headers.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray(),
                Body = body,
                IsBase64 = isBase64,
            };
        }
    }
}
