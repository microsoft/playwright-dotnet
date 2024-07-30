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

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core;

internal class APIRequest : IAPIRequest
{
    private readonly PlaywrightImpl _playwright;

    public APIRequest(PlaywrightImpl playwright)
    {
        _playwright = playwright;
    }

    async Task<IAPIRequestContext> IAPIRequest.NewContextAsync(APIRequestNewContextOptions options)
    {
        var args = new Dictionary<string, object>()
        {
            ["baseURL"] = options?.BaseURL,
            ["userAgent"] = options?.UserAgent,
            ["ignoreHTTPSErrors"] = options?.IgnoreHTTPSErrors,
            ["extraHTTPHeaders"] = options?.ExtraHTTPHeaders?.ToProtocol(),
            ["httpCredentials"] = options?.HttpCredentials,
            ["proxy"] = options?.Proxy,
            ["timeout"] = options?.Timeout,
            ["clientCertificates"] = Browser.ToClientCertificatesProtocol(options?.ClientCertificates),
        };
        string storageState = options?.StorageState;
        if (!string.IsNullOrEmpty(options?.StorageStatePath))
        {
            if (!File.Exists(options?.StorageStatePath))
            {
                throw new PlaywrightException($"The specified storage state file does not exist: {options?.StorageStatePath}");
            }

            storageState = File.ReadAllText(options?.StorageStatePath);
        }
        if (!string.IsNullOrEmpty(storageState))
        {
            args.Add("storageState", JsonSerializer.Deserialize<StorageState>(storageState, Helpers.JsonExtensions.DefaultJsonSerializerOptions));
        }

        var context = await _playwright.SendMessageToServerAsync<APIRequestContext>(
            "newRequest",
            args).ConfigureAwait(false);
        context._request = this;
        return context;
    }
}
