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
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport.Channels;

internal class RouteChannel : Channel<Route>
{
    public RouteChannel(string guid, Connection connection, Route owner) : base(guid, connection, owner)
    {
    }

    public Task AbortAsync(string requestUrl, string errorCode)
        => Connection.SendMessageToServerAsync(
            Guid,
            "abort",
            new Dictionary<string, object>
            {
                ["requestUrl"] = requestUrl,
                ["errorCode"] = string.IsNullOrEmpty(errorCode) ? RequestAbortErrorCode.Failed : errorCode,
            });

    public Task FulfillAsync(Dictionary<string, object> args)
        => Connection.SendMessageToServerAsync(
            Guid,
            "fulfill",
            args);

    public Task ContinueAsync(string requestUrl, string url, string method, byte[] postData, IEnumerable<KeyValuePair<string, string>> headers, bool isFallback)
    {
        var args = new Dictionary<string, object>
        {
            ["requestUrl"] = requestUrl,
            ["url"] = url,
            ["method"] = method,
            ["postData"] = postData != null ? Convert.ToBase64String(postData) : null,
            ["headers"] = headers?.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray(),
            ["isFallback"] = isFallback,
        };

        return Connection.SendMessageToServerAsync(
            Guid,
            "continue",
            args);
    }

    internal Task RedirectNavigationRequestAsync(string url) =>
        Connection.SendMessageToServerAsync(
            Guid,
            "redirectNavigationRequest",
            new Dictionary<string, object>
            {
                ["url"] = url,
            });
}
