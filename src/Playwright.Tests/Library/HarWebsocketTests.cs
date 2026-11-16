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

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/har-websocket.spec.ts.</summary>
public class HarWebsocketTests
{
    private static async Task<PageWithHarResult> PageWithHar(ContextFactory contextFactory, TestInfo testInfo, PageWithHarOptions options = null)
    {
        var harPath = testInfo.OutputPath(options?.OutputPath ?? "test.har");
        var context = await contextFactory(new() { RecordHarPath = harPath, RecordHarContent = options?.Content, RecordHarOmitContent = options?.OmitContent, IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        // .NET: { page, context, getLog, getZip } is the PageWithHarResult of the harness.
        return new PageWithHarResult(context, page, harPath);
    }

    private static int HeadersSize(JsonElement headers)
    {
        var result = 0;
        foreach (var header in headers.EnumerateArray())
        {
            result += header.GetProperty("name").GetString().Length + ": ".Length + header.GetProperty("value").GetString().Length + "\r\n".Length;
        }
        return result;
    }

    private static int RequestHeadersSize(JsonElement headers)
    {
        var result = "GET /ws HTTP/1.1\r\n".Length;
        result += HeadersSize(headers);
        return result;
    }

    private static int ResponseHeadersSize(JsonElement headers)
    {
        var result = "HTTP/1.1 101 Switching Protocols\r\n".Length;
        result += HeadersSize(headers);
        result += "\r\n".Length;
        return result;
    }

    // Browser-reported message times are derived from a wall-clock baseline plus
    // monotonic timestamps in the browser process, so they can drift by a few
    // milliseconds relative to Date.now() in the test process.
    private const int clockSkewMs = 100;

    private static int MessageSize(object message)
    {
        // .NET: message is a string or an int[], as string | number[] upstream.
        var length = message is string text ? text.Length : ((int[])message).Length;
        // The payload is short enough that they only need the minimum frame header size.
        if (length <= 125)
        {
            return 6 + length;
        }
        // The payload is large enough that additional bytes are needed to represent the payload length.
        if (length < Math.Pow(2, 16))
        {
            return 6 + 2 + length;
        }
        return 6 + 8 + length;
    }

    [UpstreamTest("HA-101 should only have one websocket entry")]
    public async Task ShouldOnlyHaveOneWebsocketEntry(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        server.OnceWebSocketConnection(ws =>
        {
            ws.OnMessage(_ => ws.Close());
        });
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        var wsUrl = $"ws://{server.Host}/ws";
        var closed = page.EvaluateAsync(@"url => new Promise(resolve => {
    const ws = new WebSocket(url);
    ws.addEventListener('open', () => ws.send('ping'));
    ws.addEventListener('close', () => resolve());
  })", wsUrl);
        await closed;
        var log = await pageWithHar.GetLog();
        var wsEntries = log.GetProperty("entries").EnumerateArray().Where(e => e.GetProperty("request").GetProperty("url").GetString().EndsWith($"://{server.Host}/ws", StringComparison.Ordinal)).ToArray();
        Assert.That(wsEntries.Length, Is.EqualTo(1));

        var wsEntry = wsEntries[0];
        Assert.That(wsEntry.GetProperty("_resourceType").GetString(), Is.EqualTo("websocket"));
    }

    [UpstreamTest("HA-102 should include websocket handshake headers and status")]
    public async Task ShouldIncludeWebsocketHandshakeHeadersAndStatus(ContextFactory contextFactory, Server server, string browserName, bool isMac, int macVersion, TestInfo testInfo)
    {
        server.OnceWebSocketConnection(ws =>
        {
            ws.OnMessage(_ => ws.Close());
        });

        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);

        var beforeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var wsUrl = $"ws://{server.Host}/ws";
        var closed = page.EvaluateAsync(@"url => new Promise(resolve => {
    const ws = new WebSocket(url);
    ws.addEventListener('open', () => ws.send('ping'));
    ws.addEventListener('close', () => resolve());
  })", wsUrl);
        await closed;
        var log = await pageWithHar.GetLog();
        var afterMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var wsEntry = log.GetProperty("entries").EnumerateArray().First(e => e.GetProperty("request").GetProperty("url").GetString() == wsUrl);
        Assert.That(wsEntry.GetProperty("_resourceType").GetString(), Is.EqualTo("websocket"));
        Assert.That(wsEntry.GetProperty("request").GetProperty("headersSize").GetInt32(), Is.EqualTo(RequestHeadersSize(wsEntry.GetProperty("request").GetProperty("headers"))));
        Assert.That(wsEntry.GetProperty("response").GetProperty("status").GetInt32(), Is.EqualTo(101));
        Assert.That(wsEntry.GetProperty("response").GetProperty("statusText").GetString(), Is.EqualTo("Switching Protocols"));
        Assert.That(wsEntry.GetProperty("response").GetProperty("headersSize").GetInt32(), Is.EqualTo(ResponseHeadersSize(wsEntry.GetProperty("response").GetProperty("headers"))));

