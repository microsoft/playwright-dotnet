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
    /// <see cref="IRoute"/>.
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

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Route> IChannelOwner<Route>.Channel => _channel;

        public Task FulfillAsync(
            HttpStatusCode status,
            string body = null,
            byte[] bodyBytes = null,
            string contentType = null,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            string path = null)
            => FulfillAsync((int?)status, headers, contentType, body, bodyBytes, path);

        public Task FulfillAsync(
            int? status = default,
            IEnumerable<KeyValuePair<string, string>> headers = default,
            string contentType = default,
            string body = default,
            byte[] bodyBytes = default,
            string path = default)
        {
            var normalized = NormalizeFulfillParameters(status, headers, contentType, body, bodyBytes, path);
            return _channel.FulfillAsync(normalized);
        }

        public Task AbortAsync(string errorCode = RequestAbortErrorCode.Failed) => _channel.AbortAsync(errorCode);

        public Task ContinueAsync(
            string url = default,
            string method = default,
            byte[] postData = default,
            IEnumerable<KeyValuePair<string, string>> headers = default)
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
