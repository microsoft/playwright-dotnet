using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Whenever a network route is set up with <see cref="IPage.RouteAsync(Func{string, bool}, Action{IRoute})"/> or <see cref="IBrowserContext.RouteAsync(string, System.Text.RegularExpressions.Regex, Func{string, bool}, Action{IRoute})"/> the Route object allows to handle the route.
    /// </summary>
    public class Route : ChannelOwnerBase, IChannelOwner<Route>, IRoute
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

        /// <inheritdoc/>
        public Task FulfillAsync(
            HttpStatusCode status,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            string contentType = null,
            string body = null,
            byte[] bodyBytes = null,
            string path = null)
            => FulfillAsync((int?)status, headers, contentType, body, bodyBytes, path);

        /// <inheritdoc/>
        public Task FulfillAsync(
            int? status = null,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            string contentType = null,
            string body = null,
            byte[] bodyBytes = null,
            string path = null)
        {
            var normalized = NormalizeFulfillParameters(status, headers, contentType, body, bodyBytes, path);
            return _channel.FulfillAsync(normalized);
        }

        /// <inheritdoc/>
        public Task AbortAsync(string errorCode = RequestAbortErrorCode.Failed) => _channel.AbortAsync(errorCode);

        /// <inheritdoc/>
        public Task ResumeAsync(
            string url = null,
            string method = null,
            byte[] postData = null,
            IEnumerable<KeyValuePair<string, string>> headers = null)
            => _channel.ContinueAsync(url, method, postData, headers);

        private NormalizedFulfillResponse NormalizeFulfillParameters(
            int? status,
            IEnumerable<KeyValuePair<string, string>> headers,
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
                Status = status ?? (int)HttpStatusCode.OK,
                Headers = resultHeaders.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray(),
                Body = resultBody,
                IsBase64 = isBase64,
            };
        }
    }
}
