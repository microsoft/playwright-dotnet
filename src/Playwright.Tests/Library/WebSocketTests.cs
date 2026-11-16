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
public class WebSocketTests
{
    [UpstreamTest("WS-001 should work @smoke")]
    public async Task ShouldWork(IPage page, Server server)
    {
        server.SendOnWebSocketConnection("incoming");
        var value = await page.EvaluateAsync<string>(@"host => {
    let cb;
    const result = new Promise(f => cb = f);
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('message', data => { ws.close(); cb(data.data); });
    return result;
  }", server.Host);
        Assert.That(value, Is.EqualTo("incoming"));
    }

    [UpstreamTest("WS-002 should emit close events")]
    public async Task ShouldEmitCloseEvents(IPage page, Server server)
    {
        var socketClosePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var log = new List<string>();
        IWebSocket webSocket = null;
        page.WebSocket += (_, ws) =>
        {
            log.Add($"open<{ws.Url}>");
            webSocket = ws;
            ws.Close += (_, _) => { log.Add("close"); socketClosePromise.SetResult(); };
        };
        await page.EvaluateAsync(@"host => {
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('open', () => ws.close());
  }", server.Host);
        await socketClosePromise.Task;
        Assert.That(string.Join(":", log), Is.EqualTo($"open<ws://{server.Host}/ws>:close"));
        Assert.That(webSocket.IsClosed, Is.True);
    }

    [UpstreamTest("WS-003 should emit frame events")]
    public async Task ShouldEmitFrameEvents(IPage page, Server server)
    {
        server.OnceWebSocketConnection(ws =>
        {
            ws.OnMessage(_ => ws.Send("incoming"));
        });
        var socketClosePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var log = new List<string>();
        page.WebSocket += (_, ws) =>
        {
            log.Add("open");
            ws.FrameSent += (_, d) => log.Add("sent<" + d.Text + ">");
            ws.FrameReceived += (_, d) => log.Add("received<" + d.Text + ">");
            ws.Close += (_, _) => { log.Add("close"); socketClosePromise.SetResult(); };
        };
        await page.EvaluateAsync(@"host => {
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('open', () => ws.send('outgoing'));
    ws.addEventListener('message', () => { ws.close(); });
    window.ws = ws;
  }", server.Host);
        await socketClosePromise.Task;
        Assert.That(log, Is.EqualTo(new[] { "open", "sent<outgoing>", "received<incoming>", "close" }));
    }

    [UpstreamTest("WS-004 should filter out the close events when the server closes with a message")]
    public async Task ShouldFilterOutTheCloseEventsWhenTheServerClosesWithAMessage(IPage page, Server server)
    {
        server.OnceWebSocketConnection(ws =>
        {
            ws.Send("incoming");
            ws.OnMessage(_ => ws.Close(1003, "closed by Playwright test-server"));
        });
        var socketClosePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var log = new List<string>();
        page.WebSocket += (_, ws) =>
        {
            log.Add("open");
            ws.FrameSent += (_, d) => log.Add("sent<" + d.Text + ">");
            ws.FrameReceived += (_, d) => log.Add("received<" + d.Text + ">");
            ws.Close += (_, _) => { log.Add("close"); socketClosePromise.SetResult(); };
        };
        await page.EvaluateAsync(@"host => {
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('message', () => ws.send('outgoing'));
    window.ws = ws;
  }", server.Host);
        await socketClosePromise.Task;
        Assert.That(log, Is.EqualTo(new[] { "open", "received<incoming>", "sent<outgoing>", "close" }));
    }

    [UpstreamTest("WS-005 should pass self as argument to close event")]
    public async Task ShouldPassSelfAsArgumentToCloseEvent(IPage page, Server server)
    {
        var socketClosePromise = new TaskCompletionSource<IWebSocket>(TaskCreationOptions.RunContinuationsAsynchronously);
        IWebSocket webSocket = null;
        page.WebSocket += (_, ws) =>
        {
            webSocket = ws;
            ws.Close += (_, arg) => socketClosePromise.SetResult(arg);
        };
        await page.EvaluateAsync(@"host => {
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('open', () => ws.close());
  }", server.Host);
        var eventArg = await socketClosePromise.Task;
        Assert.That(eventArg, Is.SameAs(webSocket));
    }

