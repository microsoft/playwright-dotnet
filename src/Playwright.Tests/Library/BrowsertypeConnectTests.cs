/**
 * Copyright 2017 Google Inc. All rights reserved.
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

using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

public abstract class BrowsertypeConnectTestsBase : UpstreamBrowserTest
{
    private IBrowser _connectedBrowser;
    private TcpListener _dummyServer;
    private TcpListener _ipV6Server;

    [SetUp]
    public void Slow() => Info.Slow(true, "All connect tests are slow");

    public sealed class ConnectTestOptions
    {
        // .NET: redirectPortForTest sets the __testHookRedirectPortForwarding option of connect(), which is not in the .NET API.
        public BrowserTypeLaunchOptions LaunchOptions { get; set; }
    }

    // .NET: JSON.stringify() leaves out the properties that are not set.
    protected static readonly JsonSerializerOptions _launchOptionsJson = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    // .NET: createHttpServer((req, res) => res.end(body)) of playwright-core with server.listen(0, host) is a TcpListener that answers every request with the body.
    protected static TcpListener CreateHttpServer(string host, string body)
    {
        var server = new TcpListener(IPAddress.Parse(host), 0);
        server.Start();
        _ = Task.Run(async () =>
        {
            while (true)
            {
                TcpClient socket;
                try
                {
                    socket = await SafeAccept.AcceptAsync(server);
                }
                catch (Exception)
                {
                    return;
                }
                _ = Task.Run(async () =>
                {
                    using (socket)
                    {
                        try
                        {
                            var stream = socket.GetStream();
                            using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                            while (!string.IsNullOrEmpty(await reader.ReadLineAsync()))
                            {
                            }
                            var payload = Encoding.UTF8.GetBytes(body);
                            await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n\r\n"));
                            await stream.WriteAsync(payload);
                        }
                        catch (IOException)
                        {
                        }
                    }
                });
            }
        });
        return server;
    }

    protected async Task<IBrowser> Connect(string wsEndpoint, BrowserTypeConnectOptions options = null, ConnectTestOptions testOptions = null)
    {
        options ??= new();
        testOptions ??= new();
        // .NET: (options as any).__testHookRedirectPortForwarding = testOptions.redirectPortForTest; the test hook is not in the .NET API.
        // .NET: browserType._playwright._defaultLaunchOptions is LaunchOptions(), so the local of upstream takes another name.
        var launchOptionsOfConnect = MergeOptions(new BrowserTypeLaunchOptions(testOptions.LaunchOptions), LaunchOptions());
        // .NET: the spread of options.headers is a loop.
        var headers = new Dictionary<string, string>
        {
            ["x-playwright-launch-options"] = JsonSerializer.Serialize(launchOptionsOfConnect, _launchOptionsJson),
        };
        foreach (var (name, value) in options.Headers ?? [])
        {
            headers[name] = value;
        }
        options.Headers = headers;
        _connectedBrowser = await BrowserType.ConnectAsync(wsEndpoint, options);
        return _connectedBrowser;
    }

    [TearDown]
    public async Task ConnectTearDown()
    {
        if (_connectedBrowser != null)
        {
            await _connectedBrowser.CloseAsync();
            _connectedBrowser = null;
        }
    }

    protected int DummyServerPort()
    {
        // Only listen on IPv4 to check that we don't try to connect to it via IPv6.
        _dummyServer ??= CreateHttpServer("127.0.0.1", "<html><body>from-dummy-server</body></html>");
        return ((IPEndPoint)_dummyServer.LocalEndpoint).Port;
    }

    [TearDown]
    public void DummyServerPortTearDown()
    {
        _dummyServer?.Stop();
        _dummyServer = null;
    }

    protected int IpV6ServerPort()
    {
        if (_ipV6Server == null)
        {
            Info.Skip(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("INSIDE_DOCKER")), "docker does not support IPv6 by default");
            _ipV6Server = CreateHttpServer("::1", "<html><body>from-ipv6-server</body></html>");
        }
        var address = (IPEndPoint)_ipV6Server.LocalEndpoint;
        return address.Port;
    }

    [TearDown]
    public void IpV6ServerPortTearDown()
    {
        _ipV6Server?.Stop();
        _ipV6Server = null;
    }
}

/// <summary>Port of tests/library/browsertype-connect.spec.ts.</summary>
public class BrowsertypeConnectTests : BrowsertypeConnectTestsBase
{
    [TestFixture("launchServer")]
    [TestFixture("run-server")]
    public class Describe : BrowsertypeConnectTestsBase
    {
        private readonly string kind;

        public Describe(string kind)
        {
            this.kind = kind;
        }

        [UpstreamTest("BT-035 should print HTTP error")]
        public async Task ShouldPrintHTTPError()
        {
            var error = await Catch(() => Connect($"ws://localhost:{Server.Port}/ws-401"));
            Assert.That(error.Message, Does.Contain("401"));
            Assert.That(error.Message, Does.Contain("Unauthorized body"));
        }

        [UpstreamTest("BT-036 should print ws error")]
        public async Task ShouldPrintWsError()
        {
            var error = await Catch(() => Connect("ws://does-not-exist.problem-domain:10987?secret=MYSECRET"));
            Assert.That(error.Message, Does.Contain("<ws connecting> ws://does-not-exist.problem-domain:10987/"));
            Assert.That(error.Message, Does.Contain("<ws error>"));
            Assert.That(error.Message, Does.Contain("getaddrinfo"));
            Assert.That(error.Message, Does.Not.Contain("secret=MYSECRET"));
        }

        [UpstreamTest("BT-037 should print custom ws close error")]
        [LocalFixme("dotnet: the message is 'Oh my!', without the 'browserType.connect: ' prefix of the call")]
        public async Task ShouldPrintCustomWsCloseError()
        {
            Server.OnceWebSocketConnection((ws, request) =>
            {
                ws.OnMessage(message =>
                {
                    ws.Close(4123, "Oh my!");
                });
            });
            var error = await Catch(() => Connect($"ws://localhost:{Server.Port}/ws"));
            Assert.That(error.Message, Does.Contain("browserType.connect: Oh my!"));
        }

        [UpstreamTest("BT-038 should be able to reconnect to a browser")]
        public async Task ShouldBeAbleToReconnectToABrowser()
        {
            var remoteServer = await StartRemoteServer(kind);
            {
                var browser = await Connect(remoteServer.WsEndpoint());
                var browserContext = await browser.NewContextAsync(ContextOptions());
                Assert.That(browserContext.Pages.Count, Is.EqualTo(0));
                var page = await browserContext.NewPageAsync();
                Assert.That(await page.EvaluateAsync<int>("11 * 11"), Is.EqualTo(121));
                await page.GotoAsync(Server.EmptyPage);
                await browser.CloseAsync();
            }
            {
                var browser = await Connect(remoteServer.WsEndpoint());
                var browserContext = await browser.NewContextAsync(ContextOptions());
                var page = await browserContext.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                await browser.CloseAsync();
            }
        }

        [UpstreamTest("BT-039 should not crash on malformed json frame")]
        public async Task ShouldNotCrashOnMalformedJsonFrame()
        {
            var remoteServer = await StartRemoteServer(kind);
            // .NET: new WebSocket(url) of the 'ws' package is a ClientWebSocket: ConnectAsync() ends at the 'open' event or fails at the 'error' event.
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(remoteServer.WsEndpoint()), CancellationToken.None);
            await ws.SendAsync(Encoding.UTF8.GetBytes("]"), WebSocketMessageType.Text, true, CancellationToken.None);
            // .NET: the 'close' event is a close message, or the end of the connection.
            try
            {
                while ((await ws.ReceiveAsync(new byte[1024], CancellationToken.None)).MessageType != WebSocketMessageType.Close)
                {
                }
            }
            catch (WebSocketException)
            {
            }
            // Server should still be alive and accept new connections.
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            Assert.That(await page.EvaluateAsync<int>("1 + 2"), Is.EqualTo(3));
            await browser.CloseAsync();
        }

        [UpstreamTest("BT-040 should be able to visit ipv6")]
        public async Task ShouldBeAbleToVisitIpv6()
        {
            var ipV6ServerPort = IpV6ServerPort();
            Info.Fixme(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("INSIDE_DOCKER")), "docker does not support IPv6 by default");
            Info.Fixme(Channel == "webkit-wsl", "WebKit on WSL does not support IPv6: https://github.com/microsoft/WSL/issues/10803");
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            var ipV6Url = "http://[::1]:" + ipV6ServerPort;
            await page.GotoAsync(ipV6Url);
            Assert.That(await page.ContentAsync(), Does.Contain("from-ipv6-server"));
            await browser.CloseAsync();
        }

        [UpstreamTest("BT-042 should be able to visit ipv6 through localhost")]
        public async Task ShouldBeAbleToVisitIpv6ThroughLocalhost()
        {
            var ipV6ServerPort = IpV6ServerPort();
            Info.Fixme(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("INSIDE_DOCKER")), "docker does not support IPv6 by default");
            Info.Skip(Channel == "webkit-wsl", "WebKit on WSL does not support IPv6: https://github.com/microsoft/WSL/issues/10803");
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            var ipV6Url = "http://localhost:" + ipV6ServerPort;
            await page.GotoAsync(ipV6Url);
            Assert.That(await page.ContentAsync(), Does.Contain("from-ipv6-server"));
            await browser.CloseAsync();
        }

        [UpstreamTest("BT-043 should be able to connect two browsers at the same time")]
        public async Task ShouldBeAbleToConnectTwoBrowsersAtTheSameTime()
        {
            var remoteServer = await StartRemoteServer(kind);

            var browser1 = await Connect(remoteServer.WsEndpoint());
            Assert.That(browser1.Contexts.Count, Is.EqualTo(0));
            await browser1.NewContextAsync(ContextOptions());
            Assert.That(browser1.Contexts.Count, Is.EqualTo(1));

            var browser2 = await Connect(remoteServer.WsEndpoint());
            Assert.That(browser2.Contexts.Count, Is.EqualTo(0));
            await browser2.NewContextAsync(ContextOptions());
            Assert.That(browser2.Contexts.Count, Is.EqualTo(1));
            Assert.That(browser1.Contexts.Count, Is.EqualTo(1));

            await browser1.CloseAsync();
            Assert.That(browser2.Contexts.Count, Is.EqualTo(1));
            var page2 = await browser2.NewPageAsync(NewPageOptions());
            Assert.That(await page2.EvaluateAsync<int>("() => 7 * 6"), Is.EqualTo(42)); // original browser should still work

            await browser2.CloseAsync();
        }

        [UpstreamTest("BT-044 should timeout in socket while connecting")]
        [LocalFixme("dotnet: the message is 'Timeout 1000ms exceeded.', without the 'browserType.connect: ' prefix of the call")]
        public async Task ShouldTimeoutInSocketWhileConnecting()
        {
            var e = await Catch(() => Connect($"ws://localhost:{Server.Port}/ws-slow", new()
            {
                Timeout = 1000,
            }));
            Assert.That(e.Message, Does.Contain("browserType.connect: Timeout 1000ms exceeded"));
        }

        [UpstreamTest("BT-045 should timeout in connect while connecting")]
        [LocalFixme("dotnet: the message is 'BrowserType.ConnectAsync: Timeout 100ms exceeded', upstream expects 'browserType.connect: Timeout 100ms exceeded'")]
        public async Task ShouldTimeoutInConnectWhileConnecting()
        {
            var e = await Catch(() => Connect($"ws://localhost:{Server.Port}/ws", new()
            {
                Timeout = 100,
            }));
            Assert.That(e.Message, Does.Contain("browserType.connect: Timeout 100ms exceeded"));
        }

        [UpstreamTest("BT-046 should send extra headers with connect request")]
        public async Task ShouldSendExtraHeadersWithConnectRequest()
        {
            var (request, _) = await PromiseAll(
                Server.WaitForWebSocketConnectionRequest(),
                Catch(() => Connect($"ws://localhost:{Server.Port}/ws", new()
                {
                    Headers = new Dictionary<string, string>
                    {
                        ["User-Agent"] = "Playwright",
                        ["foo"] = "bar",
                    },
                    Timeout = 100,
                })));
            Assert.That(request.Headers["user-agent"], Is.EqualTo("Playwright"));
            Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
        }

        [UpstreamTest("BT-048 should send default User-Agent and X-Playwright-Browser headers with connect request")]
        public async Task ShouldSendDefaultUserAgentAndXPlaywrightBrowserHeadersWithConnectRequest()
        {
            Info.Skip(IsFrozenWebkit);

            var (request, _) = await PromiseAll(
                Server.WaitForWebSocketConnectionRequest(),
                Catch(() => Connect($"ws://localhost:{Server.Port}/ws", new()
                {
                    Headers = new Dictionary<string, string>
                    {
                        ["foo"] = "bar",
                    },
                    Timeout = 100,
                })));
            // .NET: expect(request.headers['user-agent']).toBe(getUserAgent()); getUserAgent() is a function of the Node.js package (coreBundle), .NET has no API for it.
            Assert.That(request.Headers["x-playwright-browser"], Is.EqualTo(BrowserName));
            Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
        }

        [UpstreamTest("BT-049 should support slowmo option")]
        public async Task ShouldSupportSlowmoOption()
        {
            var remoteServer = await StartRemoteServer(kind);

            var browser1 = await Connect(remoteServer.WsEndpoint(), new() { SlowMo = 200 });
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await browser1.NewContextAsync(ContextOptions());
            await browser1.CloseAsync();
            Assert.That(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start, Is.GreaterThan(199));
        }

        [UpstreamTest("BT-050 disconnected event should be emitted when browser is closed or server is closed")]
        public async Task DisconnectedEventShouldBeEmittedWhenBrowserIsClosedOrServerIsClosed()
        {
            var remoteServer = await StartRemoteServer(kind);

            var browser1 = await Connect(remoteServer.WsEndpoint());
            await browser1.NewPageAsync(NewPageOptions());

            var browser2 = await Connect(remoteServer.WsEndpoint());
            await browser2.NewPageAsync(NewPageOptions());

            var disconnected1 = 0;
            var disconnected2 = 0;
            browser1.Disconnected += (_, _) => ++disconnected1;
            browser2.Disconnected += (_, _) => ++disconnected2;

            await PromiseAll(
                Once<IBrowser>(h => browser1.Disconnected += h, h => browser1.Disconnected -= h),
                browser1.CloseAsync());
            Assert.That(disconnected1, Is.EqualTo(1));
            Assert.That(disconnected2, Is.EqualTo(0));

            await PromiseAll(
                Once<IBrowser>(h => browser2.Disconnected += h, h => browser2.Disconnected -= h),
                remoteServer.Close());
            Assert.That(disconnected1, Is.EqualTo(1));
            Assert.That(disconnected2, Is.EqualTo(1));
        }

        [UpstreamTest("BT-051 disconnected event should have browser as argument")]
        public async Task DisconnectedEventShouldHaveBrowserAsArgument()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var disconnected = await PromiseAll(
                Once<IBrowser>(h => browser.Disconnected += h, h => browser.Disconnected -= h),
                browser.CloseAsync());
            Assert.That(disconnected, Is.SameAs(browser));
        }

        [UpstreamTest("BT-053 should set the browser connected state")]
        public async Task ShouldSetTheBrowserConnectedState()
        {
            var remoteServer = await StartRemoteServer(kind);
            var remote = await Connect(remoteServer.WsEndpoint());
            Assert.That(remote.IsConnected, Is.EqualTo(true));
            await remote.CloseAsync();
            Assert.That(remote.IsConnected, Is.EqualTo(false));
        }

        [UpstreamTest("BT-054 should throw when used after isConnected returns false")]
        [LocalFixme("harness: on the Windows bot the test times out after remoteServer.Close(), it is not known whether the server does not close or the browser gets no 'disconnected' event (only in chromium on win32)")]
        public async Task ShouldThrowWhenUsedAfterIsConnectedReturnsFalse()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            await PromiseAll(
                remoteServer.Close(),
                Once<IBrowser>(h => browser.Disconnected += h, h => browser.Disconnected -= h));
            Assert.That(browser.IsConnected, Is.EqualTo(false));
            var error = await Catch(() => page.EvaluateAsync("1 + 1"));
            Assert.That(error.Message, Does.Contain("closed"));
        }

        [UpstreamTest("BT-055 should throw when calling waitForNavigation after disconnect")]
        [LocalFixme("harness: on the Windows bot the test times out after remoteServer.Close(), it is not known whether the server does not close or the browser gets no 'disconnected' event (only in chromium on win32)")]
        public async Task ShouldThrowWhenCallingWaitForNavigationAfterDisconnect()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            await PromiseAll(
                remoteServer.Close(),
                Once<IBrowser>(h => browser.Disconnected += h, h => browser.Disconnected -= h));
            Assert.That(browser.IsConnected, Is.EqualTo(false));
            var error = await Catch(() => page.WaitForNavigationAsync());
            Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
        }

        [UpstreamTest("BT-056 should reject navigation when browser closes")]
        public async Task ShouldRejectNavigationWhenBrowserCloses()
        {
            var remoteServer = await StartRemoteServer(kind);
            Server.SetRoute("/one-style.css", (req, res) => { });
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            var navigationPromise = Catch(() => page.GotoAsync(Server.Prefix + "/one-style.html", new() { Timeout = 60000 }));
            await Server.WaitForRequest("/one-style.css");
            await browser.CloseAsync();
            var error = await navigationPromise;
            Assert.That(error.Message, Does.Contain("has been closed"));
        }

        [UpstreamTest("BT-057 should reject waitForSelector when browser closes")]
        public async Task ShouldRejectWaitForSelectorWhenBrowserCloses()
        {
            var remoteServer = await StartRemoteServer(kind);
            Server.SetRoute("/empty.html", (req, res) => { });
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            var watchdog = Catch(() => page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Attached, Timeout = 60000 }));

            // Make sure the previous waitForSelector has time to make it to the browser before we disconnect.
            await page.WaitForSelectorAsync("body", new() { State = WaitForSelectorState.Attached });

            await browser.CloseAsync();
            var error = await watchdog;
            Assert.That(error.Message, Does.Contain("has been closed"));
        }

        [UpstreamTest("BT-058 should emit close events on pages and contexts")]
        public async Task ShouldEmitCloseEventsOnPagesAndContexts()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var context = await browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();
            var pageClosed = false;
            page.Close += (_, _) => pageClosed = true;
            await PromiseAll(
                Once<IBrowserContext>(h => context.Close += h, h => context.Close -= h),
                remoteServer.Close());
            Assert.That(pageClosed, Is.True);
        }

        [UpstreamTest("BT-059 should terminate network waiters")]
        public async Task ShouldTerminateNetworkWaiters()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var newPage = await browser.NewPageAsync(NewPageOptions());
            // .NET: the two results of Promise.all() that the test reads are an array.
            var (result0, result1) = await PromiseAll(
                Catch(() => newPage.WaitForRequestAsync(Server.EmptyPage)),
                Catch(() => newPage.WaitForResponseAsync(Server.EmptyPage)),
                remoteServer.Close());
            var results = new[] { result0, result1 };
            for (var i = 0; i < 2; i++)
            {
                var message = results[i].Message;
                Assert.That(message, Does.Contain(TargetClosedErrorMessage));
                Assert.That(message, Does.Not.Contain("Timeout"));
            }
        }

        [UpstreamTest("BT-060 should reject waitForEvent before browser.close finishes")]
        [LocalFixme("dotnet: Browser.CloseAsync() of a connected browser returns before the waiters of its pages are rejected: Browser.DidClose runs when the connection is closed locally (Connection.Close), and the pages are closed later, when the pipe reports that it closed")]
        public async Task ShouldRejectWaitForEventBeforeBrowserCloseFinishes()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var newPage = await browser.NewPageAsync(NewPageOptions());
            var rejected = false;
            async Task WaitForEventCatch()
            {
                try
                {
                    await newPage.WaitForDownloadAsync();
                }
                catch (Exception)
                {
                    rejected = true;
                }
            }
            var promise = WaitForEventCatch();
            await browser.CloseAsync();
            Assert.That(rejected, Is.EqualTo(true));
            await promise;
        }

        [UpstreamTest("BT-061 should reject waitForEvent before browser.onDisconnect fires")]
        [LocalFixme("dotnet: the log is ['disconnected', 'rejected']: when the pipe closes, BrowserType.ConnectAsync closes the pages and raises Browser.Disconnected in one turn (OnPipeClosed), before the rejected waiter resumes; upstream calls browser._didClose() in a setTimeout (only in firefox)")]
        public async Task ShouldRejectWaitForEventBeforeBrowserOnDisconnectFires()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var newPage = await browser.NewPageAsync(NewPageOptions());
            var log = new List<string>();
            async Task WaitForEventCatch()
            {
                try
                {
                    await newPage.WaitForDownloadAsync();
                }
                catch (Exception)
                {
                    log.Add("rejected");
                }
            }
            var promise = WaitForEventCatch();
            browser.Disconnected += (_, _) => log.Add("disconnected");
            await remoteServer.Close();
            await promise;
            await Expect(() => Assert.That(log, Is.EqualTo(new[] { "rejected", "disconnected" }))).ToPassAsync();
        }

        [UpstreamTest("BT-062 should respect selectors")]
        public async Task ShouldRespectSelectors()
        {
            var remoteServer = await StartRemoteServer(kind);

            var mycss1 = "mycss1-" + kind;
            var mycss2 = "mycss2-" + kind;
            var mycss3 = "mycss3-" + kind;

            const string mycss = @"() => ({
        query(root, selector) {
          return root.querySelector(selector);
        },
        queryAll(root, selector) {
          return Array.from(root.querySelectorAll(selector));
        }
      })";
            // Register one engine before connecting.
            // .NET: a function is the script that the Node.js client makes of it, `(${fun})(undefined)`.
            await Playwright.Selectors.RegisterAsync(mycss1, new() { Script = $"({mycss})(undefined)" });

            var browser1 = await Connect(remoteServer.WsEndpoint());
            var context1 = await browser1.NewContextAsync(ContextOptions());

            // Register another engine after creating context.
            await Playwright.Selectors.RegisterAsync(mycss2, new() { Script = $"({mycss})(undefined)" });

            var page1 = await context1.NewPageAsync();
            await page1.SetContentAsync("<div>hello</div>");
            Assert.That(await page1.InnerHTMLAsync("css=div"), Is.EqualTo("hello"));
            Assert.That(await page1.InnerHTMLAsync($"{mycss1}=div"), Is.EqualTo("hello"));
            Assert.That(await page1.InnerHTMLAsync($"{mycss2}=div"), Is.EqualTo("hello"));

            var browser2 = await Connect(remoteServer.WsEndpoint());

            // Register third engine after second connect.
            await Playwright.Selectors.RegisterAsync(mycss3, new() { Script = $"({mycss})(undefined)" });

            var page2 = await browser2.NewPageAsync(NewPageOptions());
            await page2.SetContentAsync("<div>hello</div>");
            Assert.That(await page2.InnerHTMLAsync("css=div"), Is.EqualTo("hello"));
            Assert.That(await page2.InnerHTMLAsync($"{mycss1}=div"), Is.EqualTo("hello"));
            Assert.That(await page2.InnerHTMLAsync($"{mycss2}=div"), Is.EqualTo("hello"));
            Assert.That(await page2.InnerHTMLAsync($"{mycss3}=div"), Is.EqualTo("hello"));

            await browser1.CloseAsync();
        }

        [UpstreamTest("BT-063 should not throw on close after disconnect")]
        public async Task ShouldNotThrowOnCloseAfterDisconnect()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            await browser.NewPageAsync(NewPageOptions());
            await PromiseAll(
                Once<IBrowser>(h => browser.Disconnected += h, h => browser.Disconnected -= h),
                remoteServer.Close());
            await browser.CloseAsync();
        }

        [UpstreamTest("BT-064 should saveAs videos from remote browser")]
        public async Task ShouldSaveAsVideosFromRemoteBrowser()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var videosPath = Info.OutputPath();
            var context = await browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videosPath,
                RecordVideoSize = new() { Width = 320, Height = 240 },
            });
            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            Assert.That(Directory.GetFileSystemEntries(videosPath), Has.Length.EqualTo(0), "recordVideo.dir might be unaccessible in server mode, so /tmp/artifacts should be used");

            var savedAsPath = Info.OutputPath("my-video.webm");
            await page.Video.SaveAsAsync(savedAsPath);
            Assert.That(File.Exists(savedAsPath), Is.True);
        }

        [UpstreamTest("BT-065 should save videos to artifactsDir")]
        public async Task ShouldSaveVideosToArtifactsDir()
        {
            var artifactsDir = Info.OutputPath("artifacts");
            var remoteServer = await StartRemoteServer(kind, new() { ArtifactsDir = artifactsDir });
            var browser = await Connect(remoteServer.WsEndpoint());
            var localDir = Info.OutputPath("random-dir");
            var context = await browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = localDir,
                RecordVideoSize = new() { Width = 320, Height = 240 },
            });
            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var savedAsPath = Info.OutputPath("my-video.webm");
            await page.Video.SaveAsAsync(savedAsPath);
            Assert.That(File.Exists(savedAsPath), Is.True);
            Assert.That(Path.Exists(localDir), Is.False);

            await browser.CloseAsync();
        }

        [UpstreamTest("BT-068 should save download")]
        [LocalFixme("dotnet: the message is 'Path is not available when connecting remotely. Use SaveAsAsync() to save a local copy.', upstream expects 'Use saveAs() to save a local copy.'")]
        public async Task ShouldSaveDownload()
        {
            Server.SetRoute("/download", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.SetHeader("Content-Disposition", "attachment");
                res.End("Hello world");
            });

            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var nestedPath = Info.OutputPath(Path.Combine("these", "are", "directories", "download.txt"));
            await download.SaveAsAsync(nestedPath);
            Assert.That(File.Exists(nestedPath), Is.True);
            Assert.That(File.ReadAllText(nestedPath), Is.EqualTo("Hello world"));
            var error = await Catch(() => download.PathAsync());
            Assert.That(error.Message, Does.Contain("Path is not available when connecting remotely. Use saveAs() to save a local copy."));
            await browser.CloseAsync();
        }

        [UpstreamTest("BT-069 should error when saving download after deletion")]
        public async Task ShouldErrorWhenSavingDownloadAfterDeletion()
        {
            Server.SetRoute("/download", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.SetHeader("Content-Disposition", "attachment");
                res.End("Hello world");
            });

            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var userPath = Info.OutputPath("download.txt");
            await download.DeleteAsync();
            var message = (await Catch(() => download.SaveAsAsync(userPath))).Message;
            Assert.That(message, Does.Contain("Target page, context or browser has been closed"));
            await browser.CloseAsync();
        }

        [UpstreamTest("BT-071 should be able to connect when the wsEndpoint is passed as an option")]
        public async Task ShouldBeAbleToConnectWhenTheWsEndpointIsPassedAsAnOption()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await BrowserType.ConnectAsync(remoteServer.WsEndpoint(), new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["x-playwright-launch-options"] = JsonSerializer.Serialize(LaunchOptions(), _launchOptionsJson),
                },
            });
            var page = await browser.NewPageAsync(NewPageOptions());
            Assert.That(await page.EvaluateAsync<int>("1 + 2"), Is.EqualTo(3));
            await browser.CloseAsync();
        }

        [UpstreamTest("BT-072 should save har")]
        public async Task ShouldSaveHar()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var harPath = Info.OutputPath("test.har");
            var context = await browser.NewContextAsync(new(ContextOptions())
            {
                RecordHarPath = harPath,
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            await context.CloseAsync();
            await browser.CloseAsync();

            var log = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log");
            Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(1));
            var entry = log.GetProperty("entries")[0];
            Assert.That(entry.GetProperty("pageref").GetString(), Is.EqualTo(log.GetProperty("pages")[0].GetProperty("id").GetString()));
            Assert.That(entry.GetProperty("request").GetProperty("url").GetString(), Is.EqualTo(Server.EmptyPage));
        }

        [UpstreamTest("BT-074 should record trace with sources")]
        public async Task ShouldRecordTraceWithSources()
        {
            Info.Skip(Trace == "on");
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var context = await browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();

            await context.Tracing.StartAsync(new() { Sources = true });
            await page.GotoAsync(Server.EmptyPage);
            await page.SetContentAsync("<button>Click</button>");
            await page.ClickAsync("\"Click\"");
            await context.Tracing.StopAsync(new() { Path = Info.OutputPath("trace1.zip") });

            await context.CloseAsync();
            await browser.CloseAsync();

            var (_, resources, _, _, _) = await ParseTraceRaw(Info.OutputPath("trace1.zip"));
            var sourceNames = resources.Keys.Where(k => k.StartsWith("src/", StringComparison.Ordinal)).ToArray();
            Assert.That(sourceNames.Length, Is.EqualTo(1));
            var sourceFile = resources[sourceNames[0]];
            // .NET: __filename
            var thisFile = await File.ReadAllBytesAsync(new System.Diagnostics.StackFrame(true).GetFileName());
            Assert.That(sourceFile, Is.EqualTo(thisFile));
        }

        [UpstreamTest("BT-075 should fulfill with global fetch result")]
        public async Task ShouldFulfillWithGlobalFetchResult()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var context = await browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();

            await page.RouteAsync("**/*", async route =>
            {
                var request = await Playwright.APIRequest.NewContextAsync();
                // .NET: 'response' is a local of the test.
                var response_ = await request.GetAsync(Server.Prefix + "/simple.json");
                await route.FulfillAsync(new() { Response = response_ });
            });
            var response = await page.GotoAsync(Server.EmptyPage);
            Assert.That(response.Status, Is.EqualTo(200));
            ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
        }

        [UpstreamTest("BT-076 should upload large file")]
        public async Task ShouldUploadLargeFile()
        {
            if (IsMac)
            {
                Info.SetTimeout(Info.Timeout * 3);  // Extra slow!
            }

            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var context = await browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();

            await page.GotoAsync(Server.Prefix + "/input/fileupload.html");
            var uploadFile = Info.OutputPath("200MB.zip");
            var str = new string('A', 4 * 1024);
            // .NET: fs.createWriteStream(file) is a StreamWriter, and stream.end() is DisposeAsync().
            var stream = new StreamWriter(uploadFile);
            for (var i = 0; i < 50 * 1024; i++)
            {
                await stream.WriteAsync(str);
            }
            await stream.DisposeAsync();
            var input = page.Locator("input[type=\"file\"]");
            var events = await input.EvaluateHandleAsync(@"e => {
        const events = [];
        e.addEventListener('input', () => events.push('input'));
        e.addEventListener('change', () => events.push('change'));
        return events;
      }");
            await input.SetInputFilesAsync(uploadFile);
            Assert.That(await input.EvaluateAsync<string>("e => e.files[0].name"), Is.EqualTo("200MB.zip"));
            Assert.That(await events.EvaluateAsync<string[]>("e => e"), Is.EqualTo(new[] { "input", "change" }));
            var serverFilePromise = new TaskCompletionSource<FormidableFile>(TaskCreationOptions.RunContinuationsAsynchronously);
            Server.SetRoute("/upload", async (req, res) =>
            {
                var form = new IncomingForm(new() { UploadDir = Info.OutputPath() });
                var (err, fields, f) = await form.Parse(req);
                res.End();
                var files = f;
                serverFilePromise.TrySetResult(files["file1"]);
            });
            var file1 = await PromiseAll(
                serverFilePromise.Task,
                page.ClickAsync("input[type=submit]"));
            Assert.That(file1.OriginalFilename, Is.EqualTo("200MB.zip"));
            Assert.That(file1.Size, Is.EqualTo(200 * 1024 * 1024));
            await PromiseAll(new[] { uploadFile, file1.Filepath }.Select(file => Task.Run(() => File.Delete(file))));
        }

        [UpstreamTest("BT-077 should upload a folder")]
        public async Task ShouldUploadAFolder()
        {
            Info.Slow();
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var context = await browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();

            await page.GotoAsync(Server.Prefix + "/input/folderupload.html");
            var input = await page.QuerySelectorAsync("input");
            var folderName = "folder-upload-test";
            var dir = Info.OutputPath(folderName);
            {
                Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(Path.Combine(dir, "file1.txt"), "file1 content");
                await File.WriteAllTextAsync(Path.Combine(dir, "file2"), "file2 content");
                Directory.CreateDirectory(Path.Combine(dir, "sub-dir"));
                await File.WriteAllTextAsync(Path.Combine(dir, "sub-dir", "really.txt"), "sub-dir file content");
            }
            await input.SetInputFilesAsync(dir);

            var webkitRelativePaths = await page.EvaluateAsync<string[]>("e => [...e.files].map(f => f.webkitRelativePath)", input);
            ExpectToEqual(new HashSet<string>(webkitRelativePaths), new HashSet<object>
            {
                $"{folderName}/file1.txt",
                $"{folderName}/file2",
                $"{folderName}/sub-dir/really.txt",
            });

            for (var i = 0; i < webkitRelativePaths.Length; i++)
            {
                var content = await input.EvaluateAsync<string>(@"(e, i) => {
          const reader = new FileReader();
          const promise = new Promise(fulfill => reader.onload = fulfill);
          reader.readAsText(e.files[i]);
          return promise.then(() => reader.result);
        }", i);
                Assert.That(content, Is.EqualTo(File.ReadAllText(Path.Combine(dir, "..", webkitRelativePaths[i]))));
            }
        }

        [UpstreamTest("BT-078 setInputFiles should preserve lastModified timestamp")]
        public async Task SetInputFilesShouldPreserveLastModifiedTimestamp()
        {
            var remoteServer = await StartRemoteServer(kind);
            var browser = await Connect(remoteServer.WsEndpoint());
            var context = await browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();

            await page.SetContentAsync("<input type=file multiple=true/>");
            var input = page.Locator("input");
            var files = new[] { "file-to-upload.txt", "file-to-upload-2.txt" };
            await input.SetInputFilesAsync(files.Select(f => Asset(f)));
            Assert.That(await input.EvaluateAsync<string[]>("e => [...e.files].map(f => f.name)"), Is.EqualTo(files));
            var timestamps = await input.EvaluateAsync<double[]>("e => [...e.files].map(f => f.lastModified)");
            // .NET: fs.statSync(file).mtimeMs is the last write time in milliseconds since the Unix epoch.
            var expectedTimestamps = files.Select(file => Math.Round((File.GetLastWriteTimeUtc(Asset(file)) - DateTime.UnixEpoch).TotalMilliseconds, MidpointRounding.AwayFromZero)).ToArray();
            // On Linux browser sometimes reduces the timestamp by 1ms: 1696272058110.0715  -> 1696272058109 or even
            // rounds it to seconds in WebKit: 1696272058110 -> 1696272058000.
            for (var i = 0; i < timestamps.Length; i++)
            {
                Assert.That(Math.Abs(timestamps[i] - expectedTimestamps[i]), Is.LessThanOrEqualTo(1000), $"expected: {string.Join(",", expectedTimestamps)}; actual: {string.Join(",", timestamps)}");
            }
        }

        [UpstreamTest("BT-079 should connect over http")]
        public async Task ShouldConnectOverHttp()
        {
            var remoteServer = await StartRemoteServer(kind);

            var url = new Uri(remoteServer.WsEndpoint());
            var browser = await Connect($"http://localhost:{url.Port}");
            Assert.That(browser.Version, Is.Not.Empty);
            await browser.CloseAsync();
        }

        [TestFixture("launchServer")]
        [TestFixture("run-server")]
        public class SocksProxy : BrowsertypeConnectTestsBase
        {
            private readonly string kind;

            public SocksProxy(string kind)
            {
                this.kind = kind;
            }

            [SetUp]
            public void Skip() => Info.Skip(kind == "launchServer", "not supported yet");

            [UpstreamTest("BT-080 should forward non-forwarded requests")]
            public async Task ShouldForwardNonForwardedRequests()
            {
                var reachedOriginalTarget = false;
                Server.SetRoute("/foo.html", (req, res) =>
                {
                    reachedOriginalTarget = true;
                    res.End("<html><body>original-target</body></html>");
                });
                var remoteServer = await StartRemoteServer(kind);
                var browser = await Connect(remoteServer.WsEndpoint(), new() { ExposeNetwork = "*" });
                var page = await browser.NewPageAsync(NewPageOptions());
                await page.GotoAsync(Server.Prefix + "/foo.html");
                Assert.That(await page.ContentAsync(), Does.Contain("original-target"));
                Assert.That(reachedOriginalTarget, Is.EqualTo(true));
            }

            [UpstreamTest("BT-086 should lead to the error page for forwarded requests when the connection is refused")]
            public async Task ShouldLeadToTheErrorPageForForwardedRequestsWhenTheConnectionIsRefused()
            {
                var examplePort = 20_000 + (Info.WorkerIndex * 3);
                var remoteServer = await StartRemoteServer(kind);
                var browser = await Connect(remoteServer.WsEndpoint(), new() { ExposeNetwork = "*" });
                var page = await browser.NewPageAsync(NewPageOptions());
                var error = await Catch(() => page.GotoAsync($"http://127.0.0.1:{examplePort}"));
                if (BrowserName == "chromium")
                {
                    Assert.That(error.Message, Does.Contain("net::ERR_SOCKS_CONNECTION_FAILED at http://127.0.0.1:20"));
                }
                else if (BrowserName == "webkit")
                {
                    Assert.That(error.Message, Is.Not.Empty);
                }
                else if (BrowserName == "firefox")
                {
                    Assert.That(error.Message.Contains("NS_ERROR_NET_RESET") || error.Message.Contains("NS_ERROR_CONNECTION_REFUSED"), Is.EqualTo(true));
                }
            }
        }
    }

    public class LaunchServerOnly : BrowsertypeConnectTestsBase
    {
        [UpstreamTest("BT-089 should work with cluster")]
        public async Task ShouldWorkWithCluster()
        {
            var remoteServer = await StartRemoteServer("launchServer", new() { InCluster = true });
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            Assert.That(await page.EvaluateAsync<int>("1 + 2"), Is.EqualTo(3));
        }

        [UpstreamTest("BT-090 should properly disconnect when connection closes from the server side")]
        public async Task ShouldProperlyDisconnectWhenConnectionClosesFromTheServerSide()
        {
            Info.Skip(Platform == "win32", "Cannot send signals");

            Server.SetRoute("/one-style.css", (req, res) => { });
            var remoteServer = await StartRemoteServer("launchServer", new() { DisconnectOnSIGHUP = true });
            var browser = await Connect(remoteServer.WsEndpoint());
            var page = await browser.NewPageAsync(NewPageOptions());
            var navigationPromise = Catch(() => page.GotoAsync(Server.Prefix + "/one-style.html", new() { Timeout = 60000 }));
            var waitForNavigationPromise = Catch(() => page.WaitForNavigationAsync());

            var disconnectedPromise = Once<IBrowser>(h => browser.Disconnected += h, h => browser.Disconnected -= h);
            // This closes the websocket server.
            NodeProcess.Kill(remoteServer.Child().Id, "SIGHUP");
            await disconnectedPromise;
            Assert.That(browser.IsConnected, Is.EqualTo(false));

            Assert.That((await navigationPromise).Message, Does.Contain("has been closed"));
            Assert.That((await waitForNavigationPromise).Message, Does.Contain(TargetClosedErrorMessage));
            Assert.That((await Catch(() => page.GotoAsync(Server.EmptyPage))).Message, Does.Contain("has been closed"));
            Assert.That((await Catch(() => page.WaitForNavigationAsync())).Message, Does.Contain(TargetClosedErrorMessage));
        }

        [UpstreamTest("BT-091 should be able to reconnect to a browser 12 times without warnings")]
        public async Task ShouldBeAbleToReconnectToABrowser12TimesWithoutWarnings()
        {
            Info.Slow();
            var remoteServer = await StartRemoteServer("launchServer", new() { ExitOnWarning = true });
            for (var i = 0; i < 12; i++)
            {
                await Info.Step("connect #" + i, async () =>
                {
                    var browser = await Connect(remoteServer.WsEndpoint());
                    var browserContext = await browser.NewContextAsync(ContextOptions());
                    Assert.That(browserContext.Pages.Count, Is.EqualTo(0));
                    var page = await browserContext.NewPageAsync();
                    Assert.That(await page.EvaluateAsync<int>("11 * 11"), Is.EqualTo(121));
                    await page.GotoAsync(Server.EmptyPage);
                    await browser.CloseAsync();
                });
            }
        }
    }

    [UpstreamTest("BT-093 should refuse connecting when versions do not match")]
    public async Task ShouldRefuseConnectingWhenVersionsDoNotMatch()
    {
        var server = new RunServer();
        await server.Start(ChildProcess, new() { Mode = "default", Env = new() { ["PW_VERSION_OVERRIDE"] = "1.2.3" } });
        var error = await Catch(() => Connect(server.WsEndpoint()));
        await server.Close();
        Assert.That(error.Message, Does.Contain("Playwright version mismatch"));
        Assert.That(error.Message, Does.Contain("server version: v1.2"));
        // .NET: expect(error.message).toContain('client version: v' + getPlaywrightVersion(true)); getPlaywrightVersion() is a function of the Node.js package (coreBundle), .NET has no API for it.
    }

    [UpstreamTest("BT-094 should filter local paths from unsafe launch options")]
    public async Task ShouldFilterLocalPathsFromUnsafeLaunchOptions()
    {
        Info.Skip(Trace == "on", "the test starts its own tracing");

        Server.SetRoute("/download", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/octet-stream");
            res.SetHeader("Content-Disposition", "attachment");
            res.End("Hello world");
        });
        var artifactsDir = Info.OutputPath("artifacts");
        var downloadsPath = Info.OutputPath("downloads");
        File.WriteAllText(artifactsDir, "not a directory");
        File.WriteAllText(downloadsPath, "not a directory");
        var remoteServer = await StartRemoteServer("run-server", new() { Unsafe = true, Env = new() { ["PWTEST_UNDER_TEST"] = null } });
        var browser = await Connect(remoteServer.WsEndpoint(), new(), new() { LaunchOptions = new() { ArtifactsDir = artifactsDir, DownloadsPath = downloadsPath } });
        var context = await browser.NewContextAsync(ContextOptions());
        await context.Tracing.StartAsync(new() { Snapshots = true });
        var page = await context.NewPageAsync();
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download = await PromiseAll(
            page.WaitForDownloadAsync(),
            page.ClickAsync("a"));
        Assert.That(await download.FailureAsync(), Is.Null);
        var tracePath = Info.OutputPath("trace.zip");
        await context.Tracing.StopAsync(new() { Path = tracePath });
        await context.CloseAsync();
        await browser.CloseAsync();

        var (_, _, actions, _, _) = await ParseTraceRaw(tracePath);
        Assert.That(actions, Does.Contain("Set content"));
    }

    [UpstreamTest("BT-095 should timeout after redirect when connecting over http")]
    public async Task ShouldTimeoutAfterRedirectWhenConnectingOverHttp()
    {
        Server.SetRedirect("/connect/json", "/connect/slow");
        var aborted = false;
        Server.SetRoute("/connect/slow", (req, res) =>
        {
            req.Socket.OnClose(() => aborted = true);
        });
        var error = await Catch(() => Connect($"{Server.Prefix}/connect", new() { Timeout = 2000, Headers = new Dictionary<string, string> { ["Connection"] = "Close" } }));
        Assert.That(error.Message, Does.Contain("Timeout 2000ms exceeded."));
        await Expect(() => Assert.That(aborted, Is.EqualTo(true))).ToPassAsync();
    }
}
