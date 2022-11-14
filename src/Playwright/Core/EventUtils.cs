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
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;

internal class EventUtils
{
    private Dictionary<string, string> _eventToSubscriptionMapping = new();
    private Dictionary<string, Delegate> _eventToDelegate = new();
    private Connection _connection;
    private string _guid;

    public EventUtils(Connection connection, string guid)
    {
        _connection = connection;
        _guid = guid;
    }

    internal void SetEventToSubscriptionMapping(Dictionary<string, string> mapping)
        => _eventToSubscriptionMapping = mapping;

    internal void UpdateEventSubscription(string eventName, bool enabled)
    {
        if (!_eventToSubscriptionMapping.TryGetValue(eventName, out var protocolName))
        {
            throw new InvalidOperationException($"Subscripting to event {eventName} is not supported");
        }
        _connection.SendMessageToServerAsync(
            _guid,
            "updateSubscription",
            new Dictionary<string, object>
            {
                ["event"] = protocolName,
                ["enabled"] = enabled,
            }).IgnoreException();
    }

    internal void OnEventHandlerAdd<T>(string eventName, EventHandler<T> handler)
    {
        if (EventListenerCount(eventName) == 0)
        {
            UpdateEventSubscription(eventName, true);
        }
        if (!_eventToDelegate.ContainsKey(eventName))
        {
            _eventToDelegate[eventName] = handler;
        }
        else
        {
            _eventToDelegate[eventName] = Delegate.Combine(_eventToDelegate[eventName], handler);
        }
    }

    internal void OnEventHandlerRemove<T>(string eventName, EventHandler<T> handler)
    {
        if (_eventToDelegate.TryGetValue(eventName, out var handlers))
        {
            handlers = Delegate.Remove(handlers, handler);
            if (EventListenerCount(eventName) == 0)
            {
                _eventToDelegate.Remove(eventName);
                UpdateEventSubscription(eventName, false);
            }
        }
    }

    internal void OnEventHandlerInvoke<T>(string eventName, T eventArgs)
    {
        if (_eventToDelegate.TryGetValue(eventName, out var handlers))
        {
            handlers.DynamicInvoke(this, eventArgs);
        }
    }

    private int EventListenerCount(string eventName)
    {
        if (_eventToDelegate.TryGetValue(eventName, out var handlers))
        {
            return handlers.GetInvocationList().Length;
        }
        return 0;
    }
}
