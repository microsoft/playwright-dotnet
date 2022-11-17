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
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport.Channels;

internal class LocalUtilsChannel : Channel<LocalUtils>
{
    public LocalUtilsChannel(string guid, Connection connection, LocalUtils owner) : base(guid, connection, owner)
    {
    }

    internal Task ZipAsync(string zipFile, List<NameValue> entries) =>
        Object.SendMessageToServerAsync("zip", new Dictionary<string, object>
        {
                  { "zipFile", zipFile },
                  { "entries", entries },
        });

    internal async Task<(string HarId, string Error)> HarOpenAsync(string file)
    {
        var response = await Object.SendMessageToServerAsync("harOpen", new Dictionary<string, object>
            {
                  { "file", file },
            }).ConfigureAwait(false);
        return (response.GetString("harId", true), response.GetString("error", true));
    }

    internal async Task<LocalUtilsHarLookupResult> HarLookupAsync(
        string harId,
        string url,
        string method,
        List<Header> headers,
        byte[] postData,
        bool isNavigationRequest)
    {
        var response = await Object.SendMessageToServerAsync<LocalUtilsHarLookupResult>("harLookup", new Dictionary<string, object>
            {
                { "harId", harId },
                { "url", url },
                { "method", method },
                { "headers", headers },
                { "postData", postData != null ? Convert.ToBase64String(postData) : null },
                { "isNavigationRequest", isNavigationRequest },
            }).ConfigureAwait(false);
        return response;
    }

    internal Task HarCloseAsync(string harId) =>
        Object.SendMessageToServerAsync("HarCloseAsync", new Dictionary<string, object>
        {
                  { "harId", harId },
        });

    internal Task HarUnzipAsync(string zipFile, string harFile) =>
        Object.SendMessageToServerAsync("harUnzip", new Dictionary<string, object>
        {
                  { "zipFile", zipFile },
                  { "harFile", harFile },
        });

    internal Task<JsonPipeChannel> ConnectAsync(string wsEndpoint, IEnumerable<KeyValuePair<string, string>> headers, float? slowMo, float? timeout)
    {
        var args = new Dictionary<string, object>
            {
                { "wsEndpoint", wsEndpoint },
                { "headers", headers },
                { "slowMo", slowMo },
                { "timeout", timeout },
            };
        return Object.SendMessageToServerAsync<JsonPipeChannel>("connect", args);
    }
}
