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

using System.Threading.Tasks;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class ArtifactChannel : Channel<Artifact>
    {
        public ArtifactChannel(string guid, Connection connection, Artifact owner) : base(guid, connection, owner)
        {
        }

        internal async Task<string> PathAfterFinishedAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "pathAfterFinished", null).ConfigureAwait(false))?.GetProperty("value").ToString();

        internal Task SaveAsAsync(string path) => Connection.SendMessageToServerAsync(Guid, "saveAs", new { path });

        internal async Task<string> GetFailureAsync()
        {
            var result = await Connection.SendMessageToServerAsync(Guid, "failure").ConfigureAwait(false);
#pragma warning disable IDE0018 // Inline variable declaration will cause issues due to Roslyn design: https://github.com/dotnet/roslyn/issues/54711
            System.Text.Json.JsonElement failureValue = default;
#pragma warning restore IDE0018 // Inline variable declaration
            if (result?.TryGetProperty("error", out failureValue) ?? false)
            {
                return failureValue.GetString();
            }

            return null;
        }

        internal Task DeleteAsync() => Connection.SendMessageToServerAsync(Guid, "delete", null);

        internal Task<ArtifactStreamResult> GetStreamAsync() => Connection.SendMessageToServerAsync<ArtifactStreamResult>(Guid, "stream", null);

        internal Task CancelAsync() => Connection.SendMessageToServerAsync(Guid, "cancel", null);
    }
}
