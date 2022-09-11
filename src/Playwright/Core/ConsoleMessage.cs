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
using System.Linq;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class ConsoleMessage : ChannelOwnerBase, IChannelOwner<ConsoleMessage>, IConsoleMessage
{
    private readonly ConsoleMessageChannel _channel;
    private readonly ConsoleMessageInitializer _initializer;

    internal ConsoleMessage(IChannelOwner parent, string guid, ConsoleMessageInitializer initializer) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
        _initializer = initializer;
    }

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<ConsoleMessage> IChannelOwner<ConsoleMessage>.Channel => _channel;

    public string Type => _initializer.Type;

    public IReadOnlyList<IJSHandle> Args => _initializer.Args.Cast<IJSHandle>().ToList().AsReadOnly();

    public string Location => _initializer.Location.ToString();

    public string Text => _initializer.Text;
}
