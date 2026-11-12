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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport;

internal class ChannelOwner
{
    internal readonly Connection _connection;

    internal bool _wasCollected;

    // The events that the server was told to send. Dispatcher thread only.
    private readonly HashSet<string> _eventSubscriptions = new();

    internal ChannelOwner(ChannelOwner parent, string guid) : this(parent, parent._connection, guid)
    {
    }

    internal ChannelOwner(ChannelOwner? parent, Connection connection, string guid)
    {
        _connection = connection;

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

    internal ChannelOwner? Parent { get; set; }

    internal virtual void OnMessage(string method, JsonElement serverParams)
    {
    }

    internal void Adopt(ChannelOwner child)
    {
        child.Parent!.Objects.TryRemove(child.Guid, out _);
        Objects[child.Guid] = child;
        child.Parent = this;
    }

    internal void DisposeOwner(string? reason)
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

    public Task<T> WrapApiCallAsync<T>(Func<Task<T>> action, bool isInternal = false, string? title = null) => _connection.WrapApiCallAsync(action, isInternal, title);

    public Task WrapApiCallAsync(Func<Task> action, bool isInternal = false, string? title = null) => _connection.WrapApiCallAsync(action, isInternal, title);

    /// <summary>
    /// The add and remove accessors of an event that the server sends only while somebody listens.
    /// <para>
    /// The change takes effect at once, on the caller's thread, as with any C# event: the list
    /// of handlers is replaced atomically, so accessors on other threads and the dispatcher
    /// thread that raises the event never see a half-made update. Once a remove has returned,
    /// the handler is not called for any event that is raised later; an event that is being
    /// raised on the dispatcher thread at that moment may still reach it.
    /// </para>
    /// <para>
    /// Telling the server is left to the dispatcher thread. It compares what the server was told
    /// last with whether there are handlers when it runs, and sends the difference. So the
    /// messages cannot go out in the wrong order or describe a state that is gone, and a call
    /// that changes nothing sends nothing.
    /// </para>
    /// </summary>
    internal void UpdateEventHandler<T>(string eventName, ref EventHandler<T>? handlers, EventHandler<T>? handler, bool add, Func<bool> hasHandlers)
    {
        EventHandler<T>? seen;
        var current = Volatile.Read(ref handlers);
        do
        {
            seen = current;
            current = Interlocked.CompareExchange(ref handlers, add ? seen + handler : seen - handler, seen);
        }
        while (!ReferenceEquals(current, seen));

        _connection.Dispatcher.RunOrPost(() =>
        {
            bool enabled = hasHandlers();
            if (_connection.IsClosed || _eventSubscriptions.Contains(eventName) == enabled)
            {
                return;
            }
            if (enabled)
            {
                _eventSubscriptions.Add(eventName);
            }
            else
            {
                _eventSubscriptions.Remove(eventName);
            }
            WrapApiCallAsync(
                () => _connection.SendMessageToServerAsync(
                this,
                "updateSubscription",
                new Dictionary<string, object?>
                {
                    ["event"] = eventName,
                    ["enabled"] = enabled,
                }),
                true).IgnoreException();
        });
    }

    internal Task<JsonElement?> SendMessageToServerAsync(
        string method,
        Dictionary<string, object?>? args = null,
        bool keepNulls = false,
        float? timeout = null)
        => SendMessageToServerAsync<JsonElement?>(method, args, keepNulls, timeout);

    internal Task<T> SendMessageToServerAsync<T>(
        string method,
        Dictionary<string, object?>? args = null,
        bool keepNulls = false,
        float? timeout = null) => _connection.SendMessageToServerAsync<T>(this, method, args, keepNulls, timeout);
}
