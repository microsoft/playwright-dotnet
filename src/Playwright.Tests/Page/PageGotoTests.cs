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

using System.Text;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-goto.spec.ts.</summary>
public class PageGotoTests
{
    [UpstreamTest("NA-001 should work @smoke")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("NA-002 should work with file URL")]
    public async Task ShouldWorkWithFileURL(IPage page, Func<string, string> asset, bool isAndroid, string mode, string channel)
    {
        It.Skip(isAndroid, "No files on Android");
        It.Skip(channel == "webkit-wsl", "separate filesystem on wsl");

        var fileurl = new Uri(asset("empty.html")).AbsoluteUri;
        await page.GotoAsync(fileurl);
        Assert.That(page.Url.ToLowerInvariant(), Is.EqualTo(fileurl.ToLowerInvariant()));
        Assert.That(page.Frames.Count, Is.EqualTo(1));
    }

    [UpstreamTest("NA-003 should navigate from file URL to about:blank")]
    public async Task ShouldNavigateFromFileURLToAboutBlank(IPage page, Func<string, string> asset, bool isAndroid, string channel)
    {
        It.Skip(isAndroid, "No files on Android");
        It.Skip(channel == "webkit-wsl", "separate filesystem on wsl");

        await page.GotoAsync(new Uri(asset("empty.html")).AbsoluteUri);
        await page.GotoAsync("about:blank");
    }

    [UpstreamTest("NA-004 should work with file URL with subframes")]
    public async Task ShouldWorkWithFileURLWithSubframes(IPage page, Func<string, string> asset, bool isAndroid, string mode, string channel)
    {
        It.Skip(isAndroid, "No files on Android");
        It.Skip(channel == "webkit-wsl", "separate filesystem on wsl");

        var fileurl = new Uri(asset("frames/two-frames.html")).AbsoluteUri;
        await page.GotoAsync(fileurl);
        Assert.That(page.Url.ToLowerInvariant(), Is.EqualTo(fileurl.ToLowerInvariant()));
        Assert.That(page.Frames.Count, Is.EqualTo(3));
    }

