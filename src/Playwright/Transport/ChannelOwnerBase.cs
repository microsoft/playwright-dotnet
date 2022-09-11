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
using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport;

internal class ChannelOwnerBase : IChannelOwner
{
    private readonly Connection _connection;
    private readonly ConcurrentDictionary<string, IChannelOwner> _objects = new();

    internal ChannelOwnerBase(IChannelOwner parent, string guid) : this(parent, null, guid)
    {
    }

    internal ChannelOwnerBase(IChannelOwner parent, Connection connection, string guid)
    {
        _connection = parent?.Connection ?? connection;

        Guid = guid;
        Parent = parent;

        _connection.Objects[guid] = this;
        if (Parent != null)
        {
            Parent.Objects[guid] = this;
        }
    }

    /// <inheritdoc/>
    Connection IChannelOwner.Connection => _connection;

    /// <inheritdoc/>
    ChannelBase IChannelOwner.Channel => null;

    /// <inheritdoc/>
    ConcurrentDictionary<string, IChannelOwner> IChannelOwner.Objects => _objects;

    internal string Guid { get; set; }

    internal IChannelOwner Parent { get; set; }

    void IChannelOwner.Adopt(ChannelOwnerBase child)
    {
        child.Parent.Objects.TryRemove(child.Guid, out _);
        _objects[child.Guid] = child;
        child.Parent = this;
    }

    /// <inheritdoc/>
    void IChannelOwner.DisposeOwner()
    {
        Parent?.Objects?.TryRemove(Guid, out var _);
        _connection?.Objects.TryRemove(Guid, out var _);

        foreach (var item in _objects.Values)
        {
            item.DisposeOwner();
        }
        _objects.Clear();
    }

    public Task<T> WrapApiCallAsync<T>(Func<Task<T>> action, bool isInternal = false) => _connection.WrapApiCallAsync(action, isInternal);

    public Task WrapApiCallAsync(Func<Task> action, bool isInternal = false) => _connection.WrapApiCallAsync(action, isInternal);

    public Task WrapApiBoundaryAsync(Func<Task> action) => _connection.WrapApiBoundaryAsync(action);
}
