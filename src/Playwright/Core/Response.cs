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
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal class Response : ChannelOwnerBase, IChannelOwner<Response>, IResponse
    {
        private readonly ResponseChannel _channel;
        private readonly ResponseInitializer _initializer;

        internal Response(IChannelOwner parent, string guid, ResponseInitializer initializer) : base(parent, guid)
        {
            _channel = new(guid, parent.Connection, this);
            _initializer = initializer;
            _initializer.Request.Timing = _initializer.Timing;

            Headers = new();
            foreach (var kv in initializer.Headers)
            {
                var name = kv.Name.ToLower();

                // There are case-sensitive dupes :/
                if (!Headers.ContainsKey(name))
                {
                    Headers.Add(kv.Name.ToLower(), kv.Value);
                }
            }

            _initializer.Request.Headers.Clear();
            foreach (var kv in _initializer.RequestHeaders)
            {
                var name = kv.Name.ToLower();

                // There are case-sensitive dupes :/
                if (!_initializer.Request.Headers.ContainsKey(name))
                {
                    _initializer.Request.Headers.Add(kv.Name.ToLower(), kv.Value);
                }
            }
        }

        public IFrame Frame => _initializer.Request.Frame;

        public Dictionary<string, string> Headers { get; }

        public bool Ok => Status is 0 or >= 200 and <= 299;

        public IRequest Request => _initializer.Request;

        public int Status => _initializer.Status;

        public string StatusText => _initializer.StatusText;

        public string Url => _initializer.Url;

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Response> IChannelOwner<Response>.Channel => _channel;

        public async Task<byte[]> BodyAsync() => Convert.FromBase64String(await _channel.GetBodyAsync().ConfigureAwait(false));

        public Task<string> FinishedAsync() => _channel.FinishedAsync();

        public async Task<JsonElement?> JsonAsync()
        {
            string content = await TextAsync().ConfigureAwait(false);
            return JsonDocument.Parse(content).RootElement;
        }

        public async Task<string> TextAsync()
        {
            byte[] content = await BodyAsync().ConfigureAwait(false);
            return Encoding.UTF8.GetString(content);
        }
    }
}
