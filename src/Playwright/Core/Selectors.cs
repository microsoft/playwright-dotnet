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
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core;

internal class Selectors : ChannelOwnerBase, IChannelOwner<Selectors>
{
    internal readonly SelectorsChannel _channel;

    internal Selectors(IChannelOwner parent, string guid) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
    }

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<Selectors> IChannelOwner<Selectors>.Channel => _channel;
}

internal class SelectorsAPI : ISelectors
{
    private readonly HashSet<Selectors> _channels = new();
    private readonly List<SelectorsRegisterParams> _registrations = new();

    public async Task RegisterAsync(string name, SelectorsRegisterOptions options = default)
    {
        options ??= new SelectorsRegisterOptions();
        var source = ScriptsHelper.EvaluationScript(options.Script, options.Path);
        SelectorsRegisterParams @params = new()
        {
            Name = name,
            Source = source,
            ContentScript = options?.ContentScript,
        };
        foreach (var channel in _channels)
        {
            await channel._channel.RegisterAsync(@params).ConfigureAwait(false);
        }
        _registrations.Add(@params);
    }

    public void SetTestIdAttribute(string attributeName)
    {
        Locator.SetTestIdAttribute(attributeName);
        foreach (var channel in _channels)
        {
            channel._channel.SetTestIdAttributeAsync(attributeName).IgnoreException();
        }
    }

    internal void AddChannel(Selectors channel)
    {
        _channels.Add(channel);
        foreach (var @params in _registrations)
        {
            // This should not fail except for connection closure, but just in case we catch.
            channel._channel.RegisterAsync(@params).IgnoreException();
            channel._channel.SetTestIdAttributeAsync(Locator.TestIdAttributeName()).IgnoreException();
        }
    }

    internal void RemoveChannel(Selectors channel)
    {
        _channels.Remove(channel);
    }
}
