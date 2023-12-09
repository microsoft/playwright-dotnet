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
using System.Text.Json;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class WebSocket : ChannelOwnerBase, IChannelOwner<WebSocket>, IWebSocket
{
    private const int OpcodeBase64 = 2;
    private readonly WebSocketChannel _channel;
    private readonly WebSocketInitializer _initializer;

    internal WebSocket(IChannelOwner parent, string guid, WebSocketInitializer initializer) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
        _initializer = initializer;
    }

    public event EventHandler<IWebSocket> Close;

    public event EventHandler<IWebSocketFrame> FrameSent;

    public event EventHandler<IWebSocketFrame> FrameReceived;

    public event EventHandler<string> SocketError;

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<WebSocket> IChannelOwner<WebSocket>.Channel => _channel;

    public string Url => _initializer.Url;

    public bool IsClosed { get; internal set; }

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        bool IsTextOrBinaryFrame(out int opcode)
        {
            opcode = serverParams?.GetProperty("opcode").ToObject<int>() ?? 0;
            return opcode != 1 && opcode != 2;
        }

        int opcode;
        switch (method)
        {
            case "close":
                IsClosed = true;
                Close?.Invoke(this, this);
                break;
            case "frameSent":
                if (IsTextOrBinaryFrame(out opcode))
                {
                    break;
                }

                FrameSent?.Invoke(
                    this,
                    new WebSocketFrame(
                        serverParams?.GetProperty("data").ToObject<string>(),
                        opcode == OpcodeBase64));
                break;
            case "frameReceived":
                if (IsTextOrBinaryFrame(out opcode))
                {
                    break;
                }
                FrameReceived?.Invoke(
                    this,
                    new WebSocketFrame(
                        serverParams?.GetProperty("data").ToObject<string>(),
                        opcode == OpcodeBase64));
                break;
            case "socketError":
                SocketError?.Invoke(this, serverParams?.GetProperty("error").ToObject<string>());
                break;
        }
    }
}
