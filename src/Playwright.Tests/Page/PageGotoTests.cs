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
public class PageGotoTests : UpstreamPageTest
{
    [UpstreamTest("NA-001 should work @smoke")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("NA-002 should work with file URL")]
    public async Task ShouldWorkWithFileURL()
    {
        Info.Skip(IsAndroid, "No files on Android");
        Info.Skip(Channel == "webkit-wsl", "separate filesystem on wsl");

        var fileurl = new Uri(Asset("empty.html")).AbsoluteUri;
        await Page.GotoAsync(fileurl);
        Assert.That(Page.Url.ToLowerInvariant(), Is.EqualTo(fileurl.ToLowerInvariant()));
        Assert.That(Page.Frames.Count, Is.EqualTo(1));
    }

    [UpstreamTest("NA-003 should navigate from file URL to about:blank")]
    public async Task ShouldNavigateFromFileURLToAboutBlank()
    {
        Info.Skip(IsAndroid, "No files on Android");
        Info.Skip(Channel == "webkit-wsl", "separate filesystem on wsl");

        await Page.GotoAsync(new Uri(Asset("empty.html")).AbsoluteUri);
        await Page.GotoAsync("about:blank");
    }

    [UpstreamTest("NA-004 should work with file URL with subframes")]
    public async Task ShouldWorkWithFileURLWithSubframes()
    {
        Info.Skip(IsAndroid, "No files on Android");
        Info.Skip(Channel == "webkit-wsl", "separate filesystem on wsl");

        var fileurl = new Uri(Asset("frames/two-frames.html")).AbsoluteUri;
        await Page.GotoAsync(fileurl);
        Assert.That(Page.Url.ToLowerInvariant(), Is.EqualTo(fileurl.ToLowerInvariant()));
        Assert.That(Page.Frames.Count, Is.EqualTo(3));
    }