        var wallTimeMs = DateTimeOffset.Parse(wsEntry.GetProperty("startedDateTime").GetString(), CultureInfo.InvariantCulture).ToUnixTimeMilliseconds();
        Assert.That(wallTimeMs, Is.GreaterThanOrEqualTo(beforeMs - clockSkewMs));
        Assert.That(wallTimeMs, Is.LessThanOrEqualTo(afterMs + clockSkewMs));

        var requestHeaderNames = wsEntry.GetProperty("request").GetProperty("headers").EnumerateArray().Select(h => h.GetProperty("name").GetString().ToLowerInvariant()).ToArray();
        Assert.That(requestHeaderNames, Does.Contain("upgrade"));
        Assert.That(requestHeaderNames, Does.Contain("connection"));
        Assert.That(requestHeaderNames, Does.Contain("sec-websocket-key"));
        Assert.That(requestHeaderNames, Does.Contain("sec-websocket-version"));
        var upgradeHeader = wsEntry.GetProperty("request").GetProperty("headers").EnumerateArray().First(h => h.GetProperty("name").GetString().ToLowerInvariant() == "upgrade");
        Assert.That(upgradeHeader.GetProperty("value").GetString().ToLowerInvariant(), Is.EqualTo("websocket"));

        var responseHeaderNames = wsEntry.GetProperty("response").GetProperty("headers").EnumerateArray().Select(h => h.GetProperty("name").GetString().ToLowerInvariant()).ToArray();
        Assert.That(responseHeaderNames, Does.Contain("upgrade"));
        Assert.That(responseHeaderNames, Does.Contain("connection"));
        Assert.That(responseHeaderNames, Does.Contain("sec-websocket-accept"));
    }

    private static async Task TestWebSocketMessages(ContextFactory contextFactory, Server server, TestInfo testInfo, HarContentPolicy content, string channel = null)
    {
        var incomingText = new[] { new string('x', 125), new string('x', 126), new string('x', (int)Math.Pow(2, 16)) };
        var incomingBinary = new[] { Enumerable.Repeat(0x01, 125).ToArray(), Enumerable.Repeat(0x01, 126).ToArray(), Enumerable.Repeat(0x01, (int)Math.Pow(2, 16)).ToArray() };
        var outgoingText = new[] { new string('y', 125), new string('y', 126), new string('y', (int)Math.Pow(2, 16)) };
        var outgoingBinary = new[] { Enumerable.Repeat(0x02, 125).ToArray(), Enumerable.Repeat(0x02, 126).ToArray(), Enumerable.Repeat(0x02, (int)Math.Pow(2, 16)).ToArray() };
        var incomingCount = incomingText.Length + incomingBinary.Length;
        var delayMs = 100;

        server.OnceWebSocketConnection(async ws =>
        {
            foreach (var text in incomingText)
            {
                ws.Send(text);
                await Task.Delay(delayMs);
            }
            foreach (var binary in incomingBinary)
            {
                await Task.Delay(delayMs);
                ws.Send(binary.Select(b => (byte)b).ToArray());
            }
        });

        var outputPath = content == HarContentPolicy.Embed ? null : "test.har.zip";
        var pageWithHar = await PageWithHar(contextFactory, testInfo, new() { Content = content, OutputPath = outputPath });
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);

        var beforeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var wsUrl = $"ws://{server.Host}/ws";
        var closed = page.EvaluateAsync(@"({ url, incomingCount, outgoingText, outgoingBinary, delayMs }) => new Promise(resolve => {
    let count = 0;
    const ws = new WebSocket(url);
    ws.addEventListener('message', async () => {
      if (++count < incomingCount)
        return;
      for (const text of outgoingText) {
        ws.send(text);
        await new Promise(resolve => window.builtins.setTimeout(resolve, delayMs));
      }
      for (const binary of outgoingBinary) {
        await new Promise(resolve => window.builtins.setTimeout(resolve, delayMs));
        ws.send(new Uint8Array(binary));
      }
      ws.close();
    });
    ws.addEventListener('close', () => resolve());
  })", new { url = wsUrl, incomingCount, outgoingText, outgoingBinary, delayMs });
        await closed;
        var afterMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Dictionary<string, byte[]> zip = null;
        JsonElement log;
        if (outputPath != null)
        {
            zip = await pageWithHar.GetZip();
            log = JsonDocument.Parse(Encoding.UTF8.GetString(zip["har.har"])).RootElement.GetProperty("log");
        }
        else
        {
            log = await pageWithHar.GetLog();
        }
        var wsEntry = log.GetProperty("entries").EnumerateArray().First(e => e.GetProperty("request").GetProperty("url").GetString() == wsUrl);
        Assert.That(wsEntry.GetProperty("response").GetProperty("_transferSize").GetInt32(), Is.EqualTo(ResponseHeadersSize(wsEntry.GetProperty("response").GetProperty("headers")) + incomingText.Cast<object>().Concat(incomingBinary).Aggregate(0, (accumulator, current) => accumulator + MessageSize(current))));
        Assert.That(wsEntry.GetProperty("time").GetDouble(), Is.LessThanOrEqualTo(afterMs - beforeMs));

        if (content == HarContentPolicy.Omit)
        {
            Assert.That(wsEntry.GetProperty("response").GetProperty("content").TryGetProperty("_file", out _), Is.False);
            return;
        }

        JsonElement[] messages;
        if (content == HarContentPolicy.Attach)
        {
            Assert.That(wsEntry.TryGetProperty("_webSocketMessages", out _), Is.False);
            var file = wsEntry.GetProperty("response").GetProperty("content").GetProperty("_file").GetString();
            Assert.That(file, Does.Match(@"^[0-9a-f]+\.jsonl$"));
            messages = Encoding.UTF8.GetString(zip[file]).Split('\n').Where(line => line.Length > 0).Select(line => JsonDocument.Parse(line).RootElement).ToArray();
        }
        else
        {
            messages = wsEntry.GetProperty("_webSocketMessages").EnumerateArray().ToArray();
        }
        ExpectToEqual(
            messages.Select(m => new { type = m.GetProperty("type").GetString(), opcode = m.GetProperty("opcode").GetInt32(), data = m.GetProperty("opcode").GetInt32() == 1 ? (object)m.GetProperty("data").GetString() : Convert.FromBase64String(m.GetProperty("data").GetString()).Select(b => (int)b).ToArray() }),
            incomingText.Select(m => new { type = "receive", opcode = 1, data = (object)m })
                .Concat(incomingBinary.Select(m => new { type = "receive", opcode = 2, data = (object)m }))
                .Concat(outgoingText.Select(m => new { type = "send", opcode = 1, data = (object)m }))
                .Concat(outgoingBinary.Select(m => new { type = "send", opcode = 2, data = (object)m })));
        // The WSL VM clock drifts relative to the Windows host clock, so the browser-reported
        // message times cannot be compared against the host wall clock.
        if (channel != "webkit-wsl")
        {
            foreach (var m in messages)
            {
                Assert.That(m.GetProperty("time").GetDouble(), Is.GreaterThanOrEqualTo(beforeMs - clockSkewMs));
                Assert.That(m.GetProperty("time").GetDouble(), Is.LessThanOrEqualTo(afterMs + clockSkewMs));
            }
        }
        Assert.That(messages[0].GetProperty("time").GetDouble(), Is.LessThanOrEqualTo(messages[1].GetProperty("time").GetDouble()));
        Assert.That(wsEntry.GetProperty("time").GetDouble(), Is.GreaterThanOrEqualTo(messages[messages.Length - 1].GetProperty("time").GetDouble() - messages[0].GetProperty("time").GetDouble()));
    }

    [UpstreamTest("HA-103 should embed websocket messages")]
    public async Task ShouldEmbedWebsocketMessages(ContextFactory contextFactory, Server server, string channel, TestInfo testInfo)
    {
        await TestWebSocketMessages(contextFactory, server, testInfo, HarContentPolicy.Embed, channel);
    }

    [UpstreamTest("HA-104 should attach websocket messages")]
    public async Task ShouldAttachWebsocketMessages(ContextFactory contextFactory, Server server, string channel, TestInfo testInfo)
    {
        await TestWebSocketMessages(contextFactory, server, testInfo, HarContentPolicy.Attach, channel);
    }

    [UpstreamTest("HA-106 should omit websocket messages")]
    public async Task ShouldOmitWebsocketMessages(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        await TestWebSocketMessages(contextFactory, server, testInfo, HarContentPolicy.Omit);
    }

    [UpstreamTest("HA-107 should record websocket connection failure")]
    public async Task ShouldRecordWebsocketConnectionFailure(ContextFactory contextFactory, Server server, string channel, TestInfo testInfo)
    {
        It.Skip(channel == "webkit-wsl", "Connection to an unbound localhost port from WSL is not refused in mirrored networking mode");
        // Reserve a port and immediately release it so the WebSocket connect attempt is refused.
        var portReservation = new TcpListener(IPAddress.Parse("127.0.0.1"), 0);
        portReservation.Start();
        var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
        portReservation.Stop();

        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);

        var wsUrl = $"ws://127.0.0.1:{port}/ws-connect-fail";
        var wsPromise = page.WaitForWebSocketAsync();
        await page.EvaluateAsync(@"url => new Promise(resolve => {
    const ws = new WebSocket(url);
    ws.addEventListener('close', () => resolve());
    ws.addEventListener('error', () => resolve());
  })", wsUrl);
        await wsPromise;
        var log = await pageWithHar.GetLog();

        var wsEntry = log.GetProperty("entries").EnumerateArray().First(e => e.GetProperty("request").GetProperty("url").GetString() == wsUrl);
        Assert.That(wsEntry.GetProperty("_resourceType").GetString(), Is.EqualTo("websocket"));
        Assert.That(wsEntry.GetProperty("response").GetProperty("_failureText").GetString(), Is.Not.Empty);
    }

    [UpstreamTest("HA-108 should record websocket handshake failure")]
    public async Task ShouldRecordWebsocketHandshakeFailure(ContextFactory contextFactory, Server server, string browserName, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);

        var wsUrl = $"ws://{server.Host}/ws-handshake-fail";
        var upgradePromise = server.WaitForUpgrade();
        var wsClose = page.EvaluateAsync(@"url => new Promise(resolve => {
    const ws = new WebSocket(url);
    ws.addEventListener('close', () => resolve());
    ws.addEventListener('error', () => resolve());
  })", wsUrl);
        var (_, socket) = await upgradePromise;
        socket.Write("HTTP/1.1 403 Forbidden\r\nContent-Length: 0\r\n\r\n");
        socket.Destroy();
        await wsClose;
        var log = await pageWithHar.GetLog();

        var wsEntry = log.GetProperty("entries").EnumerateArray().First(e => e.GetProperty("request").GetProperty("url").GetString() == wsUrl);
        Assert.That(wsEntry.GetProperty("_resourceType").GetString(), Is.EqualTo("websocket"));
        if (browserName != "chromium")
        {
            // Chromium only reports an error instead of giving a status code and text.
            Assert.That(wsEntry.GetProperty("response").GetProperty("status").GetInt32(), Is.EqualTo(403));
            Assert.That(wsEntry.GetProperty("response").GetProperty("statusText").GetString(), Is.EqualTo("Forbidden"));
        }
        Assert.That(wsEntry.GetProperty("response").GetProperty("_failureText").GetString(), Is.Not.Empty);
    }

    [UpstreamTest("HA-109 should still capture websocket when route passes messages through")]
    public async Task ShouldStillCaptureWebsocketWhenRoutePassesMessagesThrough(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        server.OnceWebSocketConnection(ws =>
        {
            ws.OnMessage(_ => ws.Send("incoming"));
        });

        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        var routeHandlerCalled = 0;
        await page.RouteWebSocketAsync(new Regex("/ws$"), ws =>
        {
            ++routeHandlerCalled;
            var serverRoute = ws.ConnectToServer();
            // .NET: send(message) of a frame sends its Text or its Binary.
            ws.OnMessage(message =>
            {
                if (message.Text != null)
                {
                    serverRoute.Send(message.Text);
                }
                else
                {
                    serverRoute.Send(message.Binary);
                }
            });
            // .NET: send(message) of a frame sends its Text or its Binary.
            serverRoute.OnMessage(message =>
            {
                if (message.Text != null)
                {
                    ws.Send(message.Text);
                }
                else
                {
                    ws.Send(message.Binary);
                }
            });
        });
        await page.GotoAsync(server.EmptyPage);

        var wsUrl = $"ws://{server.Host}/ws";
        var messages = await page.EvaluateAsync<string[]>(@"url => new Promise(resolve => {
    const seen = [];
    const ws = new WebSocket(url);
    ws.addEventListener('open', () => ws.send('outgoing'));
    ws.addEventListener('message', event => {
      seen.push(event.data);
      ws.close();
    });
    ws.addEventListener('close', () => resolve(seen));
  })", wsUrl);
        Assert.That(routeHandlerCalled, Is.EqualTo(1));
        Assert.That(messages, Is.EqualTo(new[] { "incoming" }));
        var log = await pageWithHar.GetLog();

        var wsEntries = log.GetProperty("entries").EnumerateArray().Where(e => e.GetProperty("request").GetProperty("url").GetString() == wsUrl).ToArray();
        Assert.That(wsEntries.Length, Is.EqualTo(1));
        Assert.That(wsEntries[0].GetProperty("_resourceType").GetString(), Is.EqualTo("websocket"));
        Assert.That(wsEntries[0].GetProperty("response").GetProperty("status").GetInt32(), Is.EqualTo(101));
        ExpectToEqual(wsEntries[0].GetProperty("_webSocketMessages").EnumerateArray().Select(m => new { type = m.GetProperty("type").GetString(), data = m.GetProperty("data").GetString() }), new[]
        {
            new { type = "send", data = "outgoing" },
            new { type = "receive", data = "incoming" },
        });
    }

    [UpstreamTest("HA-110 should still allow routeWebSocket to fully mock the connection when capturing HAR")]
    public async Task ShouldStillAllowRouteWebSocketToFullyMockTheConnectionWhenCapturingHAR(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        var routeHandlerCalled = 0;
        await page.RouteWebSocketAsync(new Regex("/ws$"), ws =>
        {
            ++routeHandlerCalled;
            ws.OnMessage(message =>
            {
                if (message.Text == "ping")
                {
                    ws.Send("pong");
                }
            });
        });
        await page.GotoAsync(server.EmptyPage);

        var wsUrl = $"ws://{server.Host}/ws";
        var messages = await page.EvaluateAsync<string[]>(@"url => new Promise(resolve => {
    const seen = [];
    const ws = new WebSocket(url);
    ws.addEventListener('open', () => ws.send('ping'));
    ws.addEventListener('message', event => {
      seen.push(event.data);
      ws.close();
    });
    ws.addEventListener('close', () => resolve(seen));
  })", wsUrl);
        Assert.That(routeHandlerCalled, Is.EqualTo(1));
        Assert.That(messages, Is.EqualTo(new[] { "pong" }));
        var log = await pageWithHar.GetLog();

        // Fully mocked routes never create a native WebSocket, so nothing should be recorded.
        var wsEntries = log.GetProperty("entries").EnumerateArray().Where(e => e.GetProperty("request").GetProperty("url").GetString() == wsUrl).ToArray();
        ExpectToEqual(wsEntries, Array.Empty<object>());
    }

    [UpstreamTest("HA-111 should still allow routeWebSocket to modify messages when capturing HAR")]
    public async Task ShouldStillAllowRouteWebSocketToModifyMessagesWhenCapturingHAR(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        server.OnceWebSocketConnection(ws =>
        {
            ws.OnMessage(message => ws.Send($"server-saw-{message.Text}"));
        });

        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        var routeHandlerCalled = 0;
        await page.RouteWebSocketAsync(new Regex("/ws$"), ws =>
        {
            ++routeHandlerCalled;
            var serverRoute = ws.ConnectToServer();
            ws.OnMessage(message => serverRoute.Send($"modified-{message.Text}"));
            serverRoute.OnMessage(message => ws.Send($"page-got-{message.Text}"));
        });
        await page.GotoAsync(server.EmptyPage);

        var wsUrl = $"ws://{server.Host}/ws";
        var messages = await page.EvaluateAsync<string[]>(@"url => new Promise(resolve => {
    const seen = [];
    const ws = new WebSocket(url);
    ws.addEventListener('open', () => ws.send('hello'));
    ws.addEventListener('message', event => {
      seen.push(event.data);
      ws.close();
    });
    ws.addEventListener('close', () => resolve(seen));
  })", wsUrl);
        Assert.That(routeHandlerCalled, Is.EqualTo(1));
        // The page sees the route-modified server response.
        Assert.That(messages, Is.EqualTo(new[] { "page-got-server-saw-modified-hello" }));
        var log = await pageWithHar.GetLog();

        // HAR records actual wire traffic from the native WebSocket: outgoing messages
        // are modified by the client-side route handler before they hit the server,
        // and incoming messages are recorded before the server-side route handler modifies them.
        var wsEntries = log.GetProperty("entries").EnumerateArray().Where(e => e.GetProperty("request").GetProperty("url").GetString() == wsUrl).ToArray();
        Assert.That(wsEntries.Length, Is.EqualTo(1));
        ExpectToEqual(wsEntries[0].GetProperty("_webSocketMessages").EnumerateArray().Select(m => new { type = m.GetProperty("type").GetString(), data = m.GetProperty("data").GetString() }), new[]
        {
            new { type = "send", data = "modified-hello" },
            new { type = "receive", data = "server-saw-modified-hello" },
        });
    }
}
