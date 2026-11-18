/**
 * Copyright 2018 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/web-socket.spec.ts.</summary>
public class WebSocketTests : UpstreamPageTest
{
    [UpstreamTest("WS-001 should work @smoke")]
    public async Task ShouldWork()
    {
        Server.SendOnWebSocketConnection("incoming");
        var value = await Page.EvaluateAsync<string>(@"host => {
    let cb;
    const result = new Promise(f => cb = f);
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('message', data => { ws.close(); cb(data.data); });
    return result;
  }", Server.Host);
        Assert.That(value, Is.EqualTo("incoming"));
    }

    [UpstreamTest("WS-002 should emit close events")]
    public async Task ShouldEmitCloseEvents()
    {
        var socketClosePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var log = new List<string>();
        IWebSocket webSocket = null;
        Page.WebSocket += (_, ws) =>
        {
            log.Add($"open<{ws.Url}>");
            webSocket = ws;
            ws.Close += (_, _) => { log.Add("close"); socketClosePromise.SetResult(); };
        };
        await Page.EvaluateAsync(@"host => {
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('open', () => ws.close());
  }", Server.Host);
        await socketClosePromise.Task;
        Assert.That(string.Join(":", log), Is.EqualTo($"open<ws://{Server.Host}/ws>:close"));
        Assert.That(webSocket.IsClosed, Is.True);
    }

    [UpstreamTest("WS-003 should emit frame events")]
    public async Task ShouldEmitFrameEvents()
    {
        Server.OnceWebSocketConnection(ws =>
        {
            ws.OnMessage(_ => ws.Send("incoming"));
        });
        var socketClosePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var log = new List<string>();
        Page.WebSocket += (_, ws) =>
        {
            log.Add("open");
            ws.FrameSent += (_, d) => log.Add("sent<" + d.Text + ">");
            ws.FrameReceived += (_, d) => log.Add("received<" + d.Text + ">");
            ws.Close += (_, _) => { log.Add("close"); socketClosePromise.SetResult(); };
        };
        await Page.EvaluateAsync(@"host => {
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('open', () => ws.send('outgoing'));
    ws.addEventListener('message', () => { ws.close(); });
    window.ws = ws;
  }", Server.Host);
        await socketClosePromise.Task;
        Assert.That(log, Is.EqualTo(new[] { "open", "sent<outgoing>", "received<incoming>", "close" }));
    }

    [UpstreamTest("WS-004 should filter out the close events when the server closes with a message")]
    public async Task ShouldFilterOutTheCloseEventsWhenTheServerClosesWithAMessage()
    {
        Server.OnceWebSocketConnection(ws =>
        {
            ws.Send("incoming");
            ws.OnMessage(_ => ws.Close(1003, "closed by Playwright test-server"));
        });
        var socketClosePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var log = new List<string>();
        Page.WebSocket += (_, ws) =>
        {
            log.Add("open");
            ws.FrameSent += (_, d) => log.Add("sent<" + d.Text + ">");
            ws.FrameReceived += (_, d) => log.Add("received<" + d.Text + ">");
            ws.Close += (_, _) => { log.Add("close"); socketClosePromise.SetResult(); };
        };
        await Page.EvaluateAsync(@"host => {
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('message', () => ws.send('outgoing'));
    window.ws = ws;
  }", Server.Host);
        await socketClosePromise.Task;
        Assert.That(log, Is.EqualTo(new[] { "open", "received<incoming>", "sent<outgoing>", "close" }));
    }

    [UpstreamTest("WS-005 should pass self as argument to close event")]
    public async Task ShouldPassSelfAsArgumentToCloseEvent()
    {
        var socketClosePromise = new TaskCompletionSource<IWebSocket>(TaskCreationOptions.RunContinuationsAsynchronously);
        IWebSocket webSocket = null;
        Page.WebSocket += (_, ws) =>
        {
            webSocket = ws;
            ws.Close += (_, arg) => socketClosePromise.SetResult(arg);
        };
        await Page.EvaluateAsync(@"host => {
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('open', () => ws.close());
  }", Server.Host);
        var eventArg = await socketClosePromise.Task;
        Assert.That(eventArg, Is.SameAs(webSocket));
    }

    [UpstreamTest("WS-006 should emit binary frame events")]
    public async Task ShouldEmitBinaryFrameEvents()
    {
        var donePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: payload is the Text of a text frame and the Binary of a binary frame.
        var sent = new List<IWebSocketFrame>();
        Page.WebSocket += (_, ws) =>
        {
            ws.Close += (_, _) => donePromise.SetResult();
            ws.FrameSent += (_, d) => sent.Add(d);
        };
        await Page.EvaluateAsync(@"host => {
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('open', () => {
      const binary = new Uint8Array(5);
      for (let i = 0; i < 5; ++i)
        binary[i] = i;
      ws.send('text');
      ws.send(binary);
      ws.close();
    });
  }", Server.Host);
        await donePromise.Task;
        Assert.That(sent[0].Text, Is.EqualTo("text"));
        for (var i = 0; i < 5; ++i)
        {
            Assert.That(sent[1].Binary[i], Is.EqualTo(i));
        }
    }

    [UpstreamTest("WS-007 should emit error")]
    public async Task ShouldEmitError()
    {
        var result = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: the second call of the callback of a promise does nothing: TrySetResult.
        Page.WebSocket += (_, ws) => ws.SocketError += (_, e) => result.TrySetResult(e);
        await Page.EvaluateAsync(@"host => {
    new WebSocket('ws://' + host + '/bogus-ws');
  }", Server.Host);
        var message = await result.Task;
        Assert.That(message, Does.Contain(Channel?.Contains("msedge") == true ? "" : ": 400"));
    }

    [UpstreamTest("WS-011 should not tear down the page when a WebSocket is opened inside a worker")]
    public async Task ShouldNotTearDownThePageWhenAWebSocketIsOpenedInsideAWorker()
    {
        Server.SendOnWebSocketConnection("incoming");
        await Page.GotoAsync(Server.EmptyPage);
        var received = await Page.EvaluateAsync<string>(@"host => {
    const code = `
      const ws = new WebSocket(${JSON.stringify('ws://' + host + '/ws')});
      ws.addEventListener('message', event => self.postMessage(event.data));
    `;
    const worker = new Worker(URL.createObjectURL(new Blob([code], { type: 'text/javascript' })));
    return new Promise(resolve => worker.addEventListener('message', event => resolve(event.data)));
  }", Server.Host);
        Assert.That(received, Is.EqualTo("incoming"));
        // Opening a `WebSocket` inside a worker must not tear down the page session.
        Assert.That(await Page.EvaluateAsync<int>("() => 1 + 1"), Is.EqualTo(2));
    }

    [UpstreamTest("WS-013 should send extra HTTP headers on WebSocket handshake")]
    public async Task ShouldSendExtraHTTPHeadersOnWebSocketHandshake()
    {
        Info.Fixme(BrowserName == "chromium" && BrowserMajorVersion < 151, "Chromium before 151 does not send extra HTTP headers on WebSocket handshake");

        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["foo"] = "bar" });
        await Page.GotoAsync(Server.EmptyPage);
        var reqPromise = Server.WaitForWebSocketConnectionRequest();
        await Page.EvaluateAsync("host => { new WebSocket(`ws://${host}/ws`); }", Server.Host);
        var req = await reqPromise;
        Assert.That(req.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("WS-014 should send extra HTTP headers on WebSocket handshake from a worker")]
    public async Task ShouldSendExtraHTTPHeadersOnWebSocketHandshakeFromAWorker()
    {
        Info.Fixme(BrowserName == "chromium" && BrowserMajorVersion < 151, "Chromium before 151 does not send extra HTTP headers on WebSocket handshake");

        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["foo"] = "bar" });
        await Page.GotoAsync(Server.EmptyPage);
        var reqPromise = Server.WaitForWebSocketConnectionRequest();
        await Page.EvaluateAsync(@"host => {
    const code = `new WebSocket(${JSON.stringify('ws://' + host + '/ws')});`;
    new Worker(URL.createObjectURL(new Blob([code], { type: 'text/javascript' })));
  }", Server.Host);
        var req = await reqPromise;
        Assert.That(req.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("WS-041 should navigate while an iframe opens WebSockets")]
    public async Task ShouldNavigateWhileAnIframeOpensWebSockets()
    {
        Server.SetRoute("/parent", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<iframe src=\"/child\"></iframe>");
        });
        Server.SetRoute("/child", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<script>setInterval(() => new WebSocket('ws://' + location.host + '/ws'), 5)</script>");
        });
        for (var i = 0; i < 100; ++i)
        {
            await Page.GotoAsync(Server.Prefix + "/parent");
        }
        Assert.That(Page.Frames, Has.Count.EqualTo(2));
        Assert.That(await Page.EvaluateAsync<int>("() => 1 + 1"), Is.EqualTo(2));
    }
}
