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
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels;

internal class PlaywrightChannel : Channel<PlaywrightImpl>
{
    public PlaywrightChannel(string guid, Connection connection, PlaywrightImpl owner) : base(guid, connection, owner)
    {
    }

    internal async Task<APIRequestContext> NewRequestAsync(
        string baseURL,
        string userAgent,
        bool? ignoreHTTPSErrors,
        IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders,
        HttpCredentials httpCredentials = null,
        Proxy proxy = null,
        float? timeout = null,
        string storageState = null,
        string storageStatePath = null)
    {
        IDictionary<string, object> args = new Dictionary<string, object>()
        {
            ["baseURL"] = baseURL,
            ["userAgent"] = userAgent,
            ["ignoreHTTPSErrors"] = ignoreHTTPSErrors,
            ["extraHTTPHeaders"] = extraHTTPHeaders?.ToProtocol(),
            ["httpCredentials"] = httpCredentials,
            ["proxy"] = proxy,
            ["timeout"] = timeout,
        };
        if (!string.IsNullOrEmpty(storageStatePath))
        {
            if (!File.Exists(storageStatePath))
            {
                throw new PlaywrightException($"The specified storage state file does not exist: {storageStatePath}");
            }

            storageState = File.ReadAllText(storageStatePath);
        }
        if (!string.IsNullOrEmpty(storageState))
        {
            args.Add("storageState", JsonSerializer.Deserialize<StorageState>(storageState, Helpers.JsonExtensions.DefaultJsonSerializerOptions));
        }

        var response = await Connection.SendMessageToServerAsync<APIRequestContextChannel>(
            Guid,
            "newRequest",
            args).ConfigureAwait(false);
        return response.Object;
    }
}
