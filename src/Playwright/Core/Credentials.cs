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
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core;

internal class Credentials : ICredentials
{
    private readonly BrowserContext _browserContext;

    public Credentials(BrowserContext browserContext)
    {
        _browserContext = browserContext;
    }

    public Task InstallAsync()
        => _browserContext.SendMessageToServerAsync("credentialsInstall");

    public async Task<VirtualCredential> CreateAsync(string rpId, CredentialsCreateOptions? options = default)
    {
        var result = await _browserContext.SendMessageToServerAsync("credentialsCreate", new Dictionary<string, object?>
        {
            ["rpId"] = rpId,
            ["id"] = options?.Id,
            ["userHandle"] = options?.UserHandle,
            ["privateKey"] = options?.PrivateKey,
            ["publicKey"] = options?.PublicKey,
        }).ConfigureAwait(false);
        return result!.Value.GetProperty("credential").ToObject<VirtualCredential>(_browserContext._connection.DefaultJsonSerializerOptions);
    }

    public Task DeleteAsync(string id)
        => _browserContext.SendMessageToServerAsync("credentialsDelete", new Dictionary<string, object?>
        {
            ["id"] = id,
        });

    public async Task<IReadOnlyList<VirtualCredential>> GetAsync(CredentialsGetOptions? options = default)
    {
        var result = await _browserContext.SendMessageToServerAsync("credentialsGet", new Dictionary<string, object?>
        {
            ["rpId"] = options?.RpId,
            ["id"] = options?.Id,
        }).ConfigureAwait(false);
        return result!.Value.GetProperty("credentials").ToObject<List<VirtualCredential>>(_browserContext._connection.DefaultJsonSerializerOptions);
    }
}
