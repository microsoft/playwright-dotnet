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
using System.Net;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    /// <summary>
    /// <see cref="IRoute"/>.
    /// </summary>
    internal partial class Route : ChannelOwnerBase, IChannelOwner<Route>, IRoute
    {
        private readonly RouteChannel _channel;
        private readonly RouteInitializer _initializer;

        internal Route(IChannelOwner parent, string guid, RouteInitializer initializer) : base(parent, guid)
        {
            _channel = new(guid, parent.Connection, this);
            _initializer = initializer;
        }

        /// <summary>
        /// A request to be routed.
        /// </summary>
        public IRequest Request => _initializer.Request;

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Route> IChannelOwner<Route>.Channel => _channel;

        public Task FulfillAsync(RouteFulfillOptions options = default)
        {
            options ??= new RouteFulfillOptions();
            var normalized = NormalizeFulfillParameters(
                options.Status,
                options.Headers,
                options.ContentType,
                options.Body,
                options.BodyBytes,
                options.Path);

            return RaceWithPageCloseAsync(_channel.FulfillAsync(normalized));
        }

        public Task AbortAsync(string errorCode = RequestAbortErrorCode.Failed) => RaceWithPageCloseAsync(_channel.AbortAsync(errorCode));

        public Task ContinueAsync(RouteContinueOptions options = default)
        {
            options ??= new RouteContinueOptions();
            return RaceWithPageCloseAsync(_channel.ContinueAsync(url: options.Url, method: options.Method, postData: options.PostData, headers: options.Headers));
        }

        private async Task RaceWithPageCloseAsync(Task task)
        {
            var page = (Page)Request.Frame.Page;
            if (page == null)
            {
                task.IgnoreException();
                return;
            }

            // When page closes or crashes, we catch any potential rejects from this Route.
            // Note that page could be missing when routing popup's initial request that
            // does not have a Page initialized just yet.
            if (task != await Task.WhenAny(task.ContinueWith(t => t.Exception.Handle(_ => true), System.Threading.CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default), page.ClosedOrCrashedTcs.Task).ConfigureAwait(false))
            {
                task.IgnoreException();
            }
        }

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
                resultHeaders["content-type"] = contentType;
            }
            else if (!string.IsNullOrEmpty(path))
            {
                resultHeaders["content-type"] = path.GetContentType();
            }

            if (length > 0 && !resultHeaders.ContainsKey("content-length"))
            {
                resultHeaders["content-length"] = length.ToString();
            }

            return new()
            {
                Status = status ?? (int)HttpStatusCode.OK,
                Headers = resultHeaders.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray(),
                Body = resultBody,
                IsBase64 = isBase64,
            };
        }
    }
}
