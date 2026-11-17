/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/route-web-socket.spec.ts.</summary>
public class RouteWebSocketTests
{
    // Polyfill for Promise.withResolvers, not available in older Node.
    private static TaskCompletionSource<T> WithResolvers<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    // .NET: target is Page | Frame upstream.
    private static Task SetupWS(IPage target, Server server, string binaryType, object protocols = null, bool relativeURL = false) => SetupWS(target.MainFrame, server, binaryType, protocols, relativeURL);

    private static async Task SetupWS(IFrame target, Server server, string binaryType, object protocols = null, bool relativeURL = false)
    {
        await target.GotoAsync(server.EmptyPage);
        var wsUrl = relativeURL ? "/ws" : "ws://" + server.Host + "/ws";
        // .NET: protocols that is undefined upstream is not a property of the argument: null would be the protocol 'null'.
        var arg = new Dictionary<string, object> { ["wsUrl"] = wsUrl, ["binaryType"] = binaryType };
        if (protocols != null)
        {
            arg["protocols"] = protocols;
        }
        await target.EvaluateAsync(@"({ wsUrl, binaryType, protocols }) => {
    window.log = [];
    window.ws = new WebSocket(wsUrl, protocols);
    window.ws.binaryType = binaryType;
    window.ws.addEventListener('open', () => window.log.push('open'));
    window.ws.addEventListener('close', event => window.log.push(`close code=${event.code} reason=${event.reason} wasClean=${event.wasClean}`));
    window.ws.addEventListener('error', event => window.log.push(`error`));
    window.ws.addEventListener('message', async event => {
      let data;
      if (typeof event.data === 'string')
        data = event.data;
      else if (event.data instanceof Blob)
        data = 'blob:' + await event.data.text();
      else
        data = 'arraybuffer:' + await (new Blob([event.data])).text();
      window.log.push(`message: data=${data} origin=${event.origin} lastEventId=${event.lastEventId}`);
    });
    window.wsOpened = new Promise(f => window.ws.addEventListener('open', () => f()));
  }", arg);
    }

    [TestFixture("no-mock")]
    [TestFixture("no-match")]
    [TestFixture("pass-through")]
    public class Describe
    {
        private readonly string mock;

        public Describe(string mock)
        {
            this.mock = mock;
        }

