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

namespace Microsoft.Playwright.Transport.Channels;

/// <summary>
/// An IChannelOwner has the ability to build data coming from a Playwright server and convert it into a Playwright class.
/// </summary>
internal interface IChannelOwner
{
    /// <summary>
    /// Connection.
    /// </summary>
    Connection Connection { get; }

    /// <summary>
    /// Channel.
    /// </summary>
    ChannelBase Channel { get; }

    /// <summary>
    /// Child objects.
    /// </summary>
    ConcurrentDictionary<string, IChannelOwner> Objects { get; }

    /// <summary>
    /// Removes the object from the parent and the connection list.
    /// </summary>
    void DisposeOwner();

    void Adopt(ChannelOwnerBase child);

    Task<T> WrapApiCallAsync<T>(Func<Task<T>> action, bool isInternal = false);

    Task WrapApiBoundaryAsync(Func<Task> action);
}

/// <summary>
/// An IChannelOwner has the ability to build data coming from a Playwright server and convert it into a Playwright class.
/// </summary>
/// <typeparam name="T">Channel Owner implementation.</typeparam>
internal interface IChannelOwner<T> : IChannelOwner
    where T : ChannelOwnerBase, IChannelOwner<T>
{
    /// <summary>
    /// Channel.
    /// </summary>
    new IChannel<T> Channel { get; }
}
