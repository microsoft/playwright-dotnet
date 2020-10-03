using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    public class Route : ChannelOwnerBase, IChannelOwner<Route>
    {
        private readonly RouteChannel _channel;
        private readonly RouteInitializer _initializer;

        internal Route(IChannelOwner parent, string guid, RouteInitializer initializer) : base(parent, guid)
        {
            _channel = new RouteChannel(guid, parent.Connection, this);
            _initializer = initializer;
        }

        /// <summary>
        /// A request to be routed.
        /// </summary>
        public IRequest Request => _initializer.Request;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Route> IChannelOwner<Route>.Channel => _channel;

        /// <summary>
        /// Fulfills route's request with given response.
        /// </summary>
        /// <param name="status">Status code of the response.</param>
        /// <param name="body">Optional response body as text.</param>
        /// <param name="headers">Optional response headers.</param>
        /// <param name="contentType">If set, equals to setting Content-Type response header.</param>
        /// <param name="bodyContent">Optional response body as binary.</param>
        /// <param name="path">Optional file path to respond with. The content type will be inferred from file extension.
        /// If path is a relative path, then it is resolved relative to current working directory.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was sent.</returns>
        public Task FulfillAsync(
            HttpStatusCode? status = null,
            string body = null,
            Dictionary<string, string> headers = null,
            string contentType = null,
            byte[] bodyContent = null,
            string path = null)
        {
#pragma warning disable CA1062 // Validate arguments of public methods
            var normalized = NormalizeFulfillParameters(status, headers, contentType, body, bodyContent, path);
#pragma warning restore CA1062 // Validate arguments of public methods
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
        /// <param name="method">HTTP method.</param>
        /// <param name="postData">Post data.</param>
        /// <param name="headers">HTTP headers.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was sent.</returns>
        public Task ContinueAsync(HttpMethod method = null, string postData = null, Dictionary<string, string> headers = null)
            => _channel.ContinueAsync(method, postData, headers);

        private NormalizedFulfillResponse NormalizeFulfillParameters(
            HttpStatusCode? status,
            Dictionary<string, string> headers,
            string contentType,
            string body,
            byte[] bodyContent,
            string path)
        {
            string resultBody = string.Empty;
            bool isBase64 = false;
            int length = 0;

            if (!string.IsNullOrEmpty(path))
            {
                byte[] content = File.ReadAllBytes(path);
                resultBody = Convert.ToBase64String(content);
                isBase64 = true;
                length = resultBody.Length;
            }
            else if (!string.IsNullOrEmpty(body))
            {
                resultBody = body;
                isBase64 = false;
                length = resultBody.Length;
            }
            else if (bodyContent != null)
            {
                resultBody = Convert.ToBase64String(bodyContent);
                isBase64 = true;
                length = resultBody.Length;
            }

            var resultHeaders = new Dictionary<string, string>();

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    resultHeaders[header.Key.ToLower()] = header.Value;
                }
            }

            if (!string.IsNullOrEmpty(contentType))
            {
                resultHeaders["content-type'"] = contentType;
            }
            else if (!string.IsNullOrEmpty(path))
            {
                resultHeaders["content-type"] = path.GetContentType();
            }

            if (length > 0 && !resultHeaders.ContainsKey("content-length"))
            {
                resultHeaders["content-length"] = length.ToString();
            }

            return new NormalizedFulfillResponse
            {
                Status = (int)(status ?? HttpStatusCode.OK),
                Headers = resultHeaders.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray(),
                Body = resultBody,
                IsBase64 = isBase64,
            };
        }
    }
}
