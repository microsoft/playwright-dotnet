/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/multiclient.spec.ts.</summary>
public class MulticlientTests
{
    [SetUp]
    public void Slow() => Info.Slow(true, "All connect tests are slow");

    public delegate Task<IBrowser> ConnectFixture(string wsEndpoint, BrowserTypeConnectOptions options = null);

    // .NET: browserType.launchServer({ _sharedBrowser: true }) is not in the .NET API. startRemoteServer('launchServer', { sharedBrowser: true }) makes this call in Node.js, and closes the server.
    [Fixture]
    public static Task<PlaywrightServer> RemoteServer(StartRemoteServer startRemoteServer) => startRemoteServer("launchServer", new() { SharedBrowser = true });

    [Fixture]
    public static async Task Connect(IBrowserType browserType, Func<ConnectFixture, Task> use)
    {
        var browsers = new List<IBrowser>();
        await use(async (wsEndpoint, options) =>
        {
            var browser = await browserType.ConnectAsync(wsEndpoint, options);
            browsers.Add(browser);
            return browser;
        });
        await PromiseAll(browsers.Select(b => b.CloseAsync()));
    }

    [Fixture]
    public static async Task TwoPages(PlaywrightServer remoteServer, ConnectFixture connect, BrowserNewContextOptions contextOptions, Func<(IPage pageA, IPage pageB), Task> use)
    {
        var browserA = await connect(remoteServer.WsEndpoint());
        var contextA = await browserA.NewContextAsync(contextOptions);
        var pageA = await contextA.NewPageAsync();

        var browserB = await connect(remoteServer.WsEndpoint());
        var contextB = browserB.Contexts[0];
        var pageB = contextB.Pages[0];

        await use((pageA, pageB));
    }

    [BeforeEach]
    public static void Skip(string mode) => Info.Skip(mode != "default");

    private static async Task Disconnect(IPage page)
    {
        await page.Context.Browser.CloseAsync();
        // Give disconnect some time to cleanup.
        await Task.Delay(1000);
    }

    [UpstreamTest("MC-001 should connect two clients")]
    public async Task ShouldConnectTwoClients(ConnectFixture connect, PlaywrightServer remoteServer, Server server, BrowserNewContextOptions contextOptions)
    {
        var browserA = await connect(remoteServer.WsEndpoint());
        Assert.That(browserA.Contexts.Count, Is.EqualTo(0));
        var contextA1 = await browserA.NewContextAsync(contextOptions);
        var pageA1 = await contextA1.NewPageAsync();
        await pageA1.GotoAsync(server.EmptyPage);

        var browserB = await connect(remoteServer.WsEndpoint());
        Assert.That(browserB.Contexts.Count, Is.EqualTo(1));
        var contextB1 = browserB.Contexts[0];
        Assert.That(contextB1.Pages.Count, Is.EqualTo(1));
        var pageB1 = contextB1.Pages[0];
        await Expect(pageB1).ToHaveURLAsync(server.EmptyPage);

        var contextB2 = await browserB.NewContextAsync(new(contextOptions) { BaseURL = server.Prefix });
        Assert.That(browserB.Contexts, Is.EqualTo(new[] { contextB1, contextB2 }));
        await Expect(() => Assert.That(browserA.Contexts.Count, Is.EqualTo(2))).ToPassAsync();
        var contextA2 = browserA.Contexts[1];
        Assert.That(browserA.Contexts, Is.EqualTo(new[] { contextA1, contextA2 }));

        var pageEventPromise = Once<IPage>(h => contextB2.Page += h, h => contextB2.Page -= h);
        var pageA2 = await contextA2.NewPageAsync();
        var pageB2 = await pageEventPromise;
        await pageA2.GotoAsync("/frames/frame.html");
        await Expect(pageB2).ToHaveURLAsync("/frames/frame.html");

        // Both contexts and pages should be still operational after any client disconnects.
        await Disconnect(pageA1);

        await Expect(pageB1).ToHaveURLAsync(server.EmptyPage);
        await Expect(pageB2).ToHaveURLAsync(server.Prefix + "/frames/frame.html");
    }

