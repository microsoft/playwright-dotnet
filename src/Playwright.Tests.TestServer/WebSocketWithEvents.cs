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
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests.TestServer;

public class WebSocketWithEvents : IDisposable
{
    public readonly WebSocket ws;
    public readonly HttpRequest request;
    private readonly CancellationTokenSource _cts = new();

    public WebSocketWithEvents(WebSocket ws, HttpRequest request)
    {
        this.ws = ws;
        this.request = request;
    }

    public event EventHandler<string> MessageReceived;
    public event EventHandler<(WebSocketCloseStatus? Code, string Reason)> Closed;

    internal async Task RunReceiveLoop()
    {
        var buffer = new byte[1024 * 4];
        try
        {
            while (ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(this, message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CloseAsync().ConfigureAwait(false);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation, do nothing
        }
        finally
        {
            if (ws.State == WebSocketState.CloseReceived)
            {
                Closed?.Invoke(this, (ws.CloseStatus, ws.CloseStatusDescription));
            }
            await CloseAsync().ConfigureAwait(false);
        }
    }

    public async Task SendAsync(string message)
    {
        if (ws.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not open.");

        var buffer = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token).ConfigureAwait(false);
    }

    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "Closing")
    {
        if (ws.State == WebSocketState.Open)
        {
            try
            {
                await ws.CloseAsync(closeStatus, statusDescription, CancellationToken.None).ConfigureAwait(false);
            }
            catch (WebSocketException)
            {
                // The WebSocket might have been closed by the server, ignore the exception
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        ws.Dispose();
    }
}
