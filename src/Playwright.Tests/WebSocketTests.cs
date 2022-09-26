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

namespace Microsoft.Playwright.Tests;

///<playwright-file>web-socket.spec.ts</playwright-file>
public class WebSocketTests : PageTestEx
{
    [PlaywrightTest("web-socket.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        Server.SendOnWebSocketConnection("incoming");
        string value = await Page.EvaluateAsync<string>(@"port => {
                let cb;
                const result = new Promise(f => cb = f);
                const ws = new WebSocket('ws://localhost:' + port + '/ws');
                ws.addEventListener('message', data => { ws.close(); cb(data.data); });
                return result;
            }", Server.Port);
        Assert.AreEqual("incoming", value);
    }

    [PlaywrightTest("web-socket.spec.ts", "should emit close events")]
    public async Task ShouldEmitCloseEvents()
    {
        var socketClosedTcs = new TaskCompletionSource<bool>();
        var log = new List<string>();
        IWebSocket webSocket = null;

        Page.WebSocket += (_, e) =>
        {
            log.Add($"open<{e.Url}>");
            webSocket = e;
            webSocket.Close += (_, _) =>
            {
                log.Add("close");
                socketClosedTcs.TrySetResult(true);
            };
        };

        await Page.EvaluateAsync(@"port => {
                const ws = new WebSocket('ws://localhost:' + port + '/ws');
                ws.addEventListener('open', () => ws.close());
            }", Server.Port);

        await socketClosedTcs.Task;
        Assert.AreEqual($"open<ws://localhost:{Server.Port}/ws>:close", string.Join(":", log));
        Assert.True(webSocket.IsClosed);
    }

    [PlaywrightTest("web-socket.spec.ts", "should emit frame events")]
    public async Task ShouldEmitFrameEvents()
    {
        Server.SendOnWebSocketConnection("incoming");
        var socketClosedTcs = new TaskCompletionSource<bool>();
        var log = new List<string>();

        Page.WebSocket += (_, e) =>
        {
            log.Add($"open");

            e.FrameSent += (_, e) => log.Add($"sent<{e.Text}>");
            e.FrameReceived += (_, e) => log.Add($"received<{e.Text}>");

            e.Close += (_, _) =>
            {
                log.Add("close");
                socketClosedTcs.TrySetResult(true);
            };
        };

        await Page.EvaluateAsync(@"port => {
                const ws = new WebSocket('ws://127.0.0.1:' + port + '/ws');
                ws.addEventListener('open', () => { ws.send('outgoing'); });
                ws.addEventListener('message', e => { ws.close() });
            }", Server.Port);

        await socketClosedTcs.Task;
        Assert.AreEqual("open", log[0]);
        Assert.AreEqual("close", log[3]);
        log.Sort();
        Assert.AreEqual("close:open:received<incoming>:sent<outgoing>", string.Join(":", log));
    }

    [PlaywrightTest("web-socket.spec.ts", "should emit binary frame events")]
    public async Task ShouldEmitBinaryFrameEvents()
    {
        var socketClosedTcs = new TaskCompletionSource<bool>();
        var log = new List<IWebSocketFrame>();

        Page.WebSocket += (_, e) =>
        {
            e.FrameSent += (_, e) => log.Add(e);
            e.Close += (_, _) => socketClosedTcs.TrySetResult(true);
        };

        await Page.EvaluateAsync(@"port => {
                const ws = new WebSocket('ws://localhost:' + port + '/ws');
                ws.addEventListener('open', () => {
                    const binary = new Uint8Array(5);
                    for (let i = 0; i < 5; ++i)
                        binary[i] = i;
                    ws.send('text');
                    ws.send(binary);
                    ws.close();
                });
            }", Server.Port);


        await socketClosedTcs.Task;
        Assert.AreEqual("text", log[0].Text);

        for (int i = 0; i < 5; i++)
        {
            Assert.AreEqual(i, log[1].Binary[i]);
        }
    }

    [PlaywrightTest("web-socket.spec.ts", "should emit error")]
    public async Task ShouldEmitError()
    {
        var socketErrorTcs = new TaskCompletionSource<string>();

        Page.WebSocket += (_, e) =>
        {
            e.SocketError += (_, e) => socketErrorTcs.TrySetResult(e);
        };

        await Page.EvaluateAsync(@"port => {
                new WebSocket('ws://localhost:' + port + '/bogus-ws');
            }", Server.Port);


        await socketErrorTcs.Task;

        if (TestConstants.IsFirefox)
        {
            Assert.AreEqual("CLOSE_ABNORMAL", socketErrorTcs.Task.Result);
        }
        else
        {
            StringAssert.Contains(": 40", socketErrorTcs.Task.Result);
        }
    }

    [PlaywrightTest("web-socket.spec.ts", "should not have stray error events")]
    public async Task ShouldNotHaveStrayErrorEvents()
    {
        Server.SendOnWebSocketConnection("incoming");
        var frameReceivedTcs = new TaskCompletionSource<bool>();
        string socketError = null;
        IWebSocket ws = null;

        Page.WebSocket += (_, e) =>
        {
            ws = e;
            e.SocketError += (_, e) => socketError = e;
            e.FrameReceived += (_, _) => frameReceivedTcs.TrySetResult(true);
        };

        await TaskUtils.WhenAll(
            frameReceivedTcs.Task,
            Page.EvaluateAsync(@"port => {
                    window.ws = new WebSocket('ws://localhost:' + port + '/ws');
                }", Server.Port));

        await Page.EvaluateAsync("window.ws.close();");
        Assert.Null(socketError);
    }
}
