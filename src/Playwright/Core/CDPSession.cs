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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

#nullable enable

namespace Microsoft.Playwright.Core;

internal class CDPSession : ChannelOwnerBase, ICDPSession, IChannelOwner<CDPSession>
{
    private readonly CDPChannel _channel;
    private readonly Dictionary<string, Action<JsonElement?>> _eventListeners;

    public CDPSession(IChannelOwner parent, string guid) : base(parent, guid)
    {
        _eventListeners = new();
        _channel = new(guid, parent.Connection, this);

        _channel.CDPEvent += OnCDPEvent;
    }

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<CDPSession> IChannelOwner<CDPSession>.Channel => _channel;

    public async Task DetachAsync()
    {
        await _channel.DetachAsync();
    }

    public async Task<JsonElement?> SendAsync(string method, Dictionary<string, object>? args = null)
    {
        return await _channel.SendAsync(method, args);
    }

    private void OnCDPEvent(object sender, CDPChannelEventArgs e)
    {
        if (_eventListeners.TryGetValue(e.EventName, out Action<JsonElement?> listener))
        {
            listener(e.EventParams);
        }
    }

    public void AddEventListener(string eventName, Action<JsonElement?> eventHandler)
    {
        if (_eventListeners.TryGetValue(eventName, out Action<JsonElement?> listener))
        {
            _eventListeners[eventName] += eventHandler;
        }
        else
        {
            _eventListeners.Add(eventName, eventHandler);
        }
    }

    public void RemoveEventListener(string eventName, Action<JsonElement?> eventHandler)
    {
        if (_eventListeners.TryGetValue(eventName, out Action<JsonElement?>? listener))
        {
            listener -= eventHandler;
            if (listener != null)
            {
                _eventListeners[eventName] = listener;
            }
            else
            {
                _eventListeners.Remove(eventName);
            }
        }
    }
}
#nullable disable