    [UpstreamTest("NA-005 should use http for no protocol")]
    public async Task ShouldUseHttpForNoProtocol()
    {
        Info.Skip(IsAndroid);

        await Page.GotoAsync(Server.EmptyPage.Substring("http://".Length));
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("NA-006 should work cross-process")]
    public async Task ShouldWorkCrossProcess()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));

        var url = Server.CrossProcessPrefix + "/empty.html";
        IFrame requestFrame = null;
        Page.Request += (_, r) =>
        {
            if (r.Url == url)
            {
                requestFrame = r.Frame;
            }
        };
        var response = await Page.GotoAsync(url);
        Assert.That(Page.Url, Is.EqualTo(url));
        Assert.That(response.Frame, Is.SameAs(Page.MainFrame));
        Assert.That(requestFrame, Is.SameAs(Page.MainFrame));
        Assert.That(response.Url, Is.EqualTo(url));
    }

    [UpstreamTest("NA-007 should work with cross-process that fails before committing")]
    public async Task ShouldWorkWithCrossProcessThatFailsBeforeCommitting()
    {
        Server.SetRoute("/empty.html", (req, res) =>
        {
            req.Socket.Destroy();
        });
        var response1 = await Page.GotoAsync(Server.CrossProcessPrefix + "/title.html");
        await response1.FinishedAsync();
        var error = await Catch(() => Page.GotoAsync(Server.EmptyPage));
        Assert.That(error is Exception, Is.True);
    }

    [UpstreamTest("NA-008 should work with Cross-Origin-Opener-Policy")]
    public async Task ShouldWorkWithCrossOriginOpenerPolicy()
    {
        Server.SetRoute("/empty.html", (req, res) =>
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
        Page.Request += (_, r) =>
        {
            events.Add("request");
            requests.Add(r);
        };
        Page.RequestFailed += (_, r) =>
        {
            events.Add("requestfailed");
            requests.Add(r);
        };
        Page.RequestFinished += (_, r) =>
        {
            events.Add("requestfinished");
            requests.Add(r);
        };
        Page.Response += (_, r) =>
        {
            events.Add("response");
            requests.Add(r.Request);
        };
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
        await response.FinishedAsync();
        Assert.That(events, Is.EqualTo(new[] { "request", "response", "requestfinished" }));
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(response.Request.Failure, Is.Null);
    }

    [UpstreamTest("NA-009 should work with Cross-Origin-Opener-Policy${crossOriginEmbedderPolicy ? ' and Cross-Origin-Embedder-Policy' : ''} and history state")]
    [TestCase(null)]
    [TestCase("require-corp")]
    public async Task ShouldWorkWithCrossOriginOpenerPolicyAndHistoryState(string crossOriginEmbedderPolicy)
    {
        Server.SetRoute("/empty.html", (req, res) =>
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
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
        ExpectToEqual(await Page.EvaluateAsync<object>("() => history.state"), new { key = 1 });
        await Expect(Page.Locator("h1")).ToHaveTextAsync("hello");
    }

    [UpstreamTest("NA-010 should work with Cross-Origin-Opener-Policy and interception")]
    public async Task ShouldWorkWithCrossOriginOpenerPolicyAndInterception()
    {
        Server.SetRoute("/empty.html", (req, res) =>
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
        Page.Request += (_, r) =>
        {
            events.Add("request");
            requests.Add(r);
        };
        Page.RequestFailed += (_, r) =>
        {
            events.Add("requestfailed");
            requests.Add(r);
        };
        Page.RequestFinished += (_, r) =>
        {
            events.Add("requestfinished");
            requests.Add(r);
        };
        Page.Response += (_, r) =>
        {
            events.Add("response");
            requests.Add(r.Request);
        };
        await Page.RouteAsync("**/*", async route =>
        {
            await Task.Delay(100);
            await route.ContinueAsync();
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
        await response.FinishedAsync();
        Assert.That(events, Is.EqualTo(new[] { "request", "response", "requestfinished" }));
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(response.Request.Failure, Is.Null);
    }

    [UpstreamTest("NA-011 should work with Cross-Origin-Opener-Policy after redirect")]
    public async Task ShouldWorkWithCrossOriginOpenerPolicyAfterRedirect()
    {
        Server.SetRedirect("/redirect", "/empty.html");
        Server.SetRoute("/empty.html", (req, res) =>
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
        Page.Request += (_, r) =>
        {
            events.Add("request");
            requests.Add(r);
        };
        Page.RequestFailed += (_, r) =>
        {
            events.Add("requestfailed");
            requests.Add(r);
        };
        Page.RequestFinished += (_, r) =>
        {
            events.Add("requestfinished");
            requests.Add(r);
        };
        Page.Response += (_, r) =>
        {
            events.Add("response");
            requests.Add(r.Request);
        };
        var response = await Page.GotoAsync(Server.Prefix + "/redirect");
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
        await response.FinishedAsync();
        Assert.That(events, Is.EqualTo(new[] { "request", "response", "requestfinished", "request", "response", "requestfinished" }));
        Assert.That(requests.Count, Is.EqualTo(2));
        Assert.That(response.Request.Failure, Is.Null);
        var firstRequest = response.Request.RedirectedFrom;
        Assert.That(firstRequest, Is.Not.Null);
        Assert.That(firstRequest.Url, Is.EqualTo(Server.Prefix + "/redirect"));
    }

    [UpstreamTest("NA-012 should capture iframe navigation request")]
    public async Task ShouldCaptureIframeNavigationRequest()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));

        IFrame requestFrame = null;
        Page.Request += (_, r) =>
        {
            if (r.Url == Server.Prefix + "/frames/frame.html")
            {
                requestFrame = r.Frame;
            }
        };
        var response = await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/frames/one-frame.html"));
        Assert.That(response.Frame, Is.SameAs(Page.MainFrame));
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/frames/one-frame.html"));

        Assert.That(Page.Frames.Count, Is.EqualTo(2));
        Assert.That(requestFrame, Is.SameAs(Page.Frames[1]));
    }

    [UpstreamTest("NA-013 should capture cross-process iframe navigation request")]
    public async Task ShouldCaptureCrossProcessIframeNavigationRequest()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));

        IFrame requestFrame = null;
        Page.Request += (_, r) =>
        {
            if (r.Url == Server.CrossProcessPrefix + "/frames/frame.html")
            {
                requestFrame = r.Frame;
            }
        };
        var response = await Page.GotoAsync(Server.CrossProcessPrefix + "/frames/one-frame.html");
        Assert.That(Page.Url, Is.EqualTo(Server.CrossProcessPrefix + "/frames/one-frame.html"));
        Assert.That(response.Frame, Is.SameAs(Page.MainFrame));
        Assert.That(response.Url, Is.EqualTo(Server.CrossProcessPrefix + "/frames/one-frame.html"));

        Assert.That(Page.Frames.Count, Is.EqualTo(2));
        Assert.That(requestFrame, Is.SameAs(Page.Frames[1]));
    }

    [UpstreamTest("NA-014 should work with anchor navigation")]
    public async Task ShouldWorkWithAnchorNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
        await Page.GotoAsync(Server.EmptyPage + "#foo");
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage + "#foo"));
        await Page.GotoAsync(Server.EmptyPage + "#bar");
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage + "#bar"));
    }

    [UpstreamTest("NA-015 should work with redirects")]
    public async Task ShouldWorkWithRedirects()
    {
        Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
        Server.SetRedirect("/redirect/2.html", "/empty.html");
        var response = await Page.GotoAsync(Server.Prefix + "/redirect/1.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("NA-016 should navigate to about:blank")]
    public async Task ShouldNavigateToAboutBlank()
    {
        var response = await Page.GotoAsync("about:blank");
        Assert.That(response, Is.Null);
    }

    [UpstreamTest("NA-017 should return response when page changes its URL after load")]
    public async Task ShouldReturnResponseWhenPageChangesItsURLAfterLoad()
    {
        var response = await Page.GotoAsync(Server.Prefix + "/historyapi.html");
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("NA-018 should work with subframes return 204")]
    public async Task ShouldWorkWithSubframesReturn204()
    {
        Server.SetRoute("/frames/frame.html", (req, res) =>
        {
            res.StatusCode = 204;
            res.End();
        });
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
    }

    [UpstreamTest("NA-019 should work with subframes return 204 with domcontentloaded")]
    public async Task ShouldWorkWithSubframesReturn204WithDomcontentloaded()
    {
        Server.SetRoute("/frames/frame.html", (req, res) =>
        {
            res.StatusCode = 204;
            res.End();
        });
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    [UpstreamTest("NA-020 should fail when server returns 204")]
    public async Task ShouldFailWhenServerReturns204()
    {
        Info.Fixme(BrowserName == "webkit" && IsLinux, "Regressed in https://github.com/microsoft/playwright-browsers/pull/1297");
        // WebKit just loads an empty page.
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.StatusCode = 204;
            res.End();
        });
        Exception error = null;
        error = await Catch(() => Page.GotoAsync(Server.EmptyPage));
        Assert.That(error, Is.Not.Null);
        if (BrowserName == "chromium")
        {
            Assert.That(error.Message, Does.Contain("net::ERR_ABORTED"));
        }
        else if (BrowserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain("Aborted: 204 No Content"));
        }
        else
        {
            Assert.That(error.Message, Does.Contain("NS_BINDING_ABORTED"));
        }
    }

    [UpstreamTest("NA-021 should navigate to empty page with domcontentloaded")]
    public async Task ShouldNavigateToEmptyPageWithDomcontentloaded()
    {
        var response = await Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("NA-022 should work when page calls history API in beforeunload")]
    public async Task ShouldWorkWhenPageCallsHistoryAPIInBeforeunload()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"() => {
    window.addEventListener('beforeunload', () => history.replaceState(null, 'initial', window.location.href), false);
  }");
        var response = await Page.GotoAsync(Server.Prefix + "/grid.html");
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("NA-023 should fail when navigating to bad url")]
    public async Task ShouldFailWhenNavigatingToBadUrl()
    {
        Exception error = null;
        error = await Catch(() => Page.GotoAsync("asdfasdf"));
        if (BrowserName == "chromium" && IsBidi)
        {
            Assert.That(error.Message, Does.Contain("Invalid URL"));
        }
        else if (BrowserName == "firefox" && IsBidi)
        {
            Assert.That(error.Message, Does.Contain("NS_ERROR_MALFORMED_URI"));
        }
        else if (BrowserName == "chromium" || BrowserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain("Cannot navigate to invalid URL"));
        }
        else
        {
            Assert.That(error.Message, Does.Contain("Invalid url"));
        }
    }

    [UpstreamTest("NA-024 should fail when navigating to bad SSL")]
    public async Task ShouldFailWhenNavigatingToBadSSL()
    {
        // Make sure that network events do not emit 'undefined'.
        // @see https://crbug.com/750469
        Page.Request += (_, request) => Assert.That(request, Is.Not.Null);
        Page.RequestFinished += (_, request) => Assert.That(request, Is.Not.Null);
        Page.RequestFailed += (_, request) => Assert.That(request, Is.Not.Null);
        Exception error = null;
        error = await Catch(() => Page.GotoAsync(HttpsServer.EmptyPage));
        Assert.That(error.Message, Does.Match(ExpectedSSLError(BrowserName, Platform, Channel)));
    }

    [UpstreamTest("NA-025 should fail when navigating to bad SSL after redirects")]
    public async Task ShouldFailWhenNavigatingToBadSSLAfterRedirects()
    {
        Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
        Server.SetRedirect("/redirect/2.html", "/empty.html");
        Exception error = null;
        error = await Catch(() => Page.GotoAsync(HttpsServer.Prefix + "/redirect/1.html"));
        Assert.That(error.Message, Does.Match(ExpectedSSLError(BrowserName, Platform, Channel)));
    }

    [UpstreamTest("NA-026 should not crash when navigating to bad SSL after a cross origin navigation")]
    public async Task ShouldNotCrashWhenNavigatingToBadSSLAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        await Catch(() => Page.GotoAsync(HttpsServer.EmptyPage));
    }

    [UpstreamTest("NA-029 should fail when main resources failed to load")]
    public async Task ShouldFailWhenMainResourcesFailedToLoad()
    {
        Info.Skip(Channel == "webkit-wsl", "Networking mode mirrored ends up stalling connections rather than terminating them, see https://github.com/microsoft/WSL/issues/10855.");
        Exception error = null;
        error = await Catch(() => Page.GotoAsync("http://localhost:44123/non-existing-url"));
        if (BrowserName == "chromium")
        {
            Assert.That(error.Message, Does.Contain("net::ERR_CONNECTION_REFUSED"));
        }
        else if (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl")
        {
            Assert.That(error.Message, Does.Contain("Could not connect to server"));
        }
        else if (BrowserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain("Could not connect"));
        }
        else
        {
            Assert.That(error.Message, Does.Contain("NS_ERROR_CONNECTION_REFUSED"));
        }
    }

    [UpstreamTest("NA-030 should fail when exceeding maximum navigation timeout")]
    public async Task ShouldFailWhenExceedingMaximumNavigationTimeout()
    {
        // Hang for request to the empty.html
        Server.SetRoute("/empty.html", (req, res) => { });
        Exception error = null;
        error = await Catch(() => Page.GotoAsync(Server.Prefix + "/empty.html", new() { Timeout = 1 }));
        Assert.That(error.Message, Does.Contain("Timeout 1ms exceeded."));
        Assert.That(error.Message, Does.Contain(Server.Prefix + "/empty.html"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("NA-031 should fail when exceeding default maximum navigation timeout")]
    public async Task ShouldFailWhenExceedingDefaultMaximumNavigationTimeout()
    {
        Info.Skip(IsAndroid || IsElectron, "No context per test");

        // Hang for request to the empty.html
        Server.SetRoute("/empty.html", (req, res) => { });
        Exception error = null;
        Page.Context.SetDefaultNavigationTimeout(2);
        Page.SetDefaultNavigationTimeout(1);
        error = await Catch(() => Page.GotoAsync(Server.Prefix + "/empty.html"));
        Assert.That(error.Message, Does.Contain("Timeout 1ms exceeded."));
        Assert.That(error.Message, Does.Contain(Server.Prefix + "/empty.html"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("NA-032 should fail when exceeding browser context navigation timeout")]
    public async Task ShouldFailWhenExceedingBrowserContextNavigationTimeout()
    {
        Info.Skip(IsAndroid || IsElectron, "No context per test");

        // Hang for request to the empty.html
        Server.SetRoute("/empty.html", (req, res) => { });
        Exception error = null;
        Page.Context.SetDefaultNavigationTimeout(2);
        error = await Catch(() => Page.GotoAsync(Server.Prefix + "/empty.html"));
        Assert.That(error.Message, Does.Contain("Timeout 2ms exceeded."));
        Assert.That(error.Message, Does.Contain(Server.Prefix + "/empty.html"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("NA-035 should prioritize default navigation timeout over default timeout")]
    public async Task ShouldPrioritizeDefaultNavigationTimeoutOverDefaultTimeout()
    {
        // Hang for request to the empty.html
        Server.SetRoute("/empty.html", (req, res) => { });
        Exception error = null;
        Page.SetDefaultTimeout(0);
        Page.SetDefaultNavigationTimeout(1);
        error = await Catch(() => Page.GotoAsync(Server.Prefix + "/empty.html"));
        Assert.That(error.Message, Does.Contain("Timeout 1ms exceeded."));
        Assert.That(error.Message, Does.Contain(Server.Prefix + "/empty.html"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("NA-036 should disable timeout when its set to 0")]
    public async Task ShouldDisableTimeoutWhenItsSetTo0()
    {
        Exception error = null;
        var loaded = false;
        EventHandler<IPage> handler = null;
        handler = (_, _) =>
        {
            Page.Load -= handler;
            loaded = true;
        };
        Page.Load += handler;
        error = await Catch(() => Page.GotoAsync(Server.Prefix + "/grid.html", new() { Timeout = 0, WaitUntil = WaitUntilState.Load }));
        Assert.That(error, Is.Null);
        Assert.That(loaded, Is.EqualTo(true));
    }

    [UpstreamTest("NA-037 should fail when replaced by another navigation")]
    public async Task ShouldFailWhenReplacedByAnotherNavigation()
    {
        Task<IResponse> anotherPromise = null;
        Server.SetRoute("/empty.html", (req, res) =>
        {
            anotherPromise = Page.GotoAsync(Server.Prefix + "/one-style.html");
            // Hang request to empty.html.
        });
        var error = await Catch(() => Page.GotoAsync(Server.Prefix + "/empty.html"));
        await anotherPromise;
        if (BrowserName == "chromium")
        {
            Assert.That(error.Message, Does.Contain("net::ERR_ABORTED"));
        }
        else if (BrowserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain($"Navigation to \"{Server.Prefix + "/empty.html"}\" is interrupted by another navigation to \"{Server.Prefix + "/one-style.html"}\""));
        }
        else if (BrowserName == "firefox")
        {
            if (IsBidi)
            {
                Assert.That(error.Message, Does.Contain("Protocol error (browsingContext.navigate): unknown error"));
            }
            else
            {
                // Firefox might yield either NS_BINDING_ABORTED or 'navigation interrupted by another one'
                Assert.That(error.Message.Contains($"Navigation to \"{Server.Prefix + "/empty.html"}\" is interrupted by another navigation to \"{Server.Prefix + "/one-style.html"}\"", StringComparison.Ordinal) || error.Message.Contains("NS_BINDING_ABORTED", StringComparison.Ordinal), Is.EqualTo(true));
            }
        }
    }

    [UpstreamTest("NA-038 js redirect overrides url bar navigation ")]
    public async Task JsRedirectOverridesUrlBarNavigation()
    {
        Info.Skip(Trace == "on", "tracing waits for snapshot that never arrives because pending navigation");

        Server.SetRoute("/a", (req, res) =>
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
        Server.SetRoute("/b", async (req, res) =>
        {
            events.Add("started b");
            await Task.Delay(2000);
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End("BBB");
            events.Add("finished b");
        });
        Server.SetRoute("/c", async (req, res) =>
        {
            events.Add("started c");
            await Task.Delay(2000);
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End("CCC");
            events.Add("finished c");
        });
        await Page.GotoAsync(Server.Prefix + "/a");
        var error = await Catch(() => Page.GotoAsync(Server.Prefix + "/b"));
        var expectEvents = (BrowserName == "chromium") ?
            new[] { "started b", "finished b" } :
            new[] { "started b", "started c", "finished b", "finished c" };
        await Expect(() => Assert.That(events, Is.EqualTo(expectEvents))).ToPassAsync();
        Assert.That(events, Is.EqualTo(expectEvents));
        if (BrowserName == "chromium")
        {
            // Chromium prioritizes the url bar navigation over the js redirect.
            Assert.That(error, Is.Null);
            await Expect(Page).ToHaveURLAsync(Server.Prefix + "/b");
        }
        else if (BrowserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain($"Navigation to \"{Server.Prefix + "/b"}\" is interrupted by another navigation to \"{Server.Prefix + "/c"}\""));
            await Expect(Page).ToHaveURLAsync(Server.Prefix + "/c");
        }
        else if (BrowserName == "firefox")
        {
            Assert.That(error.Message, Does.Contain("NS_BINDING_ABORTED"));
            await Expect(Page).ToHaveURLAsync(Server.Prefix + "/c");
        }
    }

    [UpstreamTest("NA-039 should succeed on url bar navigation when there is pending navigation")]
    public async Task ShouldSucceedOnUrlBarNavigationWhenThereIsPendingNavigation()
    {
        Info.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
        Server.SetRoute("/a", (req, res) =>
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
        Server.SetRoute("/b", async (req, res) =>
        {
            events.Add("started b");
            await Task.Delay(2000);
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End("BBB");
            events.Add("finished b");
        });
        Server.SetRoute("/c", async (req, res) =>
        {
            events.Add("started c");
            await Task.Delay(2000);
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End("CCC");
            events.Add("finished c");
        });
        await Page.GotoAsync(Server.Prefix + "/a");
        await Page.WaitForTimeoutAsync(1000);
        var error = await Catch(() => Page.GotoAsync(Server.Prefix + "/b"));
        var expectEvents = new[] { "started c", "started b", "finished c", "finished b" };
        await Expect(() => Assert.That(events, Is.EqualTo(expectEvents))).ToPassAsync(new() { Timeout = 5000 });
        Assert.That(events, Is.EqualTo(expectEvents));
        Assert.That(error, Is.Null);
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/b"));
    }

    [UpstreamTest("NA-040 should work when navigating to valid url")]
    public async Task ShouldWorkWhenNavigatingToValidUrl()
    {
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    [UpstreamTest("NA-041 should work when navigating to data url")]
    public async Task ShouldWorkWhenNavigatingToDataUrl()
    {
        var response = await Page.GotoAsync("data:text/html,hello");
        Assert.That(response, Is.Null);
    }

    [UpstreamTest("NA-042 should work when navigating to 404")]
    public async Task ShouldWorkWhenNavigatingTo404()
    {
        var response = await Page.GotoAsync(Server.Prefix + "/not-found");
        Assert.That(response.Ok, Is.EqualTo(false));
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [UpstreamTest("NA-043 should return last response in redirect chain")]
    public async Task ShouldReturnLastResponseInRedirectChain()
    {
        Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
        Server.SetRedirect("/redirect/2.html", "/redirect/3.html");
        Server.SetRedirect("/redirect/3.html", Server.EmptyPage);
        var response = await Page.GotoAsync(Server.Prefix + "/redirect/1.html");
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("NA-047 should navigate to dataURL and not fire dataURL requests")]
    public async Task ShouldNavigateToDataURLAndNotFireDataURLRequests()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, request) => requests.Add(request);
        var dataURL = "data:text/html,<div>yo</div>";
        var response = await Page.GotoAsync(dataURL);
        Assert.That(response, Is.Null);
        Assert.That(requests.Count, Is.EqualTo(0));
    }

    [UpstreamTest("NA-048 should navigate to URL with hash and fire requests without hash")]
    public async Task ShouldNavigateToURLWithHashAndFireRequestsWithoutHash()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, request) => requests.Add(request);
        var response = await Page.GotoAsync(Server.EmptyPage + "#hash");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.Url, Is.EqualTo(Server.EmptyPage));
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(requests[0].Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("NA-049 should work with self requesting page")]
    public async Task ShouldWorkWithSelfRequestingPage()
    {
        var response = await Page.GotoAsync(Server.Prefix + "/self-request.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.Url, Does.Contain("self-request.html"));
    }

    [UpstreamTest("NA-050 should fail when navigating and show the url at the error message")]
    public async Task ShouldFailWhenNavigatingAndShowTheUrlAtTheErrorMessage()
    {
        var url = HttpsServer.Prefix + "/redirect/1.html";
        Exception error = null;
        try
        {
            await Page.GotoAsync(url);
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error.Message, Does.Contain(url));
    }

    [UpstreamTest("NA-051 should be able to navigate to a page controlled by service worker")]
    public async Task ShouldBeAbleToNavigateToAPageControlledByServiceWorker()
    {
        Info.Skip(IsElectron);
        Info.Skip(IsAndroid, "Service workers need a secure context, but the Android test server is reached over http://10.0.2.2");

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

        await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetch/sw.html");
        await Page.EvaluateAsync("() => window['activationPromise']");
        await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetch/sw.html");
        await Page.WaitForFunctionAsync("() => !!navigator.serviceWorker.controller");
        await Page.EvaluateAsync(transaction, "value");
        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        await Page.GoBackAsync();
        await Page.WaitForFunctionAsync("() => !!navigator.serviceWorker.controller");
        Assert.That(await Page.EvaluateAsync<string>(transaction, null), Is.EqualTo("value"));
    }

    [UpstreamTest("NA-052 should send referer")]
    public async Task ShouldSendReferer()
    {
        var (request1, request2, _) = await PromiseAll(
            Server.WaitForRequest("/grid.html"),
            Server.WaitForRequest("/digits/1.png"),
            Page.GotoAsync(Server.Prefix + "/grid.html", new()
            {
                Referer = "http://google.com/",
            }));
        Assert.That(request1.Headers["referer"], Is.EqualTo("http://google.com/"));
        // Make sure subresources do not inherit referer.
        Assert.That(request2.Headers["referer"], Is.EqualTo(Server.Prefix + "/grid.html"));
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/grid.html"));
    }

    [UpstreamTest("NA-053 should send referer of cross-origin URL")]
    public async Task ShouldSendRefererOfCrossOriginURL()
    {
        var (request1, request2, _) = await PromiseAll(
            Server.WaitForRequest("/grid.html"),
            Server.WaitForRequest("/digits/1.png"),
            Page.GotoAsync(Server.Prefix + "/grid.html", new()
            {
                Referer = "https://microsoft.com/xbox/"
            }));
        Assert.That(request1.Headers["referer"], Is.EqualTo("https://microsoft.com/xbox/"));
        // Make sure subresources do not inherit referer.
        Assert.That(request2.Headers["referer"], Is.EqualTo(Server.Prefix + "/grid.html"));
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/grid.html"));
    }

    [UpstreamTest("NA-054 should reject referer option when setExtraHTTPHeaders provides referer")]
    public async Task ShouldRejectRefererOptionWhenSetExtraHTTPHeadersProvidesReferer()
    {
        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = "http://microsoft.com/" });
        Exception error = null;
        error = await Catch(() => Page.GotoAsync(Server.Prefix + "/grid.html", new()
        {
            Referer = "http://google.com/",
        }));
        Assert.That(error.Message, Does.Contain("\"referer\" is already specified as extra HTTP header"));
        Assert.That(error.Message, Does.Contain(Server.Prefix + "/grid.html"));
    }

    [UpstreamTest("NA-055 should override referrer-policy")]
    public async Task ShouldOverrideReferrerPolicy()
    {
        Server.SetRoute("/grid.html", (req, res) =>
        {
            res.SetHeader("Referrer-Policy", "no-referrer");
            Server.ServeFile(req, res);
        });
        var (request1, request2, _) = await PromiseAll(
            Server.WaitForRequest("/grid.html"),
            Server.WaitForRequest("/digits/1.png"),
            Page.GotoAsync(Server.Prefix + "/grid.html", new()
            {
                Referer = "http://microsoft.com/",
            }));
        Assert.That(request1.Headers["referer"], Is.EqualTo("http://microsoft.com/"));
        // Make sure subresources do not inherit referer.
        Assert.That(request2.Headers["referer"], Is.Null);
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/grid.html"));
    }

    [UpstreamTest("NA-056 should fail when canceled by another navigation")]
    public async Task ShouldFailWhenCanceledByAnotherNavigation()
    {
        Server.SetRoute("/one-style.html", (req, res) => { });
        var failed = Catch(() => Page.GotoAsync(Server.Prefix + "/one-style.html"));
        await Server.WaitForRequest("/one-style.html");
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var error = await failed;
        Assert.That(error.Message, Is.Not.Empty);
    }

    [UpstreamTest("NA-057 should work with lazy loading iframes")]
    public async Task ShouldWorkWithLazyLoadingIframes()
    {
        Info.Fixme(IsAndroid);

        await Page.GotoAsync(Server.Prefix + "/frames/lazy-frame.html");
        Assert.That(Page.Frames.Count, Is.EqualTo(2));
    }

    [UpstreamTest("NA-058 should report raw buffer for main resource")]
    public async Task ShouldReportRawBufferForMainResource()
    {
        Info.Fixme(BrowserName == "chromium", "Chromium sends main resource as text");
        Info.Fixme(BrowserName == "webkit" && Platform == "win32" && Channel != "webkit-wsl", "Same here");

        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.StatusCode = 200;
            res.End(Encoding.UTF8.GetBytes("Ü (lowercase ü)"));
        });
        var response = await Page.GotoAsync(Server.Prefix + "/empty.html");
        var body = await response.BodyAsync();
        Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo("Ü (lowercase ü)"));
    }

    [UpstreamTest("NA-059 should not throw unhandled rejections on invalid url")]
    public async Task ShouldNotThrowUnhandledRejectionsOnInvalidUrl()
    {
        var e = await Catch(() => Page.GotoAsync("https://www.youtube Panel Title.com/"));
        Assert.That(e.ToString(), Does.Contain("Panel Title"));
    }

    [UpstreamTest("NA-060 should not crash when RTCPeerConnection is used")]
    public async Task ShouldNotCrashWhenRTCPeerConnectionIsUsed()
    {
        Server.SetRoute("/rtc.html", (_, res) =>
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
        await Page.GotoAsync(Server.Prefix + "/rtc.html");
        await Page.EvaluateAsync(@"() => {
    // RTCPeerConnection is not present on WebKit Linux
    window.RTCPeerConnection && new window.RTCPeerConnection({
      iceServers: []
    });
  }");
    }

    [UpstreamTest("NA-061 should properly wait for load")]
    public async Task ShouldProperlyWaitForLoad()
    {
        Server.SetRoute("/slow.js", async (req, res) =>
        {
            await Task.Delay(100);
            res.WriteHead(200, new() { ["Content-Type"] = "application/javascript" });
            res.End("window.results.push('slow module');export const foo = 'slow';");
        });
        await Page.GotoAsync(Server.Prefix + "/load-event/load-event.html");
        var results = await Page.EvaluateAsync<string[]>("window.results");
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
    public async Task ShouldNotResolveGotoUponWindowStop()
    {
        Info.Fixme(BrowserName == "firefox" && !IsBidi, "load/domcontentloaded events are flaky");
        Info.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");

        ServerResponse response = null;
        Server.SetRoute("/module.js", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/javascript" });
            response = res;
        });
        var done = false;
        async Task Goto()
        {
            await Page.GotoAsync(Server.Prefix + "/window-stop.html");
            done = true;
        }
        IgnoreErrors(Goto());
        await Server.WaitForRequest("/module.js");
        Assert.That(done, Is.EqualTo(false));
        await Page.WaitForTimeoutAsync(1000);  // give it some time to erroneously resolve
        response.End("");
        await Page.WaitForTimeoutAsync(1000);  // give it more time to erroneously resolve
        Assert.That(done, Is.EqualTo(false));
    }

    [UpstreamTest("NA-063 should return from goto if new navigation is started")]
    public async Task ShouldReturnFromGotoIfNewNavigationIsStarted()
    {
        Info.Fixme(IsAndroid);
        Server.SetRoute("/slow.js", (req, res) => { });
        var finished = false;
        async Task<IResponse> Navigation()
        {
            var r = await Page.GotoAsync(Server.Prefix + "/load-event/load-event.html");
            finished = true;
            return r;
        }
        var navigation = Navigation();
        await Task.Delay(500);
        Assert.That(finished, Is.False);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That((await navigation).Status, Is.EqualTo(200));
    }

    [UpstreamTest("NA-064 should return when navigation is committed if commit is specified")]
    public async Task ShouldReturnWhenNavigationIsCommittedIfCommitIsSpecified()
    {
        Server.SetRoute("/script.js", (req, res) => { });
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<title>Hello</title><script src=\"script.js\"></script>");
        });
        var response = await Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.Commit });
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await Page.TitleAsync(), Is.EqualTo("Hello"));
    }

    [UpstreamTest("NA-065 should wait for load when iframe attaches and detaches")]
    public async Task ShouldWaitForLoadWhenIframeAttachesAndDetaches()
    {
        Info.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
        Server.SetRoute("/empty.html", (req, res) =>
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

        Server.SetRoute("/iframe.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(@"
      <link rel=""stylesheet"" href=""./style2.css"">
    ");
        });

        // Stall the css so that 'load' does not fire.
        Server.SetRoute("/style2.css", (req, res) => { });

        var frameDetached = Once<IFrame>(h => Page.FrameDetached += h, h => Page.FrameDetached -= h);
        var done = Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.Load });
        await frameDetached; // Make sure that iframe is gone.
        await done;
        Assert.That(await Page.QuerySelectorAsync("iframe"), Is.Null);
    }

    [UpstreamTest("NA-066 should return url with basic auth info")]
    public async Task ShouldReturnUrlWithBasicAuthInfo()
    {
        var url = $"http://admin:admin@{Loopback ?? "localhost"}:{Server.Port}/empty.html";
        await Page.GotoAsync(url);
        Assert.That(Page.Url, Is.EqualTo(url));
    }
}