    [UpstreamTest("WS-006 should emit binary frame events")]
    public async Task ShouldEmitBinaryFrameEvents(IPage page, Server server)
    {
        var donePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: payload is the Text of a text frame and the Binary of a binary frame.
        var sent = new List<IWebSocketFrame>();
        page.WebSocket += (_, ws) =>
        {
            ws.Close += (_, _) => donePromise.SetResult();
            ws.FrameSent += (_, d) => sent.Add(d);
        };
        await page.EvaluateAsync(@"host => {
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('open', () => {
      const binary = new Uint8Array(5);
      for (let i = 0; i < 5; ++i)
        binary[i] = i;
      ws.send('text');
      ws.send(binary);
      ws.close();
    });
  }", server.Host);
        await donePromise.Task;
        Assert.That(sent[0].Text, Is.EqualTo("text"));
        for (var i = 0; i < 5; ++i)
        {
            Assert.That(sent[1].Binary[i], Is.EqualTo(i));
        }
    }

    [UpstreamTest("WS-007 should emit error")]
    public async Task ShouldEmitError(IPage page, Server server, string channel)
    {
        var result = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: the second call of the callback of a promise does nothing: TrySetResult.
        page.WebSocket += (_, ws) => ws.SocketError += (_, e) => result.TrySetResult(e);
        await page.EvaluateAsync(@"host => {
    new WebSocket('ws://' + host + '/bogus-ws');
  }", server.Host);
        var message = await result.Task;
        Assert.That(message, Does.Contain(channel?.Contains("msedge") == true ? "" : ": 400"));
    }

    [UpstreamTest("WS-011 should not tear down the page when a WebSocket is opened inside a worker")]
    public async Task ShouldNotTearDownThePageWhenAWebSocketIsOpenedInsideAWorker(IPage page, Server server)
    {
        server.SendOnWebSocketConnection("incoming");
        await page.GotoAsync(server.EmptyPage);
        var received = await page.EvaluateAsync<string>(@"host => {
    const code = `
      const ws = new WebSocket(${JSON.stringify('ws://' + host + '/ws')});
      ws.addEventListener('message', event => self.postMessage(event.data));
    `;
    const worker = new Worker(URL.createObjectURL(new Blob([code], { type: 'text/javascript' })));
    return new Promise(resolve => worker.addEventListener('message', event => resolve(event.data)));
  }", server.Host);
        Assert.That(received, Is.EqualTo("incoming"));
        // Opening a `WebSocket` inside a worker must not tear down the page session.
        Assert.That(await page.EvaluateAsync<int>("() => 1 + 1"), Is.EqualTo(2));
    }

    [UpstreamTest("WS-013 should send extra HTTP headers on WebSocket handshake")]
    public async Task ShouldSendExtraHTTPHeadersOnWebSocketHandshake(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Fixme(browserName == "chromium" && browserMajorVersion < 151, "Chromium before 151 does not send extra HTTP headers on WebSocket handshake");

        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["foo"] = "bar" });
        await page.GotoAsync(server.EmptyPage);
        var reqPromise = server.WaitForWebSocketConnectionRequest();
        await page.EvaluateAsync("host => { new WebSocket(`ws://${host}/ws`); }", server.Host);
        var req = await reqPromise;
        Assert.That(req.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("WS-014 should send extra HTTP headers on WebSocket handshake from a worker")]
    public async Task ShouldSendExtraHTTPHeadersOnWebSocketHandshakeFromAWorker(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Fixme(browserName == "chromium" && browserMajorVersion < 151, "Chromium before 151 does not send extra HTTP headers on WebSocket handshake");

        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["foo"] = "bar" });
        await page.GotoAsync(server.EmptyPage);
        var reqPromise = server.WaitForWebSocketConnectionRequest();
        await page.EvaluateAsync(@"host => {
    const code = `new WebSocket(${JSON.stringify('ws://' + host + '/ws')});`;
    new Worker(URL.createObjectURL(new Blob([code], { type: 'text/javascript' })));
  }", server.Host);
        var req = await reqPromise;
        Assert.That(req.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("WS-041 should navigate while an iframe opens WebSockets")]
    public async Task ShouldNavigateWhileAnIframeOpensWebSockets(IPage page, Server server)
    {
        server.SetRoute("/parent", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<iframe src=\"/child\"></iframe>");
        });
        server.SetRoute("/child", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<script>setInterval(() => new WebSocket('ws://' + location.host + '/ws'), 5)</script>");
        });
        for (var i = 0; i < 100; ++i)
        {
            await page.GotoAsync(server.Prefix + "/parent");
        }
        Assert.That(page.Frames, Has.Count.EqualTo(2));
        Assert.That(await page.EvaluateAsync<int>("() => 1 + 1"), Is.EqualTo(2));
    }
}
