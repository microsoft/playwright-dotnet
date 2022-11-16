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

using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels;

internal class ArtifactChannel : Channel<Artifact>
{
    public ArtifactChannel(string guid, Connection connection, Artifact owner) : base(guid, connection, owner)
    {
    }

    internal async Task<string> PathAfterFinishedAsync()
        => (await Connection.SendMessageToServerAsync<JsonElement?>(
            Guid,
            "pathAfterFinished",
            null)
            .ConfigureAwait(false)).GetString("value", true);

    internal async Task SaveAsAsync(string path)
        => await Connection.SendMessageToServerAsync<JsonElement>(
            Guid,
            "saveAs",
            new
            {
                path = path,
            })
            .ConfigureAwait(false);

    internal async Task<Stream> SaveAsStreamAsync()
        => (await Connection.SendMessageToServerAsync<JsonElement>(
            Guid,
            "saveAsStream",
            null)
            .ConfigureAwait(false)).GetObject<Stream>("stream", Connection);

    internal async Task<string> FailureAsync()
        => (await Connection.SendMessageToServerAsync<JsonElement?>(
            Guid,
            "failure",
            null)
            .ConfigureAwait(false)).GetString("error", true);

    internal async Task<Stream> StreamAsync()
        => (await Connection.SendMessageToServerAsync<JsonElement?>(
            Guid,
            "stream",
            null)
            .ConfigureAwait(false))?.GetObject<Stream>("stream", Connection);

    internal async Task CancelAsync()
        => await Connection.SendMessageToServerAsync<JsonElement>(
            Guid,
            "cancel",
            null)
            .ConfigureAwait(false);

    internal async Task DeleteAsync()
        => await Connection.SendMessageToServerAsync<JsonElement>(
            Guid,
            "delete",
            null)
            .ConfigureAwait(false);
}