    [UpstreamTest("MC-002 should fire context event on remote newContext")]
    public async Task ShouldFireContextEventOnRemoteNewContext(ConnectFixture connect, PlaywrightServer remoteServer, BrowserNewContextOptions contextOptions)
    {
        var browserA = await connect(remoteServer.WsEndpoint());
        var events = new List<IBrowserContext>();
        browserA.Context += (_, ctx) => events.Add(ctx);

        var browserB = await connect(remoteServer.WsEndpoint());
        var contextB = await browserB.NewContextAsync(contextOptions);

        await Expect(() => Assert.That(events, Has.Count.EqualTo(1))).ToPassAsync();
        Assert.That(browserA.Contexts, Is.EqualTo(new[] { events[0] }));
        Assert.That(events[0], Is.Not.SameAs(contextB));
    }

    [UpstreamTest("MC-003 should have separate default timeouts")]
    public async Task ShouldHaveSeparateDefaultTimeouts((IPage pageA, IPage pageB) twoPages)
    {
        var (pageA, pageB) = twoPages;
        pageA.SetDefaultTimeout(500);
        pageB.SetDefaultTimeout(600);

        var (errorA, errorB) = await PromiseAll(
            Catch(() => pageA.ClickAsync("div")),
            Catch(() => pageB.ClickAsync("div")));
        Assert.That(errorA.Message, Does.Contain("Timeout 500ms exceeded"));
        Assert.That(errorB.Message, Does.Contain("Timeout 600ms exceeded"));
    }

    [UpstreamTest("MC-004 should receive viewport size changes")]
    public async Task ShouldReceiveViewportSizeChanges((IPage pageA, IPage pageB) twoPages)
    {
        var (pageA, pageB) = twoPages;

        await pageA.SetViewportSizeAsync(567, 456);
        ExpectToEqual(pageA.ViewportSize, new { width = 567, height = 456 });
        await Expect(() => ExpectToEqual(pageB.ViewportSize, new { width = 567, height = 456 })).ToPassAsync();

        await pageB.SetViewportSizeAsync(456, 567);
        ExpectToEqual(pageB.ViewportSize, new { width = 456, height = 567 });
        await Expect(() => ExpectToEqual(pageA.ViewportSize, new { width = 456, height = 567 })).ToPassAsync();
    }

