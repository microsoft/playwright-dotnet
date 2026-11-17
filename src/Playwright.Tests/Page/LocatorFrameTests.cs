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
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/locator-frame.spec.ts.</summary>
public class LocatorFrameTests
{
    private static async Task RouteIframe(IPage page)
    {
        await page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = "<iframe src=\"iframe.html\" name=\"frame1\"></iframe>",
                ContentType = "text/html"
            }));
        });
        await page.RouteAsync("**/iframe.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = @"
        <html>
          <div>
            <button data-testid=""buttonId"">Hello iframe</button>
            <iframe src=""iframe-2.html""></iframe>
          </div>
          <span>1</span>
          <span>2</span>
          <label for=target>Name</label><input id=target type=text placeholder=Placeholder title=Title alt=Alternative>
        </html>",
                ContentType = "text/html"
            }));
        });
        await page.RouteAsync("**/iframe-2.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = "<html><button>Hello nested iframe</button></html>",
                ContentType = "text/html"
            }));
        });
    }

    private static async Task RouteAmbiguous(IPage page)
    {
        await page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = @"<iframe src=""iframe-1.html""></iframe>
             <iframe src=""iframe-2.html""></iframe>
             <iframe src=""iframe-3.html""></iframe>",
                ContentType = "text/html"
            }));
        });
        await page.RouteAsync("**/iframe-*", route =>
        {
            var path = new Uri(route.Request.Url).AbsolutePath.Substring(1);
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = $"<html><button>Hello from {path}</button></html>",
                ContentType = "text/html"
            }));
        });
    }

    [UpstreamTest("LO-089 should work for iframe @smoke")]
    public async Task ShouldWorkForIframe(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.FrameLocator("iframe").Locator("button");
        await button.WaitForAsync();
        Assert.That(await button.InnerTextAsync(), Is.EqualTo("Hello iframe"));
        await Expect(button).ToHaveTextAsync("Hello iframe");
        await button.ClickAsync();
    }

    [UpstreamTest("LO-090 should work for nested iframe")]
    public async Task ShouldWorkForNestedIframe(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.FrameLocator("iframe").FrameLocator("iframe").Locator("button");
        await button.WaitForAsync();
        Assert.That(await button.InnerTextAsync(), Is.EqualTo("Hello nested iframe"));
        await Expect(button).ToHaveTextAsync("Hello nested iframe");
        await button.ClickAsync();
    }

    [UpstreamTest("LO-091 should work for $ and $$")]
    public async Task ShouldWorkForAnd(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var locator = page.FrameLocator("iframe").Locator("button");
        await Expect(locator).ToHaveTextAsync("Hello iframe");
        var spans = page.FrameLocator("iframe").Locator("span");
        await Expect(spans).ToHaveCountAsync(2);
    }

    [UpstreamTest("LO-092 should wait for frame")]
    [LocalFixme("dotnet: the call log has Locator(\"body\").Locator(\"iframe\").ContentFrame, upstream expects locator('body').locator('iframe').contentFrame()")]
    public async Task ShouldWaitForFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var error = await Catch(() => page.Locator("body").FrameLocator("iframe").Locator("span").ClickAsync(new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("waiting for locator('body').locator('iframe').contentFrame()"));
    }

    [UpstreamTest("LO-093 should wait for frame 2")]
    public async Task ShouldWaitForFrame2(IPage page, Server server)
    {
        await RouteIframe(page);
        SetTimeout(() => IgnoreErrors(page.GotoAsync(server.EmptyPage)), 300);
        await page.FrameLocator("iframe").Locator("button").ClickAsync();
    }

    [UpstreamTest("LO-094 should wait for frame to go")]
    public async Task ShouldWaitForFrameToGo(IPage page, Server server, bool isAndroid)
    {
        Info.Fixme(isAndroid);

        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        SetTimeout(() => IgnoreErrors(page.EvalOnSelectorAsync("iframe", "e => e.remove()")), 300);
        await Expect(page.FrameLocator("iframe").Locator("button")).ToBeHiddenAsync();
    }

    [UpstreamTest("LO-095 should not wait for frame")]
    public async Task ShouldNotWaitForFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await Expect(page.FrameLocator("iframe").Locator("span")).ToBeHiddenAsync();
    }

    [UpstreamTest("LO-096 should not wait for frame 2")]
    public async Task ShouldNotWaitForFrame2(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await Expect(page.FrameLocator("iframe").Locator("span")).Not.ToBeVisibleAsync();
    }

    [UpstreamTest("LO-097 should not wait for frame 3")]
    public async Task ShouldNotWaitForFrame3(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await Expect(page.FrameLocator("iframe").Locator("span")).ToHaveCountAsync(0);
    }

    [UpstreamTest("LO-098 should click in lazy iframe")]
    public async Task ShouldClickInLazyIframe(IPage page, Server server)
    {
        await page.RouteAsync("**/iframe.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = "<html><button>Hello iframe</button></html>",
                ContentType = "text/html"
            }));
        });

        // empty pge
        await page.GotoAsync(server.EmptyPage);

        // add blank iframe
        SetTimeout(() =>
        {
            IgnoreErrors(page.EvaluateAsync(@"() => {
      const iframe = document.createElement('iframe');
      document.body.appendChild(iframe);
    }"));
            // navigate iframe
            SetTimeout(() =>
            {
                IgnoreErrors(page.EvaluateAsync("() => document.querySelector('iframe').src = 'iframe.html'"));
            }, 500);
        }, 500);

        // Click in iframe
        var button = page.FrameLocator("iframe").Locator("button");
        // .NET: const [, text] = await Promise.all([a, b, c]) is Promise.all([a, Promise.all([b, c])]): PromiseAll has no overload that gives the value of the second task of three.
        var text = await PromiseAll(
            button.ClickAsync(),
            PromiseAll(
                button.InnerTextAsync(),
                Expect(button).ToHaveTextAsync("Hello iframe")));
        Assert.That(text, Is.EqualTo("Hello iframe"));
    }

    [UpstreamTest("LO-099 waitFor should survive frame reattach")]
    public async Task WaitForShouldSurviveFrameReattach(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.FrameLocator("iframe").Locator("button:has-text(\"Hello nested iframe\")");
        var promise = button.WaitForAsync();
        await page.Locator("iframe").EvaluateAsync("e => e.remove()");
        await page.EvaluateAsync(@"() => {
    const iframe = document.createElement('iframe');
    iframe.src = 'iframe-2.html';
    document.body.appendChild(iframe);
  }");
        await promise;
    }

    [UpstreamTest("LO-100 click should survive frame reattach")]
    public async Task ClickShouldSurviveFrameReattach(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.FrameLocator("iframe").Locator("button:has-text(\"Hello nested iframe\")");
        var promise = button.ClickAsync();
        await page.Locator("iframe").EvaluateAsync("e => e.remove()");
        await page.EvaluateAsync(@"() => {
    const iframe = document.createElement('iframe');
    iframe.src = 'iframe-2.html';
    document.body.appendChild(iframe);
  }");
        await promise;
    }

    [UpstreamTest("LO-101 click should survive iframe navigation")]
    public async Task ClickShouldSurviveIframeNavigation(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.FrameLocator("iframe").Locator("button:has-text(\"Hello nested iframe\")");
        var promise = button.ClickAsync();
        IgnoreErrors(page.Locator("iframe").EvaluateAsync("e => e.src = 'iframe-2.html'"));
        await promise;
    }

    [UpstreamTest("LO-102 should non work for non-frame")]
    public async Task ShouldNonWorkForNonFrame(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.SetContentAsync("<div></div>");
        var button = page.FrameLocator("div").Locator("button");
        var error = await Catch(() => button.WaitForAsync());
        Assert.That(error.Message, Does.Contain("<div></div>"));
        Assert.That(error.Message, Does.Contain("<iframe> was expected"));
    }

    [UpstreamTest("LO-103 locator.frameLocator should work for iframe")]
    public async Task LocatorFrameLocatorShouldWorkForIframe(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.Locator("body").FrameLocator("iframe").Locator("button");
        await button.WaitForAsync();
        Assert.That(await button.InnerTextAsync(), Is.EqualTo("Hello iframe"));
        await Expect(button).ToHaveTextAsync("Hello iframe");
        await button.ClickAsync();
    }

    [UpstreamTest("LO-104 locator.frameLocator should throw on ambiguity")]
    [LocalFixme("dotnet: the error has Locator(\"body\").Locator(\"iframe\"), upstream expects locator('body').locator('iframe')")]
    public async Task LocatorFrameLocatorShouldThrowOnAmbiguity(IPage page, Server server)
    {
        await RouteAmbiguous(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.Locator("body").FrameLocator("iframe").Locator("button");
        var error = await Catch(() => button.WaitForAsync());
        Assert.That(error.Message, Does.Contain("Error: strict mode violation: locator('body').locator('iframe') resolved to 3 elements"));
    }

    [UpstreamTest("LO-105 locator.frameLocator should not throw on first/last/nth")]
    public async Task LocatorFrameLocatorShouldNotThrowOnFirstLastNth(IPage page, Server server)
    {
        await RouteAmbiguous(page);
        await page.GotoAsync(server.EmptyPage);
        var button1 = page.Locator("body").FrameLocator("iframe").First.Locator("button");
        await Expect(button1).ToHaveTextAsync("Hello from iframe-1.html");
        var button2 = page.Locator("body").FrameLocator("iframe").Nth(1).Locator("button");
        await Expect(button2).ToHaveTextAsync("Hello from iframe-2.html");
        var button3 = page.Locator("body").FrameLocator("iframe").Last.Locator("button");
        await Expect(button3).ToHaveTextAsync("Hello from iframe-3.html");
    }

    [UpstreamTest("LO-106 getBy coverage")]
    public async Task GetByCoverage(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button1 = page.FrameLocator("iframe").GetByRole(AriaRole.Button);
        var button2 = page.FrameLocator("iframe").GetByText("Hello");
        var button3 = page.FrameLocator("iframe").GetByTestId("buttonId");
        await Expect(button1).ToHaveTextAsync("Hello iframe");
        await Expect(button2).ToHaveTextAsync("Hello iframe");
        await Expect(button3).ToHaveTextAsync("Hello iframe");
        var input1 = page.FrameLocator("iframe").GetByLabel("Name");
        await Expect(input1).ToHaveValueAsync("");
        var input2 = page.FrameLocator("iframe").GetByPlaceholder("Placeholder");
        await Expect(input2).ToHaveValueAsync("");
        var input3 = page.FrameLocator("iframe").GetByAltText("Alternative");
        await Expect(input3).ToHaveValueAsync("");
        var input4 = page.FrameLocator("iframe").GetByTitle("Title");
        await Expect(input4).ToHaveValueAsync("");
    }

    [UpstreamTest("LO-107 wait for hidden should succeed when frame is not in dom")]
    public async Task WaitForHiddenShouldSucceedWhenFrameIsNotInDom(IPage page)
    {
        await page.GotoAsync("about:blank");
        var button = page.FrameLocator("iframe1").Locator("button");
        Assert.That(await button.IsHiddenAsync(), Is.True);
        await button.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 1000 });
        await button.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 1000 });
        var error = await Catch(() => button.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
    }

    [UpstreamTest("LO-108 should work with COEP/COOP/CORP isolated iframe")]
    public async Task ShouldWorkWithCOEPCOOPCORPIsolatedIframe(IPage page, Server server, string browserName)
    {
        Info.Fixme(browserName == "firefox");
        await page.RouteAsync("**/empty.html", route =>
        {
            return route.FulfillAsync(new()
            {
                Body = $"<iframe src=\"https://{server.CrossProcessPrefix}/btn.html\" allow=\"cross-origin-isolated; fullscreen\" sandbox=\"allow-same-origin allow-scripts allow-popups\" ></iframe>",
                ContentType = "text/html",
                Headers = new Dictionary<string, string>
                {
                    ["cross-origin-embedder-policy"] = "require-corp",
                    ["cross-origin-opener-policy"] = "same-origin",
                    ["cross-origin-resource-policy"] = "cross-origin",
                }
            });
        });
        await page.RouteAsync("**/btn.html", route =>
        {
            return route.FulfillAsync(new()
            {
                Body = "<button onclick=\"window.__clicked=true\">Click target</button>",
                ContentType = "text/html",
                Headers = new Dictionary<string, string>
                {
                    ["cross-origin-embedder-policy"] = "require-corp",
                    ["cross-origin-opener-policy"] = "same-origin",
                    ["cross-origin-resource-policy"] = "cross-origin",
                }
            });
        });
        await page.GotoAsync(server.EmptyPage);
        await page.FrameLocator("iframe").GetByRole(AriaRole.Button).ClickAsync();
        Assert.That(await page.Frames[1].EvaluateAsync<bool>("() => window['__clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("LO-109 locator.contentFrame should work")]
    public async Task LocatorContentFrameShouldWork(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var locator = page.Locator("iframe");
        var frameLocator = locator.ContentFrame;
        var button = frameLocator.Locator("button");
        Assert.That(await button.InnerTextAsync(), Is.EqualTo("Hello iframe"));
        await Expect(button).ToHaveTextAsync("Hello iframe");
        await button.ClickAsync();
    }

    [UpstreamTest("LO-110 frameLocator.owner should work")]
    public async Task FrameLocatorOwnerShouldWork(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var frameLocator = page.FrameLocator("iframe");
        var locator = frameLocator.Owner;
        await Expect(locator).ToBeVisibleAsync();
        Assert.That(await locator.GetAttributeAsync("name"), Is.EqualTo("frame1"));
    }

    [UpstreamTest("LO-111 should treat elements inside hidden iframe as hidden")]
    public async Task ShouldTreatElementsInsideHiddenIframeAsHidden(IPage page)
    {
        await page.SetContentAsync(@"
    <iframe name=""hidden"" style=""visibility: hidden"" srcdoc=""<button>Hidden</button>""></iframe>
    <div style=""visibility: hidden"">
      <iframe name=""hidden-parent"" srcdoc=""<button>Hidden parent</button>""></iframe>
      <iframe name=""visible-override"" style=""visibility: visible"" srcdoc=""<button>Visible override</button>""></iframe>
    </div>
    <iframe name=""outer"" style=""visibility: hidden"" srcdoc=""<iframe name='inner' srcdoc='<button>Nested</button>'></iframe>""></iframe>
  ");

        foreach (var button in new[]
        {
            page.FrameLocator("[name=hidden]").Locator("button"),
            page.FrameLocator("[name=hidden-parent]").Locator("button"),
            page.FrameLocator("[name=outer]").FrameLocator("[name=inner]").Locator("button"),
        })
        {
            await Expect(button).ToBeAttachedAsync();
            Assert.That(await button.IsVisibleAsync(), Is.EqualTo(false));
            Assert.That(await button.IsHiddenAsync(), Is.EqualTo(true));
            await Expect(button).ToBeHiddenAsync();
            await Expect(button).Not.ToBeVisibleAsync();
            await button.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        // .NET: C# does not allow a second variable named button after the one of the loop.
        var button2 = page.FrameLocator("[name=visible-override]").Locator("button");
        Assert.That(await button2.IsVisibleAsync(), Is.EqualTo(true));
        await Expect(button2).ToBeVisibleAsync();
        await button2.WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    [UpstreamTest("LO-112 should wait for hidden iframe to become visible")]
    public async Task ShouldWaitForHiddenIframeToBecomeVisible(IPage page)
    {
        await page.SetContentAsync("<iframe name=\"frame\" style=\"visibility: hidden\" srcdoc=\"<button onclick='window.__clicked = true'>Button</button>\"></iframe>");
        var button = page.FrameLocator("[name=frame]").Locator("button");
        await Expect(button).ToBeHiddenAsync();

        var error = await Catch(() => button.ClickAsync(new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("element is inside a hidden frame, retrying"));
        Assert.That(await button.EvaluateAsync<object>("() => window.__clicked"), Is.Null);

        var clickPromise = button.ClickAsync();
        var visiblePromise = Expect(button).ToBeVisibleAsync();
        await page.EvaluateAsync("() => document.querySelector('iframe').style.visibility = 'visible'");
        await PromiseAll(clickPromise, visiblePromise);
        Assert.That(await button.EvaluateAsync<bool>("() => window.__clicked"), Is.EqualTo(true));

        await page.EvaluateAsync("() => document.querySelector('iframe').style.visibility = 'hidden'");
        await Expect(button).ToBeHiddenAsync();
        Assert.That(await button.IsVisibleAsync(), Is.EqualTo(false));
    }

    [UpstreamTest("LO-113 should treat elements inside hidden cross-origin iframe as hidden")]
    public async Task ShouldTreatElementsInsideHiddenCrossOriginIframeAsHidden(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync($"<iframe style=\"visibility: hidden\" src=\"{server.CrossProcessPrefix}/frames/frame.html\"></iframe>");
        var div = page.FrameLocator("iframe").Locator("div");
        await Expect(div).ToHaveTextAsync("Hi, I'm frame");
        Assert.That(await div.IsVisibleAsync(), Is.EqualTo(false));
        await Expect(div).ToBeHiddenAsync();

        await page.EvaluateAsync("() => document.querySelector('iframe').style.visibility = 'visible'");
        await Expect(div).ToBeVisibleAsync();
        Assert.That(await div.IsVisibleAsync(), Is.EqualTo(true));
    }
}
