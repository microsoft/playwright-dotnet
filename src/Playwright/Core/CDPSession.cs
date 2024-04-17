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
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;

#nullable enable

namespace Microsoft.Playwright.Core;

internal class CDPSession : ChannelOwner, ICDPSession
{
    private readonly Dictionary<string, CDPSessionEvent> _cdpSessionEvents = new();

    public CDPSession(ChannelOwner parent, string guid) : base(parent, guid)
    {
    }

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        switch (method)
        {
            case "event":
                serverParams!.Value.TryGetProperty("params", out var cdpParams);
                OnCDPEvent(serverParams!.Value.GetProperty("method").ToString(), cdpParams);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task DetachAsync() => SendMessageToServerAsync("detach");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<JsonElement?> SendAsync(string method, Dictionary<string, object>? args = null)
    {
        var newArgs = new Dictionary<string, object>() { { "method", method } };
        if (args != null)
        {
            newArgs["params"] = args;
        }
        var result = await SendMessageToServerAsync("send", newArgs).ConfigureAwait(false);
        return result?.GetProperty("result");
    }

    private void OnCDPEvent(string name, JsonElement? @params)
    {
        if (_cdpSessionEvents.TryGetValue(name, out var cdpNamedEvent))
        {
            cdpNamedEvent.RaiseEvent(@params);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ICDPSessionEvent Event(string eventName)
    {
        if (_cdpSessionEvents.TryGetValue(eventName, out var cdpNamedEvent))
        {
            return cdpNamedEvent;
        }
        else
        {
            cdpNamedEvent = new(eventName);
            _cdpSessionEvents.Add(eventName, cdpNamedEvent);
            return cdpNamedEvent;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask DisposeAsync()
    {
        await DetachAsync().ConfigureAwait(false);
    }
}