    [UpstreamTest("MC-007 should unpause clock")]
    public async Task ShouldUnpauseClock((IPage pageA, IPage pageB) twoPages)
    {
        var (pageA, pageB) = twoPages;
        // .NET: a time in milliseconds is a DateTime.
        await pageA.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(1000) });
        await pageA.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(2000));
        var promise = pageB.EvaluateAsync("() => new Promise(f => setTimeout(f, 1000))");
        await Disconnect(pageA);
        await promise;
    }

    [UpstreamTest("MC-008 last emulateMedia wins")]
    public async Task LastEmulateMediaWins((IPage pageA, IPage pageB) twoPages)
    {
        var (pageA, pageB) = twoPages;
        await pageA.EmulateMediaAsync(new() { Media = Media.Print });
        Assert.That(await pageB.EvaluateAsync<bool>("() => window.matchMedia('screen').matches"), Is.EqualTo(false));
        Assert.That(await pageA.EvaluateAsync<bool>("() => window.matchMedia('print').matches"), Is.EqualTo(true));
        await pageB.EmulateMediaAsync(new() { Media = Media.Screen });
        Assert.That(await pageB.EvaluateAsync<bool>("() => window.matchMedia('screen').matches"), Is.EqualTo(true));
        Assert.That(await pageA.EvaluateAsync<bool>("() => window.matchMedia('print').matches"), Is.EqualTo(false));
    }

    [UpstreamTest("MC-009 should chain routes")]
    public async Task ShouldChainRoutes((IPage pageA, IPage pageB) twoPages, Server server)
    {
        var (pageA, pageB) = twoPages;

        server.SetRoute("/foo", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End("<div>server-foo</div>");
        });
        server.SetRoute("/bar", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End("<div>server-bar</div>");
        });
        server.SetRoute("/qux", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End("<div>server-qux</div>");
        });

        var stall = false;
        var stallPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await pageA.RouteAsync("**/foo", async route =>
        {
            if (stall)
            {
                stallPromise.TrySetResult();
            }
            else
            {
                await route.FallbackAsync();
            }
        });
        await pageA.RouteAsync("**/bar", async route =>
        {
            await route.FulfillAsync(new() { Body = "<div>intercepted-bar</div>", ContentType = "text/html" });
        });

        await pageB.RouteAsync("**/foo", async route =>
        {
            await route.FulfillAsync(new() { Body = "<div>intercepted2-foo</div>", ContentType = "text/html" });
        });
        await pageB.RouteAsync("**/bar", async route =>
        {
            await route.FulfillAsync(new() { Body = "<div>intercepted2-bar</div>", ContentType = "text/html" });
        });
        await pageB.RouteAsync("**/qux", async route =>
        {
            await route.FulfillAsync(new() { Body = "<div>intercepted2-qux</div>", ContentType = "text/html" });
        });

        await pageA.GotoAsync(server.Prefix + "/foo");
        await Expect(pageB.Locator("div")).ToHaveTextAsync("intercepted2-foo");

        await pageA.GotoAsync(server.Prefix + "/bar");
        await Expect(pageB.Locator("div")).ToHaveTextAsync("intercepted-bar");

        await pageA.GotoAsync(server.Prefix + "/qux");
        await Expect(pageB.Locator("div")).ToHaveTextAsync("intercepted2-qux");

        stall = true;
        var gotoPromise = pageB.GotoAsync(server.Prefix + "/foo");
        await stallPromise.Task;
        await pageA.Context.Browser.CloseAsync();

        await gotoPromise;
        await Expect(pageB.Locator("div")).ToHaveTextAsync("intercepted2-foo");

        await pageB.GotoAsync(server.Prefix + "/bar");
        await Expect(pageB.Locator("div")).ToHaveTextAsync("intercepted2-bar");
    }

    [UpstreamTest("MC-010 should chain routes with changed url")]
    [Ignore("fixme")]
    public async Task ShouldChainRoutesWithChangedUrl((IPage pageA, IPage pageB) twoPages, Server server)
    {
        var (pageA, pageB) = twoPages;

        server.SetRoute("/foo", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End("<div>server-foo</div>");
        });
        server.SetRoute("/baz", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End("<div>server-baz</div>");
        });

        await pageA.RouteAsync("**/foo", async route =>
        {
            await route.FallbackAsync(new() { Url = server.Prefix + "/baz" });
        });
        await pageB.RouteAsync("**/baz", async route =>
        {
            await route.FulfillAsync(new() { Body = "<div>intercepted2-baz</div>", ContentType = "text/html" });
        });

        await pageA.GotoAsync(server.Prefix + "/foo");
        await Expect(pageB.Locator("div")).ToHaveTextAsync("intercepted2-baz");
    }

    [UpstreamTest("MC-011 should remove exposed bindings upon disconnect")]
    public async Task ShouldRemoveExposedBindingsUponDisconnect((IPage pageA, IPage pageB) twoPages)
    {
        var (pageA, pageB) = twoPages;

        await pageA.ExposeBindingAsync("pageBindingA", _ => "pageBindingAResult");
        await pageA.EvaluateAsync(@"() => {
    window.pageBindingACopy = window.pageBindingA;
  }");
        Assert.That(await pageB.EvaluateAsync<string>("() => window.pageBindingA()"), Is.EqualTo("pageBindingAResult"));
        Assert.That(await pageB.EvaluateAsync<bool>("() => !!window.pageBindingACopy"), Is.EqualTo(true));

        await pageA.Context.ExposeBindingAsync("contextBindingA", _ => "contextBindingAResult");
        Assert.That(await pageB.EvaluateAsync<string>("() => window.contextBindingA()"), Is.EqualTo("contextBindingAResult"));

        await pageB.ExposeBindingAsync("pageBindingB", _ => "pageBindingBResult");
        Assert.That(await pageA.EvaluateAsync<string>("() => window.pageBindingB()"), Is.EqualTo("pageBindingBResult"));
        await pageB.Context.ExposeBindingAsync("contextBindingB", _ => "contextBindingBResult");
        Assert.That(await pageA.EvaluateAsync<string>("() => window.contextBindingB()"), Is.EqualTo("contextBindingBResult"));

        await Disconnect(pageA);

        Assert.That(await pageB.EvaluateAsync<object>("() => window.pageBindingA"), Is.Null);
        Assert.That(await pageB.EvaluateAsync<object>("() => window.contextBindingA"), Is.Null);
        var error = await Catch(() => pageB.EvaluateAsync("() => window.pageBindingACopy()"));
        Assert.That(error.Message, Does.Contain("binding \"pageBindingA\" has been removed"));

        Assert.That(await pageB.EvaluateAsync<string>("() => window.pageBindingB()"), Is.EqualTo("pageBindingBResult"));
    }

    [UpstreamTest("MC-012 should unroute websockets")]
    public async Task ShouldUnrouteWebsockets((IPage pageA, IPage pageB) twoPages, Server server)
    {
        var (pageA, pageB) = twoPages;

        await pageA.GotoAsync(server.EmptyPage);
        await pageA.RouteWebSocketAsync(new Regex(".*"), _ => { });
        await pageA.RouteWebSocketAsync(new Regex(".*"), _ => { });
        await pageA.RouteWebSocketAsync(new Regex(".*"), _ => { });

        var error = await Catch(() => pageB.RouteWebSocketAsync(new Regex(".*"), _ => { }));
        Assert.That(error.Message, Does.Contain("Another client is already routing WebSockets"));

        await Disconnect(pageA);

        var promise = new TaskCompletionSource<IWebSocketRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await pageB.RouteWebSocketAsync(new Regex(".*"), ws => promise.TrySetResult(ws));
        await pageB.GotoAsync(server.EmptyPage);
        await pageB.EvaluateAsync("host => window.ws = new WebSocket('ws://' + host + '/ws')", server.Host);
        await promise.Task;
    }

    [UpstreamTest("MC-013 should remove init scripts upon disconnect")]
    public async Task ShouldRemoveInitScriptsUponDisconnect((IPage pageA, IPage pageB) twoPages, Server server)
    {
        var (pageA, pageB) = twoPages;

        await pageA.AddInitScriptAsync("(() => window.pageValueA = 'pageValueA')()");
        await pageA.Context.AddInitScriptAsync("(() => window.contextValueA = 'contextValueA')()");
        await pageB.GotoAsync(server.EmptyPage);
        Assert.That(await pageB.EvaluateAsync<string>("() => window.pageValueA"), Is.EqualTo("pageValueA"));
        Assert.That(await pageB.EvaluateAsync<string>("() => window.contextValueA"), Is.EqualTo("contextValueA"));

        await pageB.AddInitScriptAsync("(() => window.pageValueB = 'pageValueB')()");
        await pageB.Context.AddInitScriptAsync("(() => window.contextValueB = 'contextValueB')()");
        await pageA.GotoAsync(server.EmptyPage);
        Assert.That(await pageA.EvaluateAsync<string>("() => window.pageValueB"), Is.EqualTo("pageValueB"));
        Assert.That(await pageA.EvaluateAsync<string>("() => window.contextValueB"), Is.EqualTo("contextValueB"));

        await Disconnect(pageB);

        await pageA.GotoAsync(server.EmptyPage);
        Assert.That(await pageA.EvaluateAsync<object>("() => window.pageValueB"), Is.Null);
        Assert.That(await pageA.EvaluateAsync<object>("() => window.contextValueB"), Is.Null);
        Assert.That(await pageA.EvaluateAsync<string>("() => window.pageValueA"), Is.EqualTo("pageValueA"));
        Assert.That(await pageA.EvaluateAsync<string>("() => window.contextValueA"), Is.EqualTo("contextValueA"));
    }

    [UpstreamTest("MC-014 should remove locator handlers upon disconnect")]
    public async Task ShouldRemoveLocatorHandlersUponDisconnect((IPage pageA, IPage pageB) twoPages, Server server)
    {
        var (pageA, pageB) = twoPages;

        await pageA.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var count = 0;
        await pageA.AddLocatorHandlerAsync(pageA.GetByText("This interstitial covers the button"), async () =>
        {
            ++count;
            await pageA.Locator("#close").ClickAsync();
        });

        await pageA.Locator("#aside").HoverAsync();
        await pageA.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('mouseover', 1);
  }");
        await pageA.Locator("#target").ClickAsync();
        Assert.That(count, Is.EqualTo(1));
        Assert.That(await pageB.EvaluateAsync<int>("window.clicked"), Is.EqualTo(1));
        await Expect(pageB.Locator("#interstitial")).Not.ToBeVisibleAsync();

        await pageA.Locator("#aside").HoverAsync();
        await pageA.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('mouseover', 1);
  }");

        await Disconnect(pageA);

        var error = await Catch(() => pageB.Locator("#target").ClickAsync(new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("Timeout 3000ms exceeded"));
        Assert.That(error.Message, Does.Contain("intercepts pointer events"));
        Assert.That(error.Message, Does.Not.Contain("locator handler"));
    }

    [UpstreamTest("MC-016 should avoid side effects upon disconnect")]
    public async Task ShouldAvoidSideEffectsUponDisconnect((IPage pageA, IPage pageB) twoPages, Server server)
    {
        var (pageA, pageB) = twoPages;

        var counter = 0;
        pageB.Console += (_, _) => ++counter;

        var promise = Catch(() => pageA.WaitForFunctionAsync(@"() => {
    window['counter'] = (window['counter'] || 0) + 1;
    console.log(window['counter']);
  }", new { }, new() { PollingInterval = 1, Timeout = 10000 }));

        await Disconnect(pageA);
        var error = await promise;
        var savedCounter = counter;

        await pageB.WaitForTimeoutAsync(2000); // Give it some time to produce more logs.

        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
        Assert.That(counter, Is.EqualTo(savedCounter));
    }

    [UpstreamTest("MC-017 screencast should deliver cached last frame to a new client")]
    public async Task ScreencastShouldDeliverCachedLastFrameToANewClient((IPage pageA, IPage pageB) twoPages, Server server, string trace, string video)
    {
        Info.Skip(trace == "on", "trace=on has screencast active on the page already");
        Info.Skip(video == "on", "video=on has screencast active on the page already");

        var (pageA, pageB) = twoPages;
        await pageA.GotoAsync(server.EmptyPage);
        await pageA.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");

        var framesA = new List<byte[]>();
        await pageA.Screencast.StartAsync(new()
        {
            OnFrame = frame =>
            {
                framesA.Add(frame.Data);
                return Task.CompletedTask;
            },
            Size = new() { Width = 320, Height = 240 },
        });
        await EnsureSomeFrames(pageA);
        Assert.That(framesA.Count, Is.GreaterThan(0));
        var lastFrameA = framesA[framesA.Count - 1];

        var framesB = new List<byte[]>();
        await pageB.Screencast.StartAsync(new()
        {
            OnFrame = frame =>
            {
                framesB.Add(frame.Data);
                return Task.CompletedTask;
            },
        });
        // Second client should receive the cached last frame without waiting for a browser repaint.
        await Expect(() => Assert.That(framesB.Count, Is.GreaterThan(0))).ToPassAsync(new() { Timeout = 5000 });
        Assert.That(framesB[0].SequenceEqual(lastFrameA), Is.EqualTo(true));

        await pageA.Screencast.StopAsync();
        await pageB.Screencast.StopAsync();
    }

    [UpstreamTest("MC-018 should stop tracing upon disconnect")]
    public async Task ShouldStopTracingUponDisconnect((IPage pageA, IPage pageB) twoPages, string trace)
    {
        Info.Skip(trace == "on");

        var (pageA, pageB) = twoPages;

        await pageA.Context.Tracing.StartAsync();
        var error = await Catch(() => pageB.Context.Tracing.StartAsync());
        Assert.That(error.Message, Does.Contain("Tracing has been already started"));

        await Disconnect(pageA);

        await pageB.Context.Tracing.StartAsync();
    }

    [UpstreamTest("MC-020 should run locator handlers after reconnecting with a pending callback")]
    public async Task ShouldRunLocatorHandlersAfterReconnectingWithAPendingCallback(ConnectFixture connect, PlaywrightServer remoteServer, Server server, BrowserNewPageOptions newPageOptions)
    {
        var browser = await connect(remoteServer.WsEndpoint());
        var page = await browser.NewPageAsync(newPageOptions);
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var called = 0;
        await page.AddLocatorHandlerAsync(page.GetByText("This interstitial covers the button"), async () =>
        {
            ++called;
            await new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;
        });
        await page.EvaluateAsync(@"() => {
    window.setupAnnoyingInterstitial('none', 1);
  }");
        var error = await Catch(() => page.Locator("#target").ClickAsync(new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
        Assert.That(called, Is.EqualTo(1));

        await Disconnect(page);

        var reconnectedBrowser = await connect(remoteServer.WsEndpoint());
        var reconnectedPage = reconnectedBrowser.Contexts[0].Pages[0];
        await reconnectedPage.AddLocatorHandlerAsync(reconnectedPage.GetByText("This interstitial covers the button"), async () =>
        {
            await reconnectedPage.Locator("#close").ClickAsync();
        });
        await reconnectedPage.Locator("#target").ClickAsync(new() { Timeout = 3000 });
        Assert.That(await reconnectedPage.EvaluateAsync<int>("window.clicked"), Is.EqualTo(1));
    }
}