        [BeforeEach]
        public async Task BeforeEach(IPage page)
        {
            if (mock == "no-match")
            {
                await page.RouteWebSocketAsync(new Regex("zzz"), _ => { });
            }
            else if (mock == "pass-through")
            {
                await page.RouteWebSocketAsync(new Regex(".*"), ws =>
                {
                    var server = ws.ConnectToServer();
                    // .NET: send(message) of a frame sends its Text or its Binary.
                    ws.OnMessage(message =>
                    {
                        if (message.Text != null)
                        {
                            server.Send(message.Text);
                        }
                        else
                        {
                            server.Send(message.Binary);
                        }
                    });
                    // .NET: send(message) of a frame sends its Text or its Binary.
                    server.OnMessage(message =>
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
            }
        }

        [UpstreamTest("WS-015 should work with text message")]
        public async Task ShouldWorkWithTextMessage(IPage page, Server server)
        {
            var wsPromise = server.WaitForWebSocket();
            var upgradePromise = server.WaitForUpgrade();
            await SetupWS(page, server, "blob");
            Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(0));
            var (doUpgrade, _) = await upgradePromise;
            Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(0));
            Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(Array.Empty<string>()));
            doUpgrade();
            await Expect(async () => Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(1))).ToPassAsync();
            Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "open" }));
            var ws = await wsPromise;
            ws.Send("hello");
            await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
            {
                "open",
                $"message: data=hello origin=ws://{server.Host} lastEventId=",
            }))).ToPassAsync();
            Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(1));
            var messagePromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            ws.OnceMessage(data => messagePromise.TrySetResult(data.ToString()));
            await page.EvaluateAsync("() => window.ws.send('hi')");
            Assert.That(await messagePromise.Task, Is.EqualTo("hi"));
            ws.Close(1008, "oops");
            await Expect(async () => Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(3))).ToPassAsync();
            Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
            {
                "open",
                $"message: data=hello origin=ws://{server.Host} lastEventId=",
                "close code=1008 reason=oops wasClean=true",
            }));
        }

        [UpstreamTest("WS-016 should work with binaryType=blob")]
        [LocalFixme("dotnet: IWebSocketFrame.Text is not null for a binary frame (WebSocketFrame.Text decodes Binary), so a handler can not tell a binary message from a text one and the pass-through route forwards 'hi' as text: the page logs 'data=hi', not 'data=blob:hi' (only for pass-through)")]
        public async Task ShouldWorkWithBinaryTypeBlob(IPage page, Server server)
        {
            var wsPromise = server.WaitForWebSocket();
            await SetupWS(page, server, "blob");
            var ws = await wsPromise;
            ws.Send(Encoding.UTF8.GetBytes("hi"));
            await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
            {
                "open",
                $"message: data=blob:hi origin=ws://{server.Host} lastEventId=",
            }))).ToPassAsync();
            var messagePromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            ws.OnceMessage(data => messagePromise.TrySetResult(data.ToString()));
            await page.EvaluateAsync("() => window.ws.send(new Blob([new Uint8Array(['h'.charCodeAt(0), 'i'.charCodeAt(0)])]))");
            Assert.That(await messagePromise.Task, Is.EqualTo("hi"));
        }

        [UpstreamTest("WS-017 should work with binaryType=arraybuffer")]
        [LocalFixme("dotnet: IWebSocketFrame.Text is not null for a binary frame (WebSocketFrame.Text decodes Binary), so a handler can not tell a binary message from a text one and the pass-through route forwards 'hi' as text: the page logs 'data=hi', not 'data=arraybuffer:hi' (only for pass-through)")]
        public async Task ShouldWorkWithBinaryTypeArraybuffer(IPage page, Server server)
        {
            var wsPromise = server.WaitForWebSocket();
            await SetupWS(page, server, "arraybuffer");
            var ws = await wsPromise;
            ws.Send(Encoding.UTF8.GetBytes("hi"));
            await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
            {
                "open",
                $"message: data=arraybuffer:hi origin=ws://{server.Host} lastEventId=",
            }))).ToPassAsync();
            var messagePromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            ws.OnceMessage(data => messagePromise.TrySetResult(data.ToString()));
            await page.EvaluateAsync("() => window.ws.send(new Uint8Array(['h'.charCodeAt(0), 'i'.charCodeAt(0)]).buffer)");
            Assert.That(await messagePromise.Task, Is.EqualTo("hi"));
        }

        [UpstreamTest("WS-018 should work when connection errors out")]
        public async Task ShouldWorkWhenConnectionErrorsOut(IPage page, Server server, string browserName)
        {
            Info.Skip(browserName == "webkit", "WebKit ignores the connection error and fires no events!");

            var upgradePromise = server.WaitForUpgrade();
            await SetupWS(page, server, "blob");
            var (_, socket) = await upgradePromise;
            Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(0));
            Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(Array.Empty<string>()));
            socket.Destroy();
            await Expect(async () => Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(3))).ToPassAsync();
            ExpectToEqual(await page.EvaluateAsync<string[]>("() => window.log"), new object[]
            {
                "error",
                StringMatching(new Regex(@"close code=\d+ reason= wasClean=false")),
            });
        }

        [UpstreamTest("WS-019 should work with error after successful open")]
        public async Task ShouldWorkWithErrorAfterSuccessfulOpen(IPage page, Server server, string browserName, bool isLinux, bool isWindows)
        {
            Info.Skip(browserName == "firefox", "Firefox does not close the websocket upon a bad frame");
            Info.Skip(browserName == "webkit" && isLinux, "WebKit linux does not close the websocket upon a bad frame");
            Info.Skip(browserName == "webkit" && isWindows, "WebKit Windows does not close the websocket upon a bad frame");

            var upgradePromise = server.WaitForUpgrade();
            await SetupWS(page, server, "blob");
            var (doUpgrade, socket) = await upgradePromise;
            doUpgrade();
            await Expect(async () => Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(1))).ToPassAsync();
            Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "open" }));
            socket.Write("garbage");
            await Expect(async () => Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(3))).ToPassAsync();
            ExpectToEqual(await page.EvaluateAsync<string[]>("() => window.log"), new object[]
            {
                "open",
                "error",
                StringMatching(new Regex(@"close code=\d+ reason= wasClean=false")),
            });
        }

        [UpstreamTest("WS-020 should work with client-side close")]
        public async Task ShouldWorkWithClientSideClose(IPage page, Server server)
        {
            var wsPromise = server.WaitForWebSocket();
            var upgradePromise = server.WaitForUpgrade();
            await SetupWS(page, server, "blob");
            Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(0));
            var (doUpgrade, _) = await upgradePromise;
            Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(0));
            Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(Array.Empty<string>()));
            doUpgrade();
            await Expect(async () => Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(1))).ToPassAsync();
            Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "open" }));
            var ws = await wsPromise;
            var closedPromise = new TaskCompletionSource<(int code, string reason)>(TaskCreationOptions.RunContinuationsAsynchronously);
            ws.OnClose((code, reason) => closedPromise.TrySetResult((code, reason)));
            var readyState = await page.EvaluateAsync<int>(@"() => {
        window.ws.close(3002, 'oops');
        return window.ws.readyState;
      }");
            Assert.That(readyState, Is.EqualTo(2));
            await Expect(async () => Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(3))).ToPassAsync();
            Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
            {
                "open",
                "close code=3002 reason=oops wasClean=true",
            }));
            var closed = await closedPromise.Task;
            Assert.That(closed.code, Is.EqualTo(3002));
            Assert.That(closed.reason, Is.EqualTo("oops"));
        }

        [UpstreamTest("WS-021 should pass through the required protocol")]
        public async Task ShouldPassThroughTheRequiredProtocol(IPage page, Server server)
        {
            await SetupWS(page, server, "blob", "my-custom-protocol");
            await page.EvaluateAsync("() => window.wsOpened");
            var protocol = await page.EvaluateAsync<string>("() => window.ws.protocol");
            Assert.That(protocol, Is.EqualTo("my-custom-protocol"));
        }

        [UpstreamTest("WS-022 should work with relative WebSocket URL")]
        [LocalFixme("dotnet: IWebSocketFrame.Text is not null for a binary frame (WebSocketFrame.Text decodes Binary), so a handler can not tell a binary message from a text one and the pass-through route forwards 'hi' as text: the page logs 'data=hi', not 'data=blob:hi' (only for pass-through)")]
        public async Task ShouldWorkWithRelativeWebSocketURL(IPage page, Server server)
        {
            var wsPromise = server.WaitForWebSocket();
            await SetupWS(page, server, "blob", null, true);
            var ws = await wsPromise;
            ws.Send(Encoding.UTF8.GetBytes("hi"));
            await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
            {
                "open",
                $"message: data=blob:hi origin=ws://{server.Host} lastEventId=",
            }))).ToPassAsync();
            var messagePromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            ws.OnceMessage(data => messagePromise.TrySetResult(data.ToString()));
            await page.EvaluateAsync("() => window.ws.send(new Blob([new Uint8Array(['h'.charCodeAt(0), 'i'.charCodeAt(0)])]))");
            Assert.That(await messagePromise.Task, Is.EqualTo("hi"));
        }
    }

    [UpstreamTest("WS-023 should work with ws.close")]
    public async Task ShouldWorkWithWsClose(IPage page, Server server)
    {
        var promise = WithResolvers<IWebSocketRoute>();
        // .NET: the parameter of the handler is `ws` upstream, which is a variable of the test here.
        await page.RouteWebSocketAsync(new Regex(".*"), ws1 =>
        {
            ws1.ConnectToServer();
            promise.TrySetResult(ws1);
        });

        var wsPromise = server.WaitForWebSocket();
        await SetupWS(page, server, "blob");
        var ws = await wsPromise;

        var route = await promise.Task;
        route.Send("hello");
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=hello origin=ws://{server.Host} lastEventId=",
        }))).ToPassAsync();

        var closedPromise = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        ws.OnClose((code, reason) => closedPromise.TrySetResult(new { code, reason }));
        await route.CloseAsync(new() { Code = 3009, Reason = "oops" });
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=hello origin=ws://{server.Host} lastEventId=",
            "close code=3009 reason=oops wasClean=true",
        }))).ToPassAsync();
        ExpectToEqual(await closedPromise.Task, new { code = 3009, reason = "oops" });
    }

    [UpstreamTest("WS-024 should observe upstream handshake failure when connectToServer is used")]
    public async Task ShouldObserveUpstreamHandshakeFailureWhenConnectToServerIsUsed(IPage page, Server server)
    {
        // Exercises the WebSocket-as-network-request path used by Firefox after the
        // har-WebSocket plumbing change, where a 4xx handshake response is synthesized
        // into a full lifecycle in FFPage._onWebSocketRequestFinished.
        var serverCloses = new List<(int? code, string reason)>();
        var routeHandlerInvoked = 0;
        await page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            ++routeHandlerInvoked;
            var serverRoute = ws.ConnectToServer();
            serverRoute.OnClose((code, reason) =>
            {
                serverCloses.Add((code, reason));
                IgnoreErrors(ws.CloseAsync());
            });
        });

        var upgradePromise = server.WaitForUpgrade();
        await SetupWS(page, server, "blob");
        var (_, socket) = await upgradePromise;
        socket.Write("HTTP/1.1 403 Forbidden\r\nContent-Length: 0\r\n\r\n");
        socket.Destroy();

        // Server-side route handler observes the close caused by the rejected handshake.
        await Expect(() => Assert.That(serverCloses.Count, Is.EqualTo(1))).ToPassAsync();
        Assert.That(serverCloses[0].code, Is.GreaterThanOrEqualTo(1000));

        // Once the route closes the page-side socket, the page sees the close.
        await Expect(async () => Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(3))).ToPassAsync();
        Assert.That(routeHandlerInvoked, Is.EqualTo(1));
    }

    [UpstreamTest("WS-025 should observe multiple concurrent routed WebSockets with connectToServer")]
    public async Task ShouldObserveMultipleConcurrentRoutedWebSocketsWithConnectToServer(IPage page, Server server)
    {
        // Exercises FFNetworkManager._webSocketRequestIds tracking by routing two
        // simultaneous WebSocket connections through `connectToServer()`. Each request
        // gets a distinct id in the new Firefox WebSocket-as-network-request flow.
        var routedConnections = 0;
        await page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            ++routedConnections;
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

        // Echo all incoming messages on whichever connection arrives; re-register after each.
        void handleConnection(ServerWebSocket ws)
        {
            ws.OnMessage(data => ws.Send($"echo-{data.ToString()}"));
            server.OnceWebSocketConnection(handleConnection);
        }
        server.OnceWebSocketConnection(handleConnection);

        await page.GotoAsync(server.EmptyPage);
        var results = await page.EvaluateAsync<string[]>(@"async host => {
    const collect = (tag) => new Promise(resolve => {
      const ws = new WebSocket(`ws://${host}/ws`);
      ws.addEventListener('open', () => ws.send(`hi-${tag}`));
      ws.addEventListener('message', event => {
        resolve(event.data);
        ws.close();
      });
    });
    return Promise.all([collect('a'), collect('b')]);
  }", server.Host);

        Assert.That(results.Order(StringComparer.Ordinal), Is.EqualTo(new[] { "echo-hi-a", "echo-hi-b" }));
        Assert.That(routedConnections, Is.EqualTo(2));
    }

    [UpstreamTest("WS-026 should pattern match")]
    public async Task ShouldPatternMatch(IPage page, Server server)
    {
        // .NET: the parameter of the handler is `ws` upstream, which is a variable of the test here.
        await page.RouteWebSocketAsync(new Regex(@".*\/ws$"), ws1 =>
        {
            ws1.ConnectToServer();
        });

        // .NET: the parameter of the handler is `ws` upstream, which is a variable of the test here.
        await page.RouteWebSocketAsync("**/mock-ws", ws1 =>
        {
            ws1.OnMessage(message =>
            {
                ws1.Send("mock-response");
            });
        });

        var wsPromise = server.WaitForWebSocket();

        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"async ({ host }) => {
    window.log = [];
    window.ws1 = new WebSocket('ws://' + host + '/ws');
    window.ws1.addEventListener('message', event => window.log.push(`ws1:${event.data}`));
    window.ws2 = new WebSocket('ws://' + host + '/something/something/mock-ws');
    window.ws2.addEventListener('message', event => window.log.push(`ws2:${event.data}`));
    await Promise.all([
      new Promise(f => window.ws1.addEventListener('open', f)),
      new Promise(f => window.ws2.addEventListener('open', f)),
    ]);
  }", new { host = server.Host });

        var ws = await wsPromise;
        ws.OnMessage(_ => ws.Send("response"));

        await page.EvaluateAsync("() => window.ws1.send('request')");
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "ws1:response" }))).ToPassAsync();

        await page.EvaluateAsync("() => window.ws2.send('request')");
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "ws1:response", "ws2:mock-response" }))).ToPassAsync();
    }

    [UpstreamTest("WS-027 should work with server")]
    public async Task ShouldWorkWithServer(IPage page, Server server)
    {
        var promise = WithResolvers<IWebSocketRoute>();
        // .NET: the parameter of the handler is `ws` upstream, which is a variable of the test here.
        await page.RouteWebSocketAsync(new Regex(".*"), ws1 =>
        {
            // .NET: the variable is `server` upstream, which is a parameter of the test here.
            var server1 = ws1.ConnectToServer();
            ws1.OnMessage(message =>
            {
                switch (message.Text)
                {
                    case "to-respond":
                        ws1.Send("response");
                        return;
                    case "to-block":
                        return;
                    case "to-modify":
                        server1.Send("modified");
                        return;
                }
                // .NET: send(message) of a frame sends its Text or its Binary.
                if (message.Text != null)
                {
                    server1.Send(message.Text);
                }
                else
                {
                    server1.Send(message.Binary);
                }
            });
            server1.OnMessage(message =>
            {
                switch (message.Text)
                {
                    case "to-block":
                        return;
                    case "to-modify":
                        ws1.Send("modified");
                        return;
                }
                // .NET: send(message) of a frame sends its Text or its Binary.
                if (message.Text != null)
                {
                    ws1.Send(message.Text);
                }
                else
                {
                    ws1.Send(message.Binary);
                }
            });
            server1.Send("fake");
            promise.TrySetResult(ws1);
        });

        var wsPromise = server.WaitForWebSocket();
        var log = new List<string>();
        // .NET: the parameter of the handler is `ws` upstream, which is a variable of the test here.
        server.OnceWebSocketConnection(ws1 =>
        {
            ws1.OnMessage(data => log.Add($"message: {data.ToString()}"));
            ws1.OnClose((code, reason) => log.Add($"close: code={code} reason={reason}"));
        });

        await SetupWS(page, server, "blob");
        var ws = await wsPromise;
        await Expect(() => Assert.That(log, Is.EqualTo(new[] { "message: fake" }))).ToPassAsync();

        ws.Send("to-modify");
        ws.Send("to-block");
        ws.Send("pass-server");
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=modified origin=ws://{server.Host} lastEventId=",
            $"message: data=pass-server origin=ws://{server.Host} lastEventId=",
        }))).ToPassAsync();

        await page.EvaluateAsync(@"() => {
    window.ws.send('to-respond');
    window.ws.send('to-modify');
    window.ws.send('to-block');
    window.ws.send('pass-client');
  }");
        await Expect(() => Assert.That(log, Is.EqualTo(new[] { "message: fake", "message: modified", "message: pass-client" }))).ToPassAsync();
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=modified origin=ws://{server.Host} lastEventId=",
            $"message: data=pass-server origin=ws://{server.Host} lastEventId=",
            $"message: data=response origin=ws://{server.Host} lastEventId=",
        }))).ToPassAsync();

        var route = await promise.Task;
        route.Send("another");
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=modified origin=ws://{server.Host} lastEventId=",
            $"message: data=pass-server origin=ws://{server.Host} lastEventId=",
            $"message: data=response origin=ws://{server.Host} lastEventId=",
            $"message: data=another origin=ws://{server.Host} lastEventId=",
        }))).ToPassAsync();

        await page.EvaluateAsync(@"() => {
    window.ws.send('pass-client-2');
  }");
        await Expect(() => Assert.That(log, Is.EqualTo(new[] { "message: fake", "message: modified", "message: pass-client", "message: pass-client-2" }))).ToPassAsync();

        await page.EvaluateAsync(@"() => {
    window.ws.close(3009, 'problem');
  }");
        await Expect(() => Assert.That(log, Is.EqualTo(new[] { "message: fake", "message: modified", "message: pass-client", "message: pass-client-2", "close: code=3009 reason=problem" }))).ToPassAsync();
    }

    [UpstreamTest("WS-028 should work without server")]
    public async Task ShouldWorkWithoutServer(IPage page, Server server)
    {
        var promise = WithResolvers<IWebSocketRoute>();
        await page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            ws.OnMessage(message =>
            {
                switch (message.Text)
                {
                    case "to-respond":
                        ws.Send("response");
                        return;
                }
            });
            promise.TrySetResult(ws);
        });

        await SetupWS(page, server, "blob");

        await page.EvaluateAsync(@"async () => {
    await window.wsOpened;
    window.ws.send('to-respond');
    window.ws.send('to-block');
    window.ws.send('to-respond');
  }");

        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=response origin=ws://{server.Host} lastEventId=",
            $"message: data=response origin=ws://{server.Host} lastEventId=",
        }))).ToPassAsync();

        var route = await promise.Task;
        route.Send("another");
        await route.CloseAsync(new() { Code = 3008, Reason = "oops" });

        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=response origin=ws://{server.Host} lastEventId=",
            $"message: data=response origin=ws://{server.Host} lastEventId=",
            $"message: data=another origin=ws://{server.Host} lastEventId=",
            "close code=3008 reason=oops wasClean=true",
        }))).ToPassAsync();
    }

    [UpstreamTest("WS-029 should route WebSockets in already loaded documents")]
    public async Task ShouldRouteWebSocketsInAlreadyLoadedDocuments(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var frame = await AttachFrame(page, "frame1", server.EmptyPage);
        await page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            ws.OnMessage(message => ws.Send("mock-" + message.Text));
        });

        foreach (var target in new[] { page.MainFrame, frame })
        {
            await target.EvaluateAsync(@"({ host }) => {
      window.log = [];
      window.ws = new WebSocket('ws://' + host + '/ws');
      window.ws.addEventListener('message', event => window.log.push(event.data));
      window.ws.addEventListener('close', () => window.log.push('close'));
      window.ws.addEventListener('open', () => window.ws.send('hi'));
    }", new { host = server.Host });
            await Expect(async () => Assert.That(await target.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "mock-hi" }))).ToPassAsync();
        }
    }

    [UpstreamTest("WS-030 should emit close upon frame navigation")]
    public async Task ShouldEmitCloseUponFrameNavigation(IPage page, Server server)
    {
        var promise = WithResolvers<IWebSocketRoute>();
        await page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            ws.ConnectToServer();
            promise.TrySetResult(ws);
        });

        await SetupWS(page, server, "blob");

        var route = await promise.Task;
        route.Send("hello");

        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=hello origin=ws://{server.Host} lastEventId=",
        }))).ToPassAsync();

        var closedPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        route.OnClose((_, _) => closedPromise.TrySetResult());
        await page.GotoAsync(server.EmptyPage);
        await closedPromise.Task;
    }

    [UpstreamTest("WS-031 should emit close upon frame detach")]
    public async Task ShouldEmitCloseUponFrameDetach(IPage page, Server server)
    {
        var promise = WithResolvers<IWebSocketRoute>();
        await page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            ws.ConnectToServer();
            promise.TrySetResult(ws);
        });

        var frame = await AttachFrame(page, "frame1", server.EmptyPage);
        await SetupWS(frame, server, "blob");

        var route = await promise.Task;
        route.Send("hello");

        await Expect(async () => Assert.That(await frame.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=hello origin=ws://{server.Host} lastEventId=",
        }))).ToPassAsync();

        var closedPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        route.OnClose((_, _) => closedPromise.TrySetResult());
        await DetachFrame(page, "frame1");
        await closedPromise.Task;
    }

    [UpstreamTest("WS-032 should route on context")]
    public async Task ShouldRouteOnContext(IPage page, Server server)
    {
        await page.RouteWebSocketAsync(new Regex("ws1"), ws =>
        {
            ws.OnMessage(message =>
            {
                ws.Send("page-mock-1");
            });
        });

        await page.RouteWebSocketAsync(new Regex("ws1"), ws =>
        {
            ws.OnMessage(message =>
            {
                ws.Send("page-mock-2");
            });
        });

        await page.Context.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            ws.OnMessage(message =>
            {
                ws.Send("context-mock-1");
            });
            ws.OnMessage(message =>
            {
                ws.Send("context-mock-2");
            });
        });

        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"({ host }) => {
    window.log = [];
    window.ws1 = new WebSocket('ws://' + host + '/ws1');
    window.ws1.addEventListener('message', event => window.log.push(`ws1:${event.data}`));
    window.ws2 = new WebSocket('ws://' + host + '/ws2');
    window.ws2.addEventListener('message', event => window.log.push(`ws2:${event.data}`));
  }", new { host = server.Host });

        await page.EvaluateAsync("() => window.ws1.send('request')");
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "ws1:page-mock-2" }))).ToPassAsync();

        await page.EvaluateAsync("() => window.ws2.send('request')");
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "ws1:page-mock-2", "ws2:context-mock-2" }))).ToPassAsync();
    }

    [UpstreamTest("WS-033 should not throw after page closure")]
    public async Task ShouldNotThrowAfterPageClosure(IPage page, Server server)
    {
        var promise = WithResolvers<IWebSocketRoute>();
        await page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            ws.ConnectToServer();
            promise.TrySetResult(ws);
        });

        await SetupWS(page, server, "blob");

        var route = await promise.Task;
        // .NET: route.send() gives no task, so Promise.all([page.close(), route.send('hello')]) is the two calls and the await of the first.
        var closePromise = page.CloseAsync();
        route.Send("hello");
        await closePromise;
    }

    [UpstreamTest("WS-034 should not throw with empty handler")]
    public async Task ShouldNotThrowWithEmptyHandler(IPage page, Server server)
    {
        await page.RouteWebSocketAsync(new Regex(".*"), _ => { });
        await SetupWS(page, server, "blob");
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "open" }))).ToPassAsync();
        await page.EvaluateAsync("() => window.ws.send('hi')");
        await page.EvaluateAsync("() => window.ws.send('hi2')");
        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "open" }))).ToPassAsync();
    }

    [UpstreamTest("WS-035 should throw when connecting twice")]
    public async Task ShouldThrowWhenConnectingTwice(IPage page, Server server)
    {
        var promise = WithResolvers<Exception>();

        await page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            ws.ConnectToServer();
            try
            {
                ws.ConnectToServer();
            }
            catch (Exception e)
            {
                promise.TrySetResult(e);
            }
        });
        await SetupWS(page, server, "blob");
        var error = await promise.Task;
        Assert.That(error.Message, Does.Contain("Already connected to the server"));
    }

    [UpstreamTest("WS-036 should work with no trailing slash")]
    public async Task ShouldWorkWithNoTrailingSlash(IPage page, Server server)
    {
        var log = new List<string>();
        // No trailing slash!
        await page.RouteWebSocketAsync("ws://" + server.Host, ws =>
        {
            ws.OnMessage(message =>
            {
                log.Add(message.Text);
                ws.Send("response");
            });
        });

        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"({ host }) => {
    window.log = [];
    // No trailing slash!
    window.ws = new WebSocket('ws://' + host);
    window.ws.addEventListener('message', event => window.log.push(event.data));
  }", new { host = server.Host });

        await Expect(async () => Assert.That(await page.EvaluateAsync<int>("() => window.ws.readyState"), Is.EqualTo(1))).ToPassAsync();
        await page.EvaluateAsync("() => window.ws.send('query')");
        await Expect(() => Assert.That(log, Is.EqualTo(new[] { "query" }))).ToPassAsync();
        Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[] { "response" }));
    }

    [UpstreamTest("WS-037 should work with baseURL")]
    public async Task ShouldWorkWithBaseURL(ContextFactory contextFactory, Server server)
    {
        var context = await contextFactory(new() { BaseURL = "http://" + server.Host });
        var page = await context.NewPageAsync();

        await page.RouteWebSocketAsync("/ws", ws =>
        {
            // .NET: send(message) of a frame sends its Text or its Binary.
            ws.OnMessage(message =>
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

        await SetupWS(page, server, "blob");

        await page.EvaluateAsync(@"async () => {
    await window.wsOpened;
    window.ws.send('echo');
  }");

        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=echo origin=ws://{server.Host} lastEventId=",
        }))).ToPassAsync();
    }

    [UpstreamTest("WS-038 should work with baseURL regardless of scheme casing")]
    [LocalFixme("dotnet: URLMatch.ToWebSocketBaseURL makes 'ws://' only from a lower-case 'http://' of the baseURL, upstream ignores the case of the scheme, so the route '/ws' does not match with the baseURL 'HTTP://localhost:<port>'")]
    public async Task ShouldWorkWithBaseURLRegardlessOfSchemeCasing(ContextFactory contextFactory, Server server)
    {
        // baseURL schemes are case-insensitive, same as everywhere else in URL matching.
        var context = await contextFactory(new() { BaseURL = "HTTP://" + server.Host });
        var page = await context.NewPageAsync();

        await page.RouteWebSocketAsync("/ws", ws =>
        {
            // .NET: send(message) of a frame sends its Text or its Binary.
            ws.OnMessage(message =>
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

        await SetupWS(page, server, "blob");

        await page.EvaluateAsync(@"async () => {
    await window.wsOpened;
    window.ws.send('echo');
  }");

        await Expect(async () => Assert.That(await page.EvaluateAsync<string[]>("() => window.log"), Is.EqualTo(new[]
        {
            "open",
            $"message: data=echo origin=ws://{server.Host} lastEventId=",
        }))).ToPassAsync();
    }

    [UpstreamTest("WS-039 should expose protocols to the route handler")]
    public async Task ShouldExposeProtocolsToTheRouteHandler(IPage page, Server server)
    {
        var routes = new List<IWebSocketRoute>();
        await page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            routes.Add(ws);
        });

        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"({ host }) => {
    window.wsNone = new WebSocket('ws://' + host + '/ws-none');
    window.wsString = new WebSocket('ws://' + host + '/ws-string', 'chat.v1');
    window.wsArray = new WebSocket('ws://' + host + '/ws-array', ['chat.v2', 'chat.v1']);
  }", new { host = server.Host });

        await Expect(() => Assert.That(routes.Count, Is.EqualTo(3))).ToPassAsync();

        var byUrl = routes.ToDictionary(r => new Uri(r.Url).AbsolutePath, r => r);
        Assert.That(byUrl["/ws-none"].Protocols, Is.EqualTo(Array.Empty<string>()));
        Assert.That(byUrl["/ws-string"].Protocols, Is.EqualTo(new[] { "chat.v1" }));
        Assert.That(byUrl["/ws-array"].Protocols, Is.EqualTo(new[] { "chat.v2", "chat.v1" }));
    }

    [UpstreamTest("WS-040 should expose protocols on server-side route")]
    public async Task ShouldExposeProtocolsOnServerSideRoute(IPage page, Server server)
    {
        var promise = WithResolvers<(IWebSocketRoute page, IWebSocketRoute server)>();
        await page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            // .NET: the variable is `serverRoute` upstream, which is a variable of the test here.
            var serverRoute1 = ws.ConnectToServer();
            promise.TrySetResult((ws, serverRoute1));
        });

        await SetupWS(page, server, "blob", new[] { "chat.v2", "chat.v1" });
        var (pageRoute, serverRoute) = await promise.Task;
        Assert.That(pageRoute.Protocols, Is.EqualTo(new[] { "chat.v2", "chat.v1" }));
        Assert.That(serverRoute.Protocols, Is.EqualTo(new[] { "chat.v2", "chat.v1" }));
    }
}
