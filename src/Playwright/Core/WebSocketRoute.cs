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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

/// <summary>
/// <see cref="IWebSocketRoute"/>.
/// </summary>
internal class WebSocketRoute : ChannelOwner, IWebSocketRoute
{
    private readonly IWebSocketRoute _server;
    internal readonly WebSocketRouteInitializer _initializer;
    private bool _connected;
    private Action<int?, string?>? _onPageClose;
    private Action<IWebSocketFrame>? _onPageMessage;
    internal Action<int?, string?>? _onServerClose;
    internal Action<IWebSocketFrame>? _onServerMessage;

    internal WebSocketRoute(ChannelOwner parent, string guid, WebSocketRouteInitializer initializer) : base(parent, guid)
    {
        _initializer = initializer;
        _server = new ServerWebSocketRoute(this);
    }

    public string Url => _initializer.Url;

    internal override void OnMessage(string method, JsonElement serverParams)
    {
        switch (method)
        {
            case "messageFromPage":
                if (_onPageMessage != null)
                {
                    var frame = new WebSocketFrame(serverParams.GetProperty("message").GetString()!, serverParams.GetProperty("isBase64").GetBoolean());
                    _onPageMessage(frame);
                }
                else if (_connected)
                {
                    SendMessageToServerAsync("sendToServer", new Dictionary<string, object?>
                    {
                        ["message"] = serverParams.GetProperty("message").GetString(),
                        ["isBase64"] = serverParams.GetProperty("isBase64").GetBoolean(),
                    }).IgnoreException();
                }
                break;
            case "messageFromServer":
                if (_onServerMessage != null)
                {
                    var frame = new WebSocketFrame(serverParams.GetProperty("message").GetString()!, serverParams.GetProperty("isBase64").GetBoolean());
                    _onServerMessage(frame);
                }
                else
                {
                    SendMessageToServerAsync("sendToPage", new Dictionary<string, object?>
                    {
                        ["message"] = serverParams.GetProperty("message").GetString(),
                        ["isBase64"] = serverParams.GetProperty("isBase64").GetBoolean(),
                    }).IgnoreException();
                }
                break;
            case "closePage":
                if (_onPageClose != null)
                {
                    _onPageClose(serverParams.GetProperty("code").GetInt32(), serverParams.GetProperty("reason").GetString());
                }
                else
                {
                    SendMessageToServerAsync("closeServer", new Dictionary<string, object?>
                    {
                        ["code"] = serverParams.GetProperty("code").GetInt32(),
                        ["reason"] = serverParams.GetProperty("reason").GetString(),
                        ["wasClean"] = serverParams.GetProperty("wasClean").GetBoolean(),
                    }).IgnoreException();
                }
                break;
            case "closeServer":
                if (_onServerClose != null)
                {
                    _onServerClose(serverParams.GetProperty("code").GetInt32(), serverParams.GetProperty("reason").GetString());
                }
                else
                {
                    SendMessageToServerAsync("closePage", new Dictionary<string, object?>
                    {
                        ["code"] = serverParams.GetProperty("code").GetInt32(),
                        ["reason"] = serverParams.GetProperty("reason").GetString(),
                        ["wasClean"] = serverParams.GetProperty("wasClean").GetBoolean(),
                    }).IgnoreException();
                }
                break;
        }
    }

    public async Task CloseAsync(WebSocketRouteCloseOptions? options = null)
    {
        try
        {
            await SendMessageToServerAsync("closePage", new Dictionary<string, object?>
            {
                ["code"] = options?.Code,
                ["reason"] = options?.Reason,
                ["wasClean"] = true,
            }).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignore the exception
        }
    }

    public void Send(byte[] message)
    {
        SendMessageToServerAsync("sendToPage", new Dictionary<string, object?>
        {
            ["message"] = Convert.ToBase64String(message),
            ["isBase64"] = true,
        }).IgnoreException();
    }

    public void Send(string message)
    {
        SendMessageToServerAsync("sendToPage", new Dictionary<string, object?>
        {
            ["message"] = message,
            ["isBase64"] = false,
        }).IgnoreException();
    }

    public IWebSocketRoute ConnectToServer()
    {
        if (_connected)
        {
            throw new PlaywrightException("Already connected to the server");
        }
        _connected = true;
        SendMessageToServerAsync("connect").IgnoreException();
        return _server;
    }

    public void OnMessage(Action<IWebSocketFrame> handler) => _onPageMessage = handler;

    public void OnClose(Action<int?, string?> handler) => _onPageClose = handler;

    internal async Task AfterHandleAsync()
    {
        if (_connected)
        {
            return;
        }
        // Ensure that websocket is "open" and can send messages without an actual server connection.
        await SendMessageToServerAsync("ensureOpened").ConfigureAwait(false);
    }
}

internal class ServerWebSocketRoute : IWebSocketRoute
{
    private readonly WebSocketRoute _webSocketRoute;

    internal ServerWebSocketRoute(WebSocketRoute route)
    {
        _webSocketRoute = route;
    }

    public string Url => _webSocketRoute._initializer.Url;

    public async Task CloseAsync(WebSocketRouteCloseOptions? options = null)
    {
        try
        {
            await _webSocketRoute.SendMessageToServerAsync("closeServer", new Dictionary<string, object?>
            {
                ["code"] = options?.Code,
                ["reason"] = options?.Reason,
                ["wasClean"] = true,
            }).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignore the exception
        }
    }

    public IWebSocketRoute ConnectToServer() => throw new PlaywrightException("ConnectToServer() must be called on the page-side WebSocketRoute");

    public void OnClose(Action<int?, string?> handler) => _webSocketRoute._onServerClose = handler;

    public void OnMessage(Action<IWebSocketFrame> handler) => _webSocketRoute._onServerMessage = handler;

    public void Send(byte[] message)
    {
        _webSocketRoute.SendMessageToServerAsync("sendToServer", new Dictionary<string, object?>
        {
            ["message"] = Convert.ToBase64String(message),
            ["isBase64"] = true,
        }).IgnoreException();
    }

    public void Send(string message)
    {
        _webSocketRoute.SendMessageToServerAsync("sendToServer", new Dictionary<string, object?>
        {
            ["message"] = message,
            ["isBase64"] = false,
        }).IgnoreException();
    }
}
