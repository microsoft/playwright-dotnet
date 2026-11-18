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
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-route.spec.ts.</summary>
public class BrowsercontextRouteTests : UpstreamBrowserTest
{
    [UpstreamTest("RO-165 should intercept")]
    public async Task ShouldIntercept()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var intercepted = false;
        // .NET: a lambda can not use a local that is declared after it.
        IPage page = null;
        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted = true;
            var request = route.Request;
            Assert.That(request.Url, Does.Contain("empty.html"));
            Assert.That(request.Headers["user-agent"], Is.Not.Empty);
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.PostData, Is.Null);
            Assert.That(request.IsNavigationRequest, Is.EqualTo(true));
            Assert.That(request.ResourceType, Is.EqualTo("document"));
            Assert.That(request.Frame == page.MainFrame, Is.EqualTo(true));
            Assert.That(request.Frame.Url, Is.EqualTo("about:blank"));
            IgnoreErrors(route.ContinueAsync());
        });
        page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(intercepted, Is.EqualTo(true));
        await context.CloseAsync();
    }

    [UpstreamTest("RO-166 should unroute")]
    public async Task ShouldUnroute()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();

        var intercepted = new List<int>();
        await context.RouteAsync("**/*", route =>
        {
            intercepted.Add(1);
            IgnoreErrors(route.FallbackAsync());
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(2);
            IgnoreErrors(route.FallbackAsync());
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(3);
            IgnoreErrors(route.FallbackAsync());
        });
        Action<IRoute> handler4 = route =>
        {
            intercepted.Add(4);
            IgnoreErrors(route.FallbackAsync());
        };
        await context.RouteAsync(new Regex("empty.html"), handler4);
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 4, 3, 2, 1 }));

        intercepted = new List<int>();
        await context.UnrouteAsync(new Regex("empty.html"), handler4);
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 3, 2, 1 }));

        intercepted = new List<int>();
        await context.UnrouteAsync("**/empty.html");
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 1 }));

        await context.CloseAsync();
    }

    [UpstreamTest("RO-167 should yield to page.route")]
    public async Task ShouldYieldToPageRoute()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        await context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Status = 200, Body = "context" }));
        });
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Status = 200, Body = "page" }));
        });
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(await response.TextAsync(), Is.EqualTo("page"));
        await context.CloseAsync();
    }

    [UpstreamTest("RO-168 should fall back to context.route")]
    public async Task ShouldFallBackToContextRoute()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        await context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Status = 200, Body = "context" }));
        });
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/non-empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Status = 200, Body = "page" }));
        });
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(await response.TextAsync(), Is.EqualTo("context"));
        await context.CloseAsync();
    }

    [UpstreamTest("RO-169 should support Set-Cookie header")]
    public async Task ShouldSupportSetCookieHeader()
    {
        var context = await ContextFactory();
        var page = await context.NewPageAsync();
        await page.RouteAsync("https://example.com/", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["Set-Cookie"] = "name=value; domain=.example.com; Path=/",
                },
                ContentType = "text/html",
                Body = "done",
            }));
        });
        await page.GotoAsync("https://example.com");
        ExpectToEqual(await context.CookiesAsync(), new[]
        {
            new
            {
                sameSite = DefaultSameSiteCookieValue,
                name = "name",
                value = "value",
                domain = ".example.com",
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = false,
            },
        });
    }

    [UpstreamTest("RO-170 should ignore secure Set-Cookie header for insecure requests")]
    public async Task ShouldIgnoreSecureSetCookieHeaderForInsecureRequests()
    {
        Info.Fixme(BrowserName == "webkit" && !IsMac);

        var context = await ContextFactory();
        var page = await context.NewPageAsync();
        await page.RouteAsync("http://example.com/", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["Set-Cookie"] = "name=value; domain=.example.com; Path=/; Secure",
                },
                ContentType = "text/html",
                Body = "done",
            }));
        });
        await page.GotoAsync("http://example.com");
        ExpectToEqual(await context.CookiesAsync(), Array.Empty<object>());
    }

    [UpstreamTest("RO-171 should use Set-Cookie header in future requests")]
    public async Task ShouldUseSetCookieHeaderInFutureRequests()
    {
        var context = await ContextFactory();
        var page = await context.NewPageAsync();

        await page.RouteAsync(Server.EmptyPage, route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["Set-Cookie"] = "name=value",
                },
                ContentType = "text/html",
                Body = "done",
            }));
        });
        await page.GotoAsync(Server.EmptyPage);
        ExpectToEqual(await context.CookiesAsync(), new[]
        {
            new
            {
                sameSite = DefaultSameSiteCookieValue,
                name = "name",
                value = "value",
                domain = Server.Hostname,
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = false,
            },
        });

        var cookie = "";
        Server.SetRoute("/foo.html", (req, res) =>
        {
            cookie = req.Headers["cookie"];
            res.End();
        });
        await page.GotoAsync(Server.Prefix + "/foo.html");
        Assert.That(cookie, Is.EqualTo("name=value"));
    }

    [UpstreamTest("RO-172 should work with ignoreHTTPSErrors")]
    public async Task ShouldWorkWithIgnoreHTTPSErrors()
    {
        var context = await Browser.NewContextAsync(new(ContextOptions()) { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();

        await page.RouteAsync("**/*", route => route.ContinueAsync());
        var response = await page.GotoAsync(HttpsServer.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await context.CloseAsync();
    }

    [UpstreamTest("RO-173 should support the times parameter with route matching")]
    public async Task ShouldSupportTheTimesParameterWithRouteMatching()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var intercepted = new List<int>();
        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(1);
            IgnoreErrors(route.ContinueAsync());
        }, new() { Times = 1 });
        await page.GotoAsync(Server.EmptyPage);
        await page.GotoAsync(Server.EmptyPage);
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Has.Count.EqualTo(1));
    }

    [UpstreamTest("RO-174 should work if handler with times parameter was removed from another handler")]
    public async Task ShouldWorkIfHandlerWithTimesParameterWasRemovedFromAnotherHandler()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var intercepted = new List<string>();
        Func<IRoute, Task> handler = async route =>
        {
            intercepted.Add("first");
            IgnoreErrors(route.ContinueAsync());
        };
        await context.RouteAsync("**/*", handler, new() { Times = 1 });
        await context.RouteAsync("**/*", async route =>
        {
            intercepted.Add("second");
            await context.UnrouteAsync("**/*", handler);
            await route.FallbackAsync();
        });
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { "second" }));
        intercepted.Clear();
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { "second" }));
    }

    [UpstreamTest("RO-175 should support async handler w/ times")]
    public async Task ShouldSupportAsyncHandlerWTimes()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await context.RouteAsync("**/empty.html", async route =>
        {
            await Task.Delay(100);
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = "<html>intercepted</html>",
                ContentType = "text/html",
            }));
        }, new() { Times = 1 });
        await page.GotoAsync(Server.EmptyPage);
        await Expect(page.Locator("body")).ToHaveTextAsync("intercepted");
        await page.GotoAsync(Server.EmptyPage);
        await Expect(page.Locator("body")).Not.ToHaveTextAsync("intercepted");
    }

    [UpstreamTest("RO-176 should overwrite post body with empty string")]
    public async Task ShouldOverwritePostBodyWithEmptyString()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                PostData = Encoding.UTF8.GetBytes(""),
            }));
        });

        var req = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            page.SetContentAsync($@"
      <script>
        (async () => {{
            await fetch('{Server.EmptyPage}', {{
              method: 'POST',
              body: 'original',
            }});
        }})()
      </script>
    "));

        var body = Encoding.UTF8.GetString(await req.PostBody);
        Assert.That(body, Is.EqualTo(""));
    }

    [UpstreamTest("RO-177 should chain fallback")]
    public async Task ShouldChainFallback()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var intercepted = new List<int>();
        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(1);
            IgnoreErrors(route.FallbackAsync());
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(2);
            IgnoreErrors(route.FallbackAsync());
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(3);
            IgnoreErrors(route.FallbackAsync());
        });
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 3, 2, 1 }));
    }

    [UpstreamTest("RO-178 should chain fallback w/ dynamic URL")]
    public async Task ShouldChainFallbackWDynamicURL()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var intercepted = new List<int>();
        await context.RouteAsync("**/bar", route =>
        {
            intercepted.Add(1);
            IgnoreErrors(route.FallbackAsync(new() { Url = Server.EmptyPage }));
        });
        await context.RouteAsync("**/foo", route =>
        {
            intercepted.Add(2);
            IgnoreErrors(route.FallbackAsync(new() { Url = "http://localhost/bar" }));
        });

        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(3);
            IgnoreErrors(route.FallbackAsync(new() { Url = "http://localhost/foo" }));
        });

        await page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 3, 2, 1 }));
    }

    [UpstreamTest("RO-179 should not chain fulfill")]
    public async Task ShouldNotChainFulfill()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var failed = false;
        await context.RouteAsync("**/empty.html", route =>
        {
            failed = true;
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Status = 200, Body = "fulfilled" }));
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FallbackAsync());
        });
        var response = await page.GotoAsync(Server.EmptyPage);
        var body = await response.BodyAsync();
        Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo("fulfilled"));
        Assert.That(failed, Is.False);
    }

    [UpstreamTest("RO-180 should not chain abort")]
    public async Task ShouldNotChainAbort()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var failed = false;
        await context.RouteAsync("**/empty.html", route =>
        {
            failed = true;
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.AbortAsync());
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FallbackAsync());
        });
        var e = await Catch(() => page.GotoAsync(Server.EmptyPage));
        Assert.That(e, Is.Not.Null);
        Assert.That(failed, Is.False);
    }

    [UpstreamTest("RO-181 should chain fallback into page")]
    public async Task ShouldChainFallbackIntoPage()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var intercepted = new List<int>();
        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(1);
            IgnoreErrors(route.FallbackAsync());
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(2);
            IgnoreErrors(route.FallbackAsync());
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(3);
            IgnoreErrors(route.FallbackAsync());
        });
        await page.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(4);
            IgnoreErrors(route.FallbackAsync());
        });
        await page.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(5);
            IgnoreErrors(route.FallbackAsync());
        });
        await page.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(6);
            IgnoreErrors(route.FallbackAsync());
        });
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 6, 5, 4, 3, 2, 1 }));
    }

    [UpstreamTest("RO-182 should fall back async")]
    public async Task ShouldFallBackAsync()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        var intercepted = new List<int>();
        await context.RouteAsync("**/empty.html", async route =>
        {
            intercepted.Add(1);
            await Task.Delay(100);
            IgnoreErrors(route.FallbackAsync());
        });
        await context.RouteAsync("**/empty.html", async route =>
        {
            intercepted.Add(2);
            await Task.Delay(100);
            IgnoreErrors(route.FallbackAsync());
        });
        await context.RouteAsync("**/empty.html", async route =>
        {
            intercepted.Add(3);
            await Task.Delay(100);
            IgnoreErrors(route.FallbackAsync());
        });
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 3, 2, 1 }));
    }

    [UpstreamTest("RO-183 should bypass disk cache when context interception is enabled")]
    public async Task ShouldBypassDiskCacheWhenContextInterceptionIsEnabled()
    {
        var page = await PageFixture();
        await page.Context.RouteAsync("**/api*", route => route.ContinueAsync());
        await page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        {
            var requests = new List<ServerRequest>();
            Server.SetRoute("/api", (req, res) =>
            {
                requests.Add(req);
                res.StatusCode = 200;
                res.SetHeader("content-type", "text/plain");
                res.SetHeader("cache-control", "public, max-age=31536000");
                res.End("Hello");
            });
            for (var i = 0; i < 3; i++)
            {
                await Info.Step($"main frame iteration {i}", async () =>
                {
                    var respPromise = page.WaitForResponseAsync("**/api");
                    await page.EvaluateAsync(@"async () => {
          const response = await fetch('/api');
          return response.status;
        }");
                    var response = await respPromise;
                    Assert.That(response.Status, Is.EqualTo(200));
                    Assert.That(requests.Count, Is.EqualTo(i + 1));
                });
            }
        }
    }

    [UpstreamTest("RO-184 should fulfill popup main request using alias")]
    public async Task ShouldFulfillPopupMainRequestUsingAlias()
    {
        var page = await PageFixture();
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");

        await page.Context.RouteAsync("**/*", async route =>
        {
            var response = await route.FetchAsync();
            await route.FulfillAsync(new() { Response = response, Body = "hello" });
        });
        await page.SetContentAsync($"<a target=_blank href=\"{Server.EmptyPage}\">click me</a>");
        var popup = await PromiseAll(
            page.WaitForPopupAsync(),
            page.GetByText("click me").ClickAsync());
        await Expect(popup.Locator("body")).ToHaveTextAsync("hello");
    }
}
