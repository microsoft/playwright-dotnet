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
using Microsoft.Playwright.Transport.Channels;

#nullable enable

namespace Microsoft.Playwright.Core;

internal class CDPSession : ChannelOwnerBase, ICDPSession, IChannelOwner<CDPSession>
{
    private readonly CDPChannel _channel;
    private readonly Dictionary<string, CDPSessionEvent> _cdpSessionEvents = new();

    public CDPSession(IChannelOwner parent, string guid) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
    }

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<CDPSession> IChannelOwner<CDPSession>.Channel => _channel;

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        switch (method)
        {
            case "message":
                OnCDPEvent(serverParams!.Value.GetProperty("method").ToString(), serverParams.Value.GetProperty("params"));
                break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task DetachAsync() => _channel.DetachAsync();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<JsonElement?> SendAsync(string method, Dictionary<string, object>? args = null)
        => _channel.SendAsync(method, args);

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
