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
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels;

internal class APIRequestContextChannel : Channel<APIRequestContext>
{
    public APIRequestContextChannel(string guid, Connection connection, APIRequestContext owner) : base(guid, connection, owner)
    {
    }

    internal Task DisposeAsync() => Connection.SendMessageToServerAsync(Guid, "dispose");

    internal async Task<IAPIResponse> FetchAsync(
        string url,
        IEnumerable<KeyValuePair<string, string>> parameters,
        string method,
        IEnumerable<KeyValuePair<string, string>> headers,
        object jsonData,
        byte[] postData,
        FormData formData,
        FormData multipartData,
        float? timeout,
        bool? failOnStatusCode,
        bool? ignoreHTTPSErrors,
        int? maxRedirects)
    {
        var message = new Dictionary<string, object>
        {
            ["url"] = url,
            ["method"] = method,
            ["failOnStatusCode"] = failOnStatusCode,
            ["ignoreHTTPSErrors"] = ignoreHTTPSErrors,
            ["maxRedirects"] = maxRedirects,
            ["timeout"] = timeout,
            ["params"] = parameters?.ToProtocol(),
            ["headers"] = headers?.ToProtocol(),
            ["jsonData"] = jsonData,
            ["postData"] = postData != null ? Convert.ToBase64String(postData) : null,
            ["formData"] = formData?.ToProtocol(),
            ["multipartData"] = multipartData?.ToProtocol(),
        };

        var response = await Connection.SendMessageToServerAsync(Guid, "fetch", message).ConfigureAwait(false);
        return new Core.APIResponse(Object, response?.GetProperty("response").ToObject<Protocol.APIResponse>());
    }

    internal Task<StorageState> StorageStateAsync()
        => Connection.SendMessageToServerAsync<StorageState>(Guid, "storageState", null);

    internal async Task<string> FetchResponseBodyAsync(string fetchUid)
    {
        var response = await Connection.SendMessageToServerAsync(Guid, "fetchResponseBody", new Dictionary<string, object> { ["fetchUid"] = fetchUid }).ConfigureAwait(false);
        if (response?.TryGetProperty("binary", out var binary) == true)
        {
            return binary.ToString();
        }
        return null;
    }

    internal async Task<List<string>> FetchResponseLogAsync(string fetchUid)
    {
        var response = await Connection.SendMessageToServerAsync(Guid, "fetchLog", new Dictionary<string, object> { ["fetchUid"] = fetchUid }).ConfigureAwait(false);
        return response.Value.GetProperty("log").ToObject<List<string>>();
    }

    internal Task DisposeAPIResponseAsync(string fetchUid)
        => Connection.SendMessageToServerAsync(Guid, "disposeAPIResponse", new Dictionary<string, object> { ["fetchUid"] = fetchUid });
}
