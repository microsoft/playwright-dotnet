/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;

#nullable enable

namespace Microsoft.Playwright.Transport.Channels;

internal class CDPChannel : Channel<CDPSession>
{
    public CDPChannel(string guid, Connection connection, CDPSession owner) : base(guid, connection, owner)
    {
    }

    internal event EventHandler<CDPChannelEventArgs>? CDPEvent;

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        var eventParams = serverParams!.Value.Deserialize<CDPChannelEventArgs>();
        CDPEvent?.Invoke(this, new CDPChannelEventArgs { EventName = eventParams?.EventName, EventParams = eventParams?.EventParams });
    }

    internal Task DetachAsync()
    {
        return Connection.SendMessageToServerAsync(
           Guid,
           "detach");
    }

    internal async Task<JsonElement?> SendAsync(string method, Dictionary<string, object>? args = null)
    {
        var newArgs = new Dictionary<string, object>() { { "method", method } };
        if (args != null)
        {
            newArgs["params"] = args;
        }
        var result = await Connection.SendMessageToServerAsync(
           Guid,
           "send",
           newArgs).ConfigureAwait(false);
        return result?.GetProperty("result");
    }
}
#nullable disable
