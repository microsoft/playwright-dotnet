/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

#nullable enable

namespace Microsoft.Playwright;

/// <summary><para>The <see cref="IWebSocket"/> class represents websocket connections in the page.</para></summary>
public partial interface IWebSocket
{
    /// <summary><para>Fired when the websocket closes.</para></summary>
    event EventHandler<IWebSocket> Close;

    /// <summary><para>Fired when the websocket receives a frame.</para></summary>
    event EventHandler<IWebSocketFrame> FrameReceived;

    /// <summary><para>Fired when the websocket sends a frame.</para></summary>
    event EventHandler<IWebSocketFrame> FrameSent;

    /// <summary><para>Fired when the websocket has an error.</para></summary>
    event EventHandler<String> SocketError;

    /// <summary><para>Indicates that the web socket has been closed.</para></summary>
    bool IsClosed { get; }

    /// <summary><para>Contains the URL of the WebSocket.</para></summary>
    string Url { get; }
}

#nullable disable
