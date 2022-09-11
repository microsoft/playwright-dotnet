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
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels;

internal class WebSocketChannel : Channel<WebSocket>
{
    private const int OpcodeBase64 = 2;

    public WebSocketChannel(string guid, Connection connection, WebSocket owner) : base(guid, connection, owner)
    {
    }

    internal event EventHandler Close;

    internal event EventHandler<IWebSocketFrame> FrameSent;

    internal event EventHandler<IWebSocketFrame> FrameReceived;

    internal event EventHandler<string> SocketError;

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
                Close?.Invoke(this, EventArgs.Empty);
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
