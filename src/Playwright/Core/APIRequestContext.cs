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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal class APIRequestContext : ChannelOwnerBase, IChannelOwner<APIRequestContext>, IAPIRequestContext
    {
        internal readonly APIRequestContextChannel _channel;
        internal readonly Tracing _tracing;

        internal APIRequest _request;

        public APIRequestContext(IChannelOwner parent, string guid, APIRequestContextInitializer initializer) : base(parent, guid)
        {
            _channel = new(guid, parent.Connection, this);
            _tracing = initializer.Tracing;
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<APIRequestContext> IChannelOwner<APIRequestContext>.Channel => _channel;

        public ValueTask DisposeAsync() => new(_channel.DisposeAsync());

        public async Task<IAPIResponse> FetchAsync(IRequest urlOrRequest, IRequestOptions optionsArg = null)
        {
            var options = (RequestOptions)optionsArg ?? new RequestOptions();
            if (options.Method == null)
            {
                options.SetMethod(urlOrRequest.Method);
            }
            if (options.Headers == null)
            {
                options.Headers = await urlOrRequest.AllHeadersAsync().ConfigureAwait(false);
            }
            if (options.Data == null && options.Form == null && options.MultiPart == null)
            {
                options.Data = urlOrRequest.PostDataBuffer;
            }
            return await FetchAsync(urlOrRequest.Url, options).ConfigureAwait(false);
        }

        public async Task<IAPIResponse> FetchAsync(string url, IRequestOptions optionsArg = null)
        {
            var options = optionsArg != null ? (RequestOptions)optionsArg : new RequestOptions();
            var queryParams = new Dictionary<string, string>();
            if (options.Parameters != null)
            {
                queryParams = options.Parameters.ToDictionary(x => x.Key, x => x.Value.ToString());
            }
            byte[] postData = null;
            object jsonData = null;
            if (options.Data != null)
            {
                if (options.Data is string dataString)
                {
                    if (IsJsonContentType(options.Headers))
                    {
                        jsonData = options.Data;
                    }
                    else
                    {
                        postData = System.Text.Encoding.UTF8.GetBytes(dataString);
                    }
                }
                else if (options.Data is byte[] data)
                {
                    postData = data;
                }
                else
                {
                    jsonData = options.Data;
                }
            }

            return await _channel.FetchAsync(
                url,
                queryParams,
                options.Method,
                options.Headers,
                jsonData,
                postData,
                options.Form,
                options.MultiPart,
                options.Timeout,
                options?.FailOnStatusCode,
                options?.IgnoreHTTPSErrors).ConfigureAwait(false);
        }

        private bool IsJsonContentType(IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                return false;
            }
            var contentType = headers.FirstOrDefault(x => x.Key.ToLower().Equals("content-type", StringComparison.OrdinalIgnoreCase));
            if (contentType.Value == null)
            {
                return false;
            }
            return contentType.Value.Contains("application/json");
        }

        public Task<IAPIResponse> DeleteAsync(string url, IRequestOptions options = null) => FetchAsync(url, EnsureOptions(options).SetMethod("DELETE"));

        public Task<IAPIResponse> GetAsync(string url, IRequestOptions options = null) => FetchAsync(url, EnsureOptions(options).SetMethod("GET"));

        public Task<IAPIResponse> HeadAsync(string url, IRequestOptions options = null) => FetchAsync(url, EnsureOptions(options).SetMethod("HEAD"));

        public Task<IAPIResponse> PatchAsync(string url, IRequestOptions options = null) => FetchAsync(url, EnsureOptions(options).SetMethod("PATCH"));

        public Task<IAPIResponse> PostAsync(string url, IRequestOptions options = null) => FetchAsync(url, EnsureOptions(options).SetMethod("POST"));

        public Task<IAPIResponse> PutAsync(string url, IRequestOptions options = null) => FetchAsync(url, EnsureOptions(options).SetMethod("PUT"));

        private IRequestOptions EnsureOptions(IRequestOptions options) => options ?? new RequestOptions();

        public async Task<string> StorageStateAsync(APIRequestContextStorageStateOptions options = null)
        {
            string state = JsonSerializer.Serialize(
                await _channel.StorageStateAsync().ConfigureAwait(false),
                JsonExtensions.DefaultJsonSerializerOptions);

            if (!string.IsNullOrEmpty(options?.Path))
            {
                File.WriteAllText(options?.Path, state);
            }

            return state;
        }
    }
}
