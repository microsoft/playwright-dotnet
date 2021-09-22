/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal class Request : ChannelOwnerBase, IChannelOwner<Request>, IRequest
    {
        private readonly RequestChannel _channel;
        private readonly RequestInitializer _initializer;
        private readonly NameValueCollection _headers = new();
        private NameValueCollection _rawHeaders;

        internal Request(IChannelOwner parent, string guid, RequestInitializer initializer) : base(parent, guid)
        {
            // TODO: Consider using a mapper between RequestInitiliazer and this object
            _channel = new(guid, parent.Connection, this);
            _initializer = initializer;
            RedirectedFrom = _initializer.RedirectedFrom;
            PostDataBuffer = _initializer.PostData != null ? Convert.FromBase64String(_initializer.PostData) : null;
            Timing = new();

            if (RedirectedFrom != null)
            {
                _initializer.RedirectedFrom.RedirectedTo = this;
            }

            foreach (var kv in initializer.Headers)
            {
                _headers.Add(kv.Name.ToLower(), kv.Value);
            }
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Request> IChannelOwner<Request>.Channel => _channel;

        public string Failure { get; internal set; }

        public IFrame Frame => _initializer.Frame;

        public Dictionary<string, string> Headers
        {
            get
            {
                return _headers.Keys.Cast<string>().Select<string, (string Key, string Value)>(
                    x => new(x, string.Join(", ", _headers.GetValues(x).Distinct()))).ToDictionary(x => x.Key, y => y.Value);
            }
        }

        public bool IsNavigationRequest => _initializer.IsNavigationRequest;

        public string Method => _initializer.Method;

        public string PostData => PostDataBuffer == null ? null : Encoding.UTF8.GetString(PostDataBuffer);

        public byte[] PostDataBuffer { get; }

        public IRequest RedirectedFrom { get; }

        public IRequest RedirectedTo { get; internal set; }

        public string ResourceType => _initializer.ResourceType;

        public RequestTimingResult Timing { get; internal set; }

        public string Url => _initializer.Url;

        internal Request FinalRequest => RedirectedTo != null ? ((Request)RedirectedTo).FinalRequest : this;

        public RequestSizesResult Sizes { get; internal set; }

        public async Task<IResponse> ResponseAsync() => (await _channel.GetResponseAsync().ConfigureAwait(false))?.Object;

        public JsonElement? PostDataJSON()
        {
            if (PostData == null)
            {
                return null;
            }

            string content = PostData;
            Headers.TryGetValue("content-type", out string contentType);
            if (contentType == "application/x-www-form-urlencoded")
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

            return JsonDocument.Parse(content).RootElement;
        }

        public async Task<Dictionary<string, string>> AllHeadersAsync() =>
            (from key in (await GetRawHeadersAsync().ConfigureAwait(false)).Cast<string>()
             from value in _headers.GetValues(key)
             select (key, value)).ToDictionary(x => x.key, y => y.value);

        public Task<NameValueCollection> HeadersArrayAsync() => GetRawHeadersAsync();

        public async Task<string> HeaderValueAsync(string name) => (await GetRawHeadersAsync().ConfigureAwait(false)).Get(name);

        public async Task<RequestSizesResult> SizesAsync()
        {
            if (await ResponseAsync().ConfigureAwait(false) is not IChannelOwner<Response> res)
            {
                throw new PlaywrightException("Unable to fetch sizes for a failed request.");
            }

            return await ((ResponseChannel)res.Channel).SizesAsync().ConfigureAwait(false);
        }

        private async Task<NameValueCollection> GetRawHeadersAsync()
        {
            if (_rawHeaders != null) return _rawHeaders;

            _rawHeaders = new NameValueCollection();
            var headers = await _channel.GetRawHeadersAsync().ConfigureAwait(false);
            foreach (var header in headers)
            {
                _rawHeaders.Add(header.Name, header.Value);
            }
            return _rawHeaders;
        }
    }
}