    [UpstreamTest("NA-005 should use http for no protocol")]
    public async Task ShouldUseHttpForNoProtocol(IPage page, Server server, bool isAndroid)
    {
        It.Skip(isAndroid);

        await page.GotoAsync(server.EmptyPage.Substring("http://".Length));
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("NA-006 should work cross-process")]
    public async Task ShouldWorkCrossProcess(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));

        var url = server.CrossProcessPrefix + "/empty.html";
        IFrame requestFrame = null;
        page.Request += (_, r) =>
        {
            if (r.Url == url)
            {
                requestFrame = r.Frame;
            }
        };
        var response = await page.GotoAsync(url);
        Assert.That(page.Url, Is.EqualTo(url));
        Assert.That(response.Frame, Is.SameAs(page.MainFrame));
        Assert.That(requestFrame, Is.SameAs(page.MainFrame));
        Assert.That(response.Url, Is.EqualTo(url));
    }

    [UpstreamTest("NA-007 should work with cross-process that fails before committing")]
    public async Task ShouldWorkWithCrossProcessThatFailsBeforeCommitting(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            req.Socket.Destroy();
        });
        var response1 = await page.GotoAsync(server.CrossProcessPrefix + "/title.html");
        await response1.FinishedAsync();
        var error = await Catch(() => page.GotoAsync(server.EmptyPage));
        Assert.That(error is Exception, Is.True);
    }

    [UpstreamTest("NA-008 should work with Cross-Origin-Opener-Policy")]
    public async Task ShouldWorkWithCrossOriginOpenerPolicy(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
            // Note: without 'onload', Firefox sometimes does not fire the load event
            // over the protocol. The reason is unclear.
            res.End(@"
      <div>Hello there!</div>
      <script>window.onload = () => console.log('onload')</script>
    ");
        });
        var requests = new HashSet<IRequest>();
        var events = new List<string>();
        page.Request += (_, r) =>
        {
            events.Add("request");
            requests.Add(r);
        };
        page.RequestFailed += (_, r) =>
        {
            events.Add("requestfailed");
            requests.Add(r);
        };
        page.RequestFinished += (_, r) =>
        {
            events.Add("requestfinished");
            requests.Add(r);
        };
        page.Response += (_, r) =>
        {
            events.Add("response");
            requests.Add(r.Request);
        };
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
        await response.FinishedAsync();
        Assert.That(events, Is.EqualTo(new[] { "request", "response", "requestfinished" }));
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(response.Request.Failure, Is.Null);
    }

    [UpstreamTest("NA-009 should work with Cross-Origin-Opener-Policy${crossOriginEmbedderPolicy ? ' and Cross-Origin-Embedder-Policy' : ''} and history state")]
    [Case(null)]
    [Case("require-corp")]
    public async Task ShouldWorkWithCrossOriginOpenerPolicyAndHistoryState(string crossOriginEmbedderPolicy, IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
            if (crossOriginEmbedderPolicy != null)
            {
                res.SetHeader("Cross-Origin-Embedder-Policy", crossOriginEmbedderPolicy);
            }
            res.End(@"
        <!doctype html>
        <h1>hello</h1>
        <script type=""module"">history.replaceState({ key: 1 }, '', location.href);</script>
      ");
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
        ExpectToEqual(await page.EvaluateAsync<object>("() => history.state"), new { key = 1 });
        await Expect(page.Locator("h1")).ToHaveTextAsync("hello");
    }

    [UpstreamTest("NA-010 should work with Cross-Origin-Opener-Policy and interception")]
    public async Task ShouldWorkWithCrossOriginOpenerPolicyAndInterception(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
            // Note: without 'onload', Firefox sometimes does not fire the load event
            // over the protocol. The reason is unclear.
            res.End(@"
      <div>Hello there!</div>
      <script>window.onload = () => console.log('onload')</script>
    ");
        });
        var requests = new HashSet<IRequest>();
        var events = new List<string>();
        page.Request += (_, r) =>
        {
            events.Add("request");
            requests.Add(r);
        };
        page.RequestFailed += (_, r) =>
        {
            events.Add("requestfailed");
            requests.Add(r);
        };
        page.RequestFinished += (_, r) =>
        {
            events.Add("requestfinished");
            requests.Add(r);
        };
        page.Response += (_, r) =>
        {
            events.Add("response");
            requests.Add(r.Request);
        };
        await page.RouteAsync("**/*", async route =>
        {
            await Task.Delay(100);
            await route.ContinueAsync();
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
        await response.FinishedAsync();
        Assert.That(events, Is.EqualTo(new[] { "request", "response", "requestfinished" }));
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(response.Request.Failure, Is.Null);
    }

    [UpstreamTest("NA-011 should work with Cross-Origin-Opener-Policy after redirect")]
    public async Task ShouldWorkWithCrossOriginOpenerPolicyAfterRedirect(IPage page, Server server)
    {
        server.SetRedirect("/redirect", "/empty.html");
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
            // Note: without 'onload', Firefox sometimes does not fire the load event
            // over the protocol. The reason is unclear.
            res.End(@"
      <div>Hello there!</div>
      <script>window.onload = () => console.log('onload')</script>
    ");
        });
        var requests = new HashSet<IRequest>();
        var events = new List<string>();
        page.Request += (_, r) =>
        {
            events.Add("request");
            requests.Add(r);
        };
        page.RequestFailed += (_, r) =>
        {
            events.Add("requestfailed");
            requests.Add(r);
        };
        page.RequestFinished += (_, r) =>
        {
            events.Add("requestfinished");
            requests.Add(r);
        };
        page.Response += (_, r) =>
        {
            events.Add("response");
            requests.Add(r.Request);
        };
        var response = await page.GotoAsync(server.Prefix + "/redirect");
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
        await response.FinishedAsync();
        Assert.That(events, Is.EqualTo(new[] { "request", "response", "requestfinished", "request", "response", "requestfinished" }));
        Assert.That(requests.Count, Is.EqualTo(2));
        Assert.That(response.Request.Failure, Is.Null);
        var firstRequest = response.Request.RedirectedFrom;
        Assert.That(firstRequest, Is.Not.Null);
        Assert.That(firstRequest.Url, Is.EqualTo(server.Prefix + "/redirect"));
    }

    [UpstreamTest("NA-012 should capture iframe navigation request")]
    public async Task ShouldCaptureIframeNavigationRequest(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));

        IFrame requestFrame = null;
        page.Request += (_, r) =>
        {
            if (r.Url == server.Prefix + "/frames/frame.html")
            {
                requestFrame = r.Frame;
            }
        };
        var response = await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/frames/one-frame.html"));
        Assert.That(response.Frame, Is.SameAs(page.MainFrame));
        Assert.That(response.Url, Is.EqualTo(server.Prefix + "/frames/one-frame.html"));

        Assert.That(page.Frames.Count, Is.EqualTo(2));
        Assert.That(requestFrame, Is.SameAs(page.Frames[1]));
    }

    [UpstreamTest("NA-013 should capture cross-process iframe navigation request")]
    public async Task ShouldCaptureCrossProcessIframeNavigationRequest(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));

        IFrame requestFrame = null;
        page.Request += (_, r) =>
        {
            if (r.Url == server.CrossProcessPrefix + "/frames/frame.html")
            {
                requestFrame = r.Frame;
            }
        };
        var response = await page.GotoAsync(server.CrossProcessPrefix + "/frames/one-frame.html");
        Assert.That(page.Url, Is.EqualTo(server.CrossProcessPrefix + "/frames/one-frame.html"));
        Assert.That(response.Frame, Is.SameAs(page.MainFrame));
        Assert.That(response.Url, Is.EqualTo(server.CrossProcessPrefix + "/frames/one-frame.html"));

        Assert.That(page.Frames.Count, Is.EqualTo(2));
        Assert.That(requestFrame, Is.SameAs(page.Frames[1]));
    }

    [UpstreamTest("NA-014 should work with anchor navigation")]
    public async Task ShouldWorkWithAnchorNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
        await page.GotoAsync(server.EmptyPage + "#foo");
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage + "#foo"));
        await page.GotoAsync(server.EmptyPage + "#bar");
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage + "#bar"));
    }

    [UpstreamTest("NA-015 should work with redirects")]
    public async Task ShouldWorkWithRedirects(IPage page, Server server)
    {
        server.SetRedirect("/redirect/1.html", "/redirect/2.html");
        server.SetRedirect("/redirect/2.html", "/empty.html");
        var response = await page.GotoAsync(server.Prefix + "/redirect/1.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("NA-016 should navigate to about:blank")]
    public async Task ShouldNavigateToAboutBlank(IPage page, Server server)
    {
        var response = await page.GotoAsync("about:blank");
        Assert.That(response, Is.Null);
    }

    [UpstreamTest("NA-017 should return response when page changes its URL after load")]
    public async Task ShouldReturnResponseWhenPageChangesItsURLAfterLoad(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.Prefix + "/historyapi.html");
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("NA-018 should work with subframes return 204")]
    public async Task ShouldWorkWithSubframesReturn204(IPage page, Server server)
    {
        server.SetRoute("/frames/frame.html", (req, res) =>
        {
            res.StatusCode = 204;
            res.End();
        });
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
    }

    [UpstreamTest("NA-019 should work with subframes return 204 with domcontentloaded")]
    public async Task ShouldWorkWithSubframesReturn204WithDomcontentloaded(IPage page, Server server)
    {
        server.SetRoute("/frames/frame.html", (req, res) =>
        {
            res.StatusCode = 204;
            res.End();
        });
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    [UpstreamTest("NA-020 should fail when server returns 204")]
    public async Task ShouldFailWhenServerReturns204(IPage page, Server server, string browserName, bool isLinux)
    {
        It.Fixme(browserName == "webkit" && isLinux, "Regressed in https://github.com/microsoft/playwright-browsers/pull/1297");
        // WebKit just loads an empty page.
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.StatusCode = 204;
            res.End();
        });
        Exception error = null;
        error = await Catch(() => page.GotoAsync(server.EmptyPage));
        Assert.That(error, Is.Not.Null);
        if (browserName == "chromium")
        {
            Assert.That(error.Message, Does.Contain("net::ERR_ABORTED"));
        }
        else if (browserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain("Aborted: 204 No Content"));
        }
        else
        {
            Assert.That(error.Message, Does.Contain("NS_BINDING_ABORTED"));
        }
    }

    [UpstreamTest("NA-021 should navigate to empty page with domcontentloaded")]
    public async Task ShouldNavigateToEmptyPageWithDomcontentloaded(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.EmptyPage, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("NA-022 should work when page calls history API in beforeunload")]
    public async Task ShouldWorkWhenPageCallsHistoryAPIInBeforeunload(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"() => {
    window.addEventListener('beforeunload', () => history.replaceState(null, 'initial', window.location.href), false);
  }");
        var response = await page.GotoAsync(server.Prefix + "/grid.html");
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("NA-023 should fail when navigating to bad url")]
    public async Task ShouldFailWhenNavigatingToBadUrl(IPage page, string browserName, bool isBidi)
    {
        Exception error = null;
        error = await Catch(() => page.GotoAsync("asdfasdf"));
        if (browserName == "chromium" && isBidi)
        {
            Assert.That(error.Message, Does.Contain("Invalid URL"));
        }
        else if (browserName == "firefox" && isBidi)
        {
            Assert.That(error.Message, Does.Contain("NS_ERROR_MALFORMED_URI"));
        }
        else if (browserName == "chromium" || browserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain("Cannot navigate to invalid URL"));
        }
        else
        {
            Assert.That(error.Message, Does.Contain("Invalid url"));
        }
    }

    [UpstreamTest("NA-024 should fail when navigating to bad SSL")]
    public async Task ShouldFailWhenNavigatingToBadSSL(IPage page, string browserName, Server httpsServer, string platform, string channel)
    {
        // Make sure that network events do not emit 'undefined'.
        // @see https://crbug.com/750469
        page.Request += (_, request) => Assert.That(request, Is.Not.Null);
        page.RequestFinished += (_, request) => Assert.That(request, Is.Not.Null);
        page.RequestFailed += (_, request) => Assert.That(request, Is.Not.Null);
        Exception error = null;
        error = await Catch(() => page.GotoAsync(httpsServer.EmptyPage));
        Assert.That(error.Message, Does.Match(ExpectedSSLError(browserName, platform, channel)));
    }

    [UpstreamTest("NA-025 should fail when navigating to bad SSL after redirects")]
    public async Task ShouldFailWhenNavigatingToBadSSLAfterRedirects(IPage page, string browserName, Server server, Server httpsServer, string platform, string channel)
    {
        server.SetRedirect("/redirect/1.html", "/redirect/2.html");
        server.SetRedirect("/redirect/2.html", "/empty.html");
        Exception error = null;
        error = await Catch(() => page.GotoAsync(httpsServer.Prefix + "/redirect/1.html"));
        Assert.That(error.Message, Does.Match(ExpectedSSLError(browserName, platform, channel)));
    }

    [UpstreamTest("NA-026 should not crash when navigating to bad SSL after a cross origin navigation")]
    public async Task ShouldNotCrashWhenNavigatingToBadSSLAfterACrossOriginNavigation(IPage page, Server server, Server httpsServer)
    {
        await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        await Catch(() => page.GotoAsync(httpsServer.EmptyPage));
    }

    [UpstreamTest("NA-029 should fail when main resources failed to load")]
    public async Task ShouldFailWhenMainResourcesFailedToLoad(IPage page, string browserName, bool isWindows, string mode, string channel)
    {
        It.Skip(channel == "webkit-wsl", "Networking mode mirrored ends up stalling connections rather than terminating them, see https://github.com/microsoft/WSL/issues/10855.");
        Exception error = null;
        error = await Catch(() => page.GotoAsync("http://localhost:44123/non-existing-url"));
        if (browserName == "chromium")
        {
            Assert.That(error.Message, Does.Contain("net::ERR_CONNECTION_REFUSED"));
        }
        else if (browserName == "webkit" && isWindows && channel != "webkit-wsl")
        {
            Assert.That(error.Message, Does.Contain("Could not connect to server"));
        }
        else if (browserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain("Could not connect"));
        }
        else
        {
            Assert.That(error.Message, Does.Contain("NS_ERROR_CONNECTION_REFUSED"));
        }
    }

    [UpstreamTest("NA-030 should fail when exceeding maximum navigation timeout")]
    [Fixme("dotnet: the message is 'Timeout 1ms exceeded.', without the 'page.goto: ' prefix of the call")]
    public async Task ShouldFailWhenExceedingMaximumNavigationTimeout(IPage page, Server server, IPlaywright playwright)
    {
        // Hang for request to the empty.html
        server.SetRoute("/empty.html", (req, res) => { });
        Exception error = null;
        error = await Catch(() => page.GotoAsync(server.Prefix + "/empty.html", new() { Timeout = 1 }));
        Assert.That(error.Message, Does.Contain("page.goto: Timeout 1ms exceeded."));
        Assert.That(error.Message, Does.Contain(server.Prefix + "/empty.html"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("NA-031 should fail when exceeding default maximum navigation timeout")]
    [Fixme("dotnet: the message is 'Timeout 1ms exceeded.', without the 'page.goto: ' prefix of the call")]
    public async Task ShouldFailWhenExceedingDefaultMaximumNavigationTimeout(IPage page, Server server, IPlaywright playwright, bool isAndroid, bool isElectron)
    {
        It.Skip(isAndroid || isElectron, "No context per test");

        // Hang for request to the empty.html
        server.SetRoute("/empty.html", (req, res) => { });
        Exception error = null;
        page.Context.SetDefaultNavigationTimeout(2);
        page.SetDefaultNavigationTimeout(1);
        error = await Catch(() => page.GotoAsync(server.Prefix + "/empty.html"));
        Assert.That(error.Message, Does.Contain("page.goto: Timeout 1ms exceeded."));
        Assert.That(error.Message, Does.Contain(server.Prefix + "/empty.html"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("NA-032 should fail when exceeding browser context navigation timeout")]
    [Fixme("dotnet: the message is 'Timeout 2ms exceeded.', without the 'page.goto: ' prefix of the call")]
    public async Task ShouldFailWhenExceedingBrowserContextNavigationTimeout(IPage page, Server server, IPlaywright playwright, bool isAndroid, bool isElectron)
    {
        It.Skip(isAndroid || isElectron, "No context per test");

        // Hang for request to the empty.html
        server.SetRoute("/empty.html", (req, res) => { });
        Exception error = null;
        page.Context.SetDefaultNavigationTimeout(2);
        error = await Catch(() => page.GotoAsync(server.Prefix + "/empty.html"));
        Assert.That(error.Message, Does.Contain("page.goto: Timeout 2ms exceeded."));
        Assert.That(error.Message, Does.Contain(server.Prefix + "/empty.html"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("NA-035 should prioritize default navigation timeout over default timeout")]
    [Fixme("dotnet: the message is 'Timeout 1ms exceeded.', without the 'page.goto: ' prefix of the call")]
    public async Task ShouldPrioritizeDefaultNavigationTimeoutOverDefaultTimeout(IPage page, Server server, IPlaywright playwright)
    {
        // Hang for request to the empty.html
        server.SetRoute("/empty.html", (req, res) => { });
        Exception error = null;
        page.SetDefaultTimeout(0);
        page.SetDefaultNavigationTimeout(1);
        error = await Catch(() => page.GotoAsync(server.Prefix + "/empty.html"));
        Assert.That(error.Message, Does.Contain("page.goto: Timeout 1ms exceeded."));
        Assert.That(error.Message, Does.Contain(server.Prefix + "/empty.html"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("NA-036 should disable timeout when its set to 0")]
    public async Task ShouldDisableTimeoutWhenItsSetTo0(IPage page, Server server)
    {
        Exception error = null;
        var loaded = false;
        EventHandler<IPage> handler = null;
        handler = (_, _) =>
        {
            page.Load -= handler;
            loaded = true;
        };
        page.Load += handler;
        error = await Catch(() => page.GotoAsync(server.Prefix + "/grid.html", new() { Timeout = 0, WaitUntil = WaitUntilState.Load }));
        Assert.That(error, Is.Null);
        Assert.That(loaded, Is.EqualTo(true));
    }

    [UpstreamTest("NA-037 should fail when replaced by another navigation")]
    [Fixme("dotnet: the message is 'Navigation to \"http://localhost:<port>/empty.html\" is interrupted by another navigation to \"http://localhost:<port>/one-style.html\"', without the 'page.goto: ' prefix of the call", Browsers = "firefox,webkit")]
    public async Task ShouldFailWhenReplacedByAnotherNavigation(IPage page, Server server, string browserName, bool isBidi)
    {
        Task<IResponse> anotherPromise = null;
        server.SetRoute("/empty.html", (req, res) =>
        {
            anotherPromise = page.GotoAsync(server.Prefix + "/one-style.html");
            // Hang request to empty.html.
        });
        var error = await Catch(() => page.GotoAsync(server.Prefix + "/empty.html"));
        await anotherPromise;
        if (browserName == "chromium")
        {
            Assert.That(error.Message, Does.Contain("net::ERR_ABORTED"));
        }
        else if (browserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain($"page.goto: Navigation to \"{server.Prefix + "/empty.html"}\" is interrupted by another navigation to \"{server.Prefix + "/one-style.html"}\""));
        }
        else if (browserName == "firefox")
        {
            if (isBidi)
            {
                Assert.That(error.Message, Does.Contain("page.goto: Protocol error (browsingContext.navigate): unknown error"));
            }
            else
            {
                // Firefox might yield either NS_BINDING_ABORTED or 'navigation interrupted by another one'
                Assert.That(error.Message.Contains($"page.goto: Navigation to \"{server.Prefix + "/empty.html"}\" is interrupted by another navigation to \"{server.Prefix + "/one-style.html"}\"", StringComparison.Ordinal) || error.Message.Contains("NS_BINDING_ABORTED", StringComparison.Ordinal), Is.EqualTo(true));
            }
        }
    }

    [UpstreamTest("NA-038 js redirect overrides url bar navigation ")]
    [Fixme("dotnet: the message is 'Navigation to \"http://localhost:<port>/b\" is interrupted by another navigation to \"http://localhost:<port>/c\"', without the 'page.goto: ' prefix of the call", Browsers = "webkit")]
    public async Task JsRedirectOverridesUrlBarNavigation(IPage page, Server server, string browserName, string trace)
    {
        It.Skip(trace == "on", "tracing waits for snapshot that never arrives because pending navigation");

        server.SetRoute("/a", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(@"
        <body>
          <script>
            setTimeout(() => {
              window.location.pathname = '/c';
            }, 1000);
          </script>
        </body>
      ");
        });
        var events = new List<string>();
        server.SetRoute("/b", async (req, res) =>
        {
            events.Add("started b");
            await Task.Delay(2000);
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End("BBB");
            events.Add("finished b");
        });
        server.SetRoute("/c", async (req, res) =>
        {
            events.Add("started c");
            await Task.Delay(2000);
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End("CCC");
            events.Add("finished c");
        });
        await page.GotoAsync(server.Prefix + "/a");
        var error = await Catch(() => page.GotoAsync(server.Prefix + "/b"));
        var expectEvents = (browserName == "chromium") ?
            new[] { "started b", "finished b" } :
            new[] { "started b", "started c", "finished b", "finished c" };
        await Expect(() => Assert.That(events, Is.EqualTo(expectEvents))).ToPassAsync();
        Assert.That(events, Is.EqualTo(expectEvents));
        if (browserName == "chromium")
        {
            // Chromium prioritizes the url bar navigation over the js redirect.
            Assert.That(error, Is.Null);
            await Expect(page).ToHaveURLAsync(server.Prefix + "/b");
        }
        else if (browserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain($"page.goto: Navigation to \"{server.Prefix + "/b"}\" is interrupted by another navigation to \"{server.Prefix + "/c"}\""));
            await Expect(page).ToHaveURLAsync(server.Prefix + "/c");
        }
        else if (browserName == "firefox")
        {
            Assert.That(error.Message, Does.Contain("NS_BINDING_ABORTED"));
            await Expect(page).ToHaveURLAsync(server.Prefix + "/c");
        }
    }

    [UpstreamTest("NA-039 should succeed on url bar navigation when there is pending navigation")]
    public async Task ShouldSucceedOnUrlBarNavigationWhenThereIsPendingNavigation(IPage page, Server server, string browserName)
    {
        It.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
        server.SetRoute("/a", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(@"
      <body>
        <script>
          setTimeout(() => {
            window.location.pathname = '/c';
          }, 10);
        </script>
      </body>
    ");
        });
        var events = new List<string>();
        server.SetRoute("/b", async (req, res) =>
        {
            events.Add("started b");
            await Task.Delay(2000);
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End("BBB");
            events.Add("finished b");
        });
        server.SetRoute("/c", async (req, res) =>
        {
            events.Add("started c");
            await Task.Delay(2000);
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End("CCC");
            events.Add("finished c");
        });
        await page.GotoAsync(server.Prefix + "/a");
        await page.WaitForTimeoutAsync(1000);
        var error = await Catch(() => page.GotoAsync(server.Prefix + "/b"));
        var expectEvents = new[] { "started c", "started b", "finished c", "finished b" };
        await Expect(() => Assert.That(events, Is.EqualTo(expectEvents))).ToPassAsync(new() { Timeout = 5000 });
        Assert.That(events, Is.EqualTo(expectEvents));
        Assert.That(error, Is.Null);
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/b"));
    }

    [UpstreamTest("NA-040 should work when navigating to valid url")]
    public async Task ShouldWorkWhenNavigatingToValidUrl(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    [UpstreamTest("NA-041 should work when navigating to data url")]
    public async Task ShouldWorkWhenNavigatingToDataUrl(IPage page, Server server)
    {
        var response = await page.GotoAsync("data:text/html,hello");
        Assert.That(response, Is.Null);
    }

    [UpstreamTest("NA-042 should work when navigating to 404")]
    public async Task ShouldWorkWhenNavigatingTo404(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.Prefix + "/not-found");
        Assert.That(response.Ok, Is.EqualTo(false));
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [UpstreamTest("NA-043 should return last response in redirect chain")]
    public async Task ShouldReturnLastResponseInRedirectChain(IPage page, Server server)
    {
        server.SetRedirect("/redirect/1.html", "/redirect/2.html");
        server.SetRedirect("/redirect/2.html", "/redirect/3.html");
        server.SetRedirect("/redirect/3.html", server.EmptyPage);
        var response = await page.GotoAsync(server.Prefix + "/redirect/1.html");
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("NA-047 should navigate to dataURL and not fire dataURL requests")]
    public async Task ShouldNavigateToDataURLAndNotFireDataURLRequests(IPage page, Server server)
    {
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        var dataURL = "data:text/html,<div>yo</div>";
        var response = await page.GotoAsync(dataURL);
        Assert.That(response, Is.Null);
        Assert.That(requests.Count, Is.EqualTo(0));
    }

    [UpstreamTest("NA-048 should navigate to URL with hash and fire requests without hash")]
    public async Task ShouldNavigateToURLWithHashAndFireRequestsWithoutHash(IPage page, Server server)
    {
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        var response = await page.GotoAsync(server.EmptyPage + "#hash");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.Url, Is.EqualTo(server.EmptyPage));
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(requests[0].Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("NA-049 should work with self requesting page")]
    public async Task ShouldWorkWithSelfRequestingPage(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.Prefix + "/self-request.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.Url, Does.Contain("self-request.html"));
    }

    [UpstreamTest("NA-050 should fail when navigating and show the url at the error message")]
    public async Task ShouldFailWhenNavigatingAndShowTheUrlAtTheErrorMessage(IPage page, Server server, Server httpsServer)
    {
        var url = httpsServer.Prefix + "/redirect/1.html";
        Exception error = null;
        try
        {
            await page.GotoAsync(url);
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error.Message, Does.Contain(url));
    }

    [UpstreamTest("NA-051 should be able to navigate to a page controlled by service worker")]
    public async Task ShouldBeAbleToNavigateToAPageControlledByServiceWorker(IPage page, Server server, bool isElectron, bool isAndroid)
    {
        It.Skip(isElectron);
        It.Skip(isAndroid, "Service workers need a secure context, but the Android test server is reached over http://10.0.2.2");

        const string transaction = @"async function transaction(value) {
    const db = await new Promise((resolve, reject) => {
      const request = indexedDB.open('roundtrip', 1);
      request.onupgradeneeded = () => request.result.createObjectStore('items');
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
    return new Promise((resolve, reject) => {
      const tx = db.transaction('items', 'readwrite');
      const request = tx.objectStore('items').get('name');
      request.onsuccess = () => {
        if (value)
          tx.objectStore('items').put(value, 'name');
      };
      tx.oncomplete = () => resolve(request.result);
      tx.onerror = tx.onabort = () => reject(tx.error);
    });
  }";

        await page.GotoAsync(server.Prefix + "/serviceworkers/fetch/sw.html");
        await page.EvaluateAsync("() => window['activationPromise']");
        await page.GotoAsync(server.Prefix + "/serviceworkers/fetch/sw.html");
        await page.WaitForFunctionAsync("() => !!navigator.serviceWorker.controller");
        await page.EvaluateAsync(transaction, "value");
        await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        await page.GoBackAsync();
        await page.WaitForFunctionAsync("() => !!navigator.serviceWorker.controller");
        Assert.That(await page.EvaluateAsync<string>(transaction, null), Is.EqualTo("value"));
    }

    [UpstreamTest("NA-052 should send referer")]
    public async Task ShouldSendReferer(IPage page, Server server)
    {
        var (request1, request2, _) = await PromiseAll(
            server.WaitForRequest("/grid.html"),
            server.WaitForRequest("/digits/1.png"),
            page.GotoAsync(server.Prefix + "/grid.html", new()
            {
                Referer = "http://google.com/",
            }));
        Assert.That(request1.Headers["referer"], Is.EqualTo("http://google.com/"));
        // Make sure subresources do not inherit referer.
        Assert.That(request2.Headers["referer"], Is.EqualTo(server.Prefix + "/grid.html"));
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/grid.html"));
    }

    [UpstreamTest("NA-053 should send referer of cross-origin URL")]
    public async Task ShouldSendRefererOfCrossOriginURL(IPage page, Server server)
    {
        var (request1, request2, _) = await PromiseAll(
            server.WaitForRequest("/grid.html"),
            server.WaitForRequest("/digits/1.png"),
            page.GotoAsync(server.Prefix + "/grid.html", new()
            {
                Referer = "https://microsoft.com/xbox/"
            }));
        Assert.That(request1.Headers["referer"], Is.EqualTo("https://microsoft.com/xbox/"));
        // Make sure subresources do not inherit referer.
        Assert.That(request2.Headers["referer"], Is.EqualTo(server.Prefix + "/grid.html"));
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/grid.html"));
    }

    [UpstreamTest("NA-054 should reject referer option when setExtraHTTPHeaders provides referer")]
    public async Task ShouldRejectRefererOptionWhenSetExtraHTTPHeadersProvidesReferer(IPage page, Server server)
    {
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = "http://microsoft.com/" });
        Exception error = null;
        error = await Catch(() => page.GotoAsync(server.Prefix + "/grid.html", new()
        {
            Referer = "http://google.com/",
        }));
        Assert.That(error.Message, Does.Contain("\"referer\" is already specified as extra HTTP header"));
        Assert.That(error.Message, Does.Contain(server.Prefix + "/grid.html"));
    }

    [UpstreamTest("NA-055 should override referrer-policy")]
    public async Task ShouldOverrideReferrerPolicy(IPage page, Server server)
    {
        server.SetRoute("/grid.html", (req, res) =>
        {
            res.SetHeader("Referrer-Policy", "no-referrer");
            server.ServeFile(req, res);
        });
        var (request1, request2, _) = await PromiseAll(
            server.WaitForRequest("/grid.html"),
            server.WaitForRequest("/digits/1.png"),
            page.GotoAsync(server.Prefix + "/grid.html", new()
            {
                Referer = "http://microsoft.com/",
            }));
        Assert.That(request1.Headers["referer"], Is.EqualTo("http://microsoft.com/"));
        // Make sure subresources do not inherit referer.
        Assert.That(request2.Headers["referer"], Is.Null);
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/grid.html"));
    }

    [UpstreamTest("NA-056 should fail when canceled by another navigation")]
    public async Task ShouldFailWhenCanceledByAnotherNavigation(IPage page, Server server)
    {
        server.SetRoute("/one-style.html", (req, res) => { });
        var failed = Catch(() => page.GotoAsync(server.Prefix + "/one-style.html"));
        await server.WaitForRequest("/one-style.html");
        await page.GotoAsync(server.Prefix + "/empty.html");
        var error = await failed;
        Assert.That(error.Message, Is.Not.Empty);
    }

    [UpstreamTest("NA-057 should work with lazy loading iframes")]
    public async Task ShouldWorkWithLazyLoadingIframes(IPage page, Server server, bool isAndroid)
    {
        It.Fixme(isAndroid);

        await page.GotoAsync(server.Prefix + "/frames/lazy-frame.html");
        Assert.That(page.Frames.Count, Is.EqualTo(2));
    }

    [UpstreamTest("NA-058 should report raw buffer for main resource")]
    public async Task ShouldReportRawBufferForMainResource(IPage page, Server server, string browserName, string platform, string channel)
    {
        It.Fixme(browserName == "chromium", "Chromium sends main resource as text");
        It.Fixme(browserName == "webkit" && platform == "win32" && channel != "webkit-wsl", "Same here");

        server.SetRoute("/empty.html", (req, res) =>
        {
            res.StatusCode = 200;
            res.End(Encoding.UTF8.GetBytes("Ü (lowercase ü)"));
        });
        var response = await page.GotoAsync(server.Prefix + "/empty.html");
        var body = await response.BodyAsync();
        Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo("Ü (lowercase ü)"));
    }

    [UpstreamTest("NA-059 should not throw unhandled rejections on invalid url")]
    public async Task ShouldNotThrowUnhandledRejectionsOnInvalidUrl(IPage page, Server server)
    {
        var e = await Catch(() => page.GotoAsync("https://www.youtube Panel Title.com/"));
        Assert.That(e.ToString(), Does.Contain("Panel Title"));
    }

    [UpstreamTest("NA-060 should not crash when RTCPeerConnection is used")]
    public async Task ShouldNotCrashWhenRTCPeerConnectionIsUsed(IPage page, Server server, string browserName, string platform)
    {
        server.SetRoute("/rtc.html", (_, res) =>
        {
            res.End(@"
      <!DOCTYPE html>
      <html>
        <body>
          <script>
            window.RTCPeerConnection && new window.RTCPeerConnection({
              iceServers: []
            });
          </script>
        </body>
      </html>
");
        });
        await page.GotoAsync(server.Prefix + "/rtc.html");
        await page.EvaluateAsync(@"() => {
    // RTCPeerConnection is not present on WebKit Linux
    window.RTCPeerConnection && new window.RTCPeerConnection({
      iceServers: []
    });
  }");
    }

    [UpstreamTest("NA-061 should properly wait for load")]
    public async Task ShouldProperlyWaitForLoad(IPage page, Server server, string browserName)
    {
        server.SetRoute("/slow.js", async (req, res) =>
        {
            await Task.Delay(100);
            res.WriteHead(200, new() { ["Content-Type"] = "application/javascript" });
            res.End("window.results.push('slow module');export const foo = 'slow';");
        });
        await page.GotoAsync(server.Prefix + "/load-event/load-event.html");
        var results = await page.EvaluateAsync<string[]>("window.results");
        Assert.That(results, Is.EqualTo(new[]
        {
            "script tag after after module",
            "slow module",
            "module",
            "DOMContentLoaded",
            "load"
        }));
    }

    [UpstreamTest("NA-062 should not resolve goto upon window.stop()")]
    public async Task ShouldNotResolveGotoUponWindowStop(string browserName, IPage page, Server server, bool isBidi)
    {
        It.Fixme(browserName == "firefox" && !isBidi, "load/domcontentloaded events are flaky");
        It.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");

        ServerResponse response = null;
        server.SetRoute("/module.js", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/javascript" });
            response = res;
        });
        var done = false;
        async Task Goto()
        {
            await page.GotoAsync(server.Prefix + "/window-stop.html");
            done = true;
        }
        IgnoreErrors(Goto());
        await server.WaitForRequest("/module.js");
        Assert.That(done, Is.EqualTo(false));
        await page.WaitForTimeoutAsync(1000);  // give it some time to erroneously resolve
        response.End("");
        await page.WaitForTimeoutAsync(1000);  // give it more time to erroneously resolve
        Assert.That(done, Is.EqualTo(false));
    }

    [UpstreamTest("NA-063 should return from goto if new navigation is started")]
    public async Task ShouldReturnFromGotoIfNewNavigationIsStarted(IPage page, Server server, string browserName, bool isAndroid)
    {
        It.Fixme(isAndroid);
        server.SetRoute("/slow.js", (req, res) => { });
        var finished = false;
        async Task<IResponse> Navigation()
        {
            var r = await page.GotoAsync(server.Prefix + "/load-event/load-event.html");
            finished = true;
            return r;
        }
        var navigation = Navigation();
        await Task.Delay(500);
        Assert.That(finished, Is.False);
        await page.GotoAsync(server.EmptyPage);
        Assert.That((await navigation).Status, Is.EqualTo(200));
    }

    [UpstreamTest("NA-064 should return when navigation is committed if commit is specified")]
    public async Task ShouldReturnWhenNavigationIsCommittedIfCommitIsSpecified(IPage page, Server server)
    {
        server.SetRoute("/script.js", (req, res) => { });
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<title>Hello</title><script src=\"script.js\"></script>");
        });
        var response = await page.GotoAsync(server.EmptyPage, new() { WaitUntil = WaitUntilState.Commit });
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Hello"));
    }

    [UpstreamTest("NA-065 should wait for load when iframe attaches and detaches")]
    public async Task ShouldWaitForLoadWhenIframeAttachesAndDetaches(IPage page, Server server)
    {
        It.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(@"
      <body>
        <script>
          const iframe = document.createElement('iframe');
          iframe.src = './iframe.html';
          document.body.appendChild(iframe);
          setTimeout(() => iframe.remove(), 1000);
        </script>
      </body>
    ");
        });

        server.SetRoute("/iframe.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(@"
      <link rel=""stylesheet"" href=""./style2.css"">
    ");
        });

        // Stall the css so that 'load' does not fire.
        server.SetRoute("/style2.css", (req, res) => { });

        var frameDetached = Once<IFrame>(h => page.FrameDetached += h, h => page.FrameDetached -= h);
        var done = page.GotoAsync(server.EmptyPage, new() { WaitUntil = WaitUntilState.Load });
        await frameDetached; // Make sure that iframe is gone.
        await done;
        Assert.That(await page.QuerySelectorAsync("iframe"), Is.Null);
    }

    [UpstreamTest("NA-066 should return url with basic auth info")]
    public async Task ShouldReturnUrlWithBasicAuthInfo(IPage page, Server server, string loopback)
    {
        var url = $"http://admin:admin@{loopback ?? "localhost"}:{server.Port}/empty.html";
        await page.GotoAsync(url);
        Assert.That(page.Url, Is.EqualTo(url));
    }
}
