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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport;

internal class ChannelOwner
{
    internal readonly Connection _connection;

    internal bool _wasCollected;

    internal ChannelOwner(ChannelOwner parent, string guid) : this(parent, null, guid)
    {
    }

    internal ChannelOwner(ChannelOwner parent, Connection connection, string guid)
    {
        _connection = parent?._connection ?? connection;

        Guid = guid;
        Parent = parent;

        _connection.Objects[guid] = this;
        if (Parent != null)
        {
            Parent.Objects[guid] = this;
        }
    }

    internal ConcurrentDictionary<string, ChannelOwner> Objects { get; } = new();

    internal string Guid { get; set; }

    internal ChannelOwner Parent { get; set; }

    internal virtual void OnMessage(string method, JsonElement? serverParams)
    {
    }

    internal void Adopt(ChannelOwner child)
    {
        child.Parent.Objects.TryRemove(child.Guid, out _);
        Objects[child.Guid] = child;
        child.Parent = this;
    }

    internal void DisposeOwner(string reason)
    {
        Parent?.Objects?.TryRemove(Guid, out var _);
        _connection?.Objects.TryRemove(Guid, out var _);
        _wasCollected = reason == "gc";

        foreach (var item in Objects.Values)
        {
            item.DisposeOwner(reason);
        }
        Objects.Clear();
    }

    public Task<T> WrapApiCallAsync<T>(Func<Task<T>> action, bool isInternal = false) => _connection.WrapApiCallAsync(action, isInternal);

    public Task WrapApiCallAsync(Func<Task> action, bool isInternal = false) => _connection.WrapApiCallAsync(action, isInternal);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task WrapApiBoundaryAsync(Func<Task> action) => _connection.WrapApiBoundaryAsync(action);

    internal EventHandler<T> UpdateEventHandler<T>(string eventName, EventHandler<T> handlers, EventHandler<T> handler, bool add)
    {
        if (add)
        {
            if ((handlers?.GetInvocationList().Length ?? 0) == 0)
            {
                UpdateEventSubscription(eventName, true);
            }
            handlers += handler;
        }
        else
        {
            handlers -= handler;
            if ((handlers?.GetInvocationList().Length ?? 0) == 0)
            {
                UpdateEventSubscription(eventName, false);
            }
        }
        // We need to return the new reference since += and -= operators return a new delegate.
        return handlers;
    }

    private void UpdateEventSubscription(string eventName, bool enabled)
    {
        WrapApiCallAsync(
            () => _connection.SendMessageToServerAsync(
            this,
            "updateSubscription",
            new Dictionary<string, object>
            {
                ["event"] = eventName,
                ["enabled"] = enabled,
            }),
            true).IgnoreException();
    }

    internal Task<JsonElement?> SendMessageToServerAsync(
        string method,
        Dictionary<string, object> args = null,
        bool keepNulls = false)
        => SendMessageToServerAsync<JsonElement?>(method, args, keepNulls);

    internal Task<T> SendMessageToServerAsync<T>(
        string method,
        Dictionary<string, object> args = null,
        bool keepNulls = false) => _connection.SendMessageToServerAsync<T>(this, method, args, keepNulls);
}
