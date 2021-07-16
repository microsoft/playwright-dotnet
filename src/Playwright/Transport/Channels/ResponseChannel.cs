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
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class ResponseChannel : Channel<Response>
    {
        public ResponseChannel(string guid, Connection connection, Response owner) : base(guid, connection, owner)
        {
        }

        internal async Task<string> GetBodyAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "body", null).ConfigureAwait(false))?.GetProperty("binary").ToString();

        internal async Task<string> FinishedAsync()
        {
            var element = await Connection.SendMessageToServerAsync(Guid, "finished", null).ConfigureAwait(false);
            if (element != null)
            {
                if (element.Value.TryGetProperty("error", out var errorValue))
                {
                    return errorValue.GetString();
                }
            }

            return null;
        }

        internal async Task<ResponseServerAddrResult> ServerAddrAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "serverAddr", null).ConfigureAwait(false))
                ?.GetProperty("value").ToObject<ResponseServerAddrResult>(Connection.GetDefaultJsonSerializerOptions());

        internal async Task<ResponseSecurityDetailsResult> SecurityDetailsAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "securityDetails", null).ConfigureAwait(false))
                ?.GetProperty("value").ToObject<ResponseSecurityDetailsResult>(Connection.GetDefaultJsonSerializerOptions());
    }
}
