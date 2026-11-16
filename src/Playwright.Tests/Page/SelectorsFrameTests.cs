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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/selectors-frame.spec.ts.</summary>
public class SelectorsFrameTests
{
    private static async Task RouteIframe(IPage page)
    {
        await page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = "<iframe src=\"iframe.html\"></iframe>",
                ContentType = "text/html",
            }));
        });
        await page.RouteAsync("**/iframe.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = @"
        <html>
          <div>
            <button>Hello iframe</button>
            <iframe src=""iframe-2.html""></iframe>
          </div>
          <span>1</span>
          <span>2</span>
        </html>",
                ContentType = "text/html",
            }));
        });
        await page.RouteAsync("**/iframe-2.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = "<html><button tag=\"iframe2\">Hello nested iframe</button></html>",
                ContentType = "text/html",
            }));
        });
    }

    [UpstreamTest("SE-098 should work for iframe @smoke")]
    public async Task ShouldWorkForIframe(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.Locator("iframe >> internal:control=enter-frame >> button");
        await button.WaitForAsync();
        Assert.That(await button.InnerTextAsync(), Is.EqualTo("Hello iframe"));
        await Expect(button).ToHaveTextAsync("Hello iframe");
        await button.ClickAsync();
    }

    [UpstreamTest("SE-099 should work for iframe (handle)")]
    public async Task ShouldWorkForIframeHandle(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var body = await page.QuerySelectorAsync("body");
        var button = await body.WaitForSelectorAsync("iframe >> internal:control=enter-frame >> button");
        Assert.That(await button.InnerTextAsync(), Is.EqualTo("Hello iframe"));
        Assert.That(await button.TextContentAsync(), Is.EqualTo("Hello iframe"));
        await button.ClickAsync();
    }

    [UpstreamTest("SE-100 should work for nested iframe")]
    public async Task ShouldWorkForNestedIframe(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.Locator("iframe >> internal:control=enter-frame >> iframe >> internal:control=enter-frame >> button");
        await button.WaitForAsync();
        Assert.That(await button.InnerTextAsync(), Is.EqualTo("Hello nested iframe"));
        await Expect(button).ToHaveTextAsync("Hello nested iframe");
        await button.ClickAsync();
    }

    [UpstreamTest("SE-101 should work for nested iframe (handle)")]
    public async Task ShouldWorkForNestedIframeHandle(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var body = await page.QuerySelectorAsync("body");
        var button = await body.WaitForSelectorAsync("iframe >> internal:control=enter-frame >> iframe >> internal:control=enter-frame >> button");
        Assert.That(await button.InnerTextAsync(), Is.EqualTo("Hello nested iframe"));
        Assert.That(await button.TextContentAsync(), Is.EqualTo("Hello nested iframe"));
        await button.ClickAsync();
    }

    [UpstreamTest("SE-102 should work for $ and $$")]
    public async Task ShouldWorkForAnd(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var element = await page.QuerySelectorAsync("iframe >> internal:control=enter-frame >> button");
        Assert.That(await element.TextContentAsync(), Is.EqualTo("Hello iframe"));
        var elements = await page.QuerySelectorAllAsync("iframe >> internal:control=enter-frame >> span");
        Assert.That(elements, Has.Count.EqualTo(2));
    }

    [UpstreamTest("SE-103 $ should not wait for frame")]
    public async Task ShouldNotWaitForFrame_SE103(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.QuerySelectorAsync("iframe >> internal:control=enter-frame >> canvas"), Is.Null);
        var body = await page.QuerySelectorAsync("body");
        Assert.That(await body.QuerySelectorAsync("iframe >> internal:control=enter-frame >> canvas"), Is.Null);
    }

    [UpstreamTest("SE-104 $$ should not wait for frame")]
    public async Task ShouldNotWaitForFrame_SE104(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.QuerySelectorAllAsync("iframe >> internal:control=enter-frame >> canvas"), Has.Count.EqualTo(0));
        var body = await page.QuerySelectorAsync("body");
        Assert.That(await body.QuerySelectorAllAsync("iframe >> internal:control=enter-frame >> canvas"), Has.Count.EqualTo(0));
    }

    [UpstreamTest("SE-105 $eval should throw for missing frame")]
    [Fixme("dotnet: the message is 'Failed to find element matching selector \"iframe >> internal:control=enter-frame >> canvas\"', without the 'page.$eval: ' and 'elementHandle.$eval: ' prefixes of the calls")]
    public async Task EvalShouldThrowForMissingFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        {
            var error = await Catch(() => page.EvalOnSelectorAsync("iframe >> internal:control=enter-frame >> canvas", "e => 1"));
            Assert.That(error.Message, Does.Contain("page.$eval: Failed to find element matching selector"));
        }
        {
            var body = await page.QuerySelectorAsync("body");
            var error = await Catch(() => body.EvalOnSelectorAsync("iframe >> internal:control=enter-frame >> canvas", "e => 1"));
            Assert.That(error.Message, Does.Contain("elementHandle.$eval: Failed to find element matching selector"));
        }
    }

    [UpstreamTest("SE-106 $$eval should not throw for missing frame")]
    public async Task EvalShouldNotThrowForMissingFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.EvalOnSelectorAllAsync<int>("iframe >> internal:control=enter-frame >> canvas", "e => e.length"), Is.EqualTo(0));
        var body = await page.QuerySelectorAsync("body");
        Assert.That(await body.EvalOnSelectorAllAsync<int>("iframe >> internal:control=enter-frame >> canvas", "e => e.length"), Is.EqualTo(0));
    }

    [UpstreamTest("SE-107 should work for $ and $$ (handle)")]
    public async Task ShouldWorkForAndHandle(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var body = await page.QuerySelectorAsync("body");
        var element = await body.QuerySelectorAsync("iframe >> internal:control=enter-frame >> button");
        Assert.That(await element.TextContentAsync(), Is.EqualTo("Hello iframe"));
        var elements = await body.QuerySelectorAllAsync("iframe >> internal:control=enter-frame >> span");
        Assert.That(elements, Has.Count.EqualTo(2));
    }

    [UpstreamTest("SE-108 should work for $eval")]
    public async Task ShouldWorkForEval_SE108(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var value = await page.EvalOnSelectorAsync<string>("iframe >> internal:control=enter-frame >> button", "b => b.nodeName");
        Assert.That(value, Is.EqualTo("BUTTON"));
    }

    [UpstreamTest("SE-109 should work for $eval (handle)")]
    public async Task ShouldWorkForEvalHandle_SE109(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var body = await page.QuerySelectorAsync("body");
        var value = await body.EvalOnSelectorAsync<string>("iframe >> internal:control=enter-frame >> button", "b => b.nodeName");
        Assert.That(value, Is.EqualTo("BUTTON"));
    }

    [UpstreamTest("SE-110 should work for $$eval")]
    public async Task ShouldWorkForEval_SE110(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var value = await page.EvalOnSelectorAllAsync<string[]>("iframe >> internal:control=enter-frame >> span", "ss => ss.map(s => s.textContent)");
        Assert.That(value, Is.EqualTo(new[] { "1", "2" }));
    }

    [UpstreamTest("SE-111 should work for $$eval (handle)")]
    public async Task ShouldWorkForEvalHandle_SE111(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var body = await page.QuerySelectorAsync("body");
        var value = await body.EvalOnSelectorAllAsync<string[]>("iframe >> internal:control=enter-frame >> span", "ss => ss.map(s => s.textContent)");
        Assert.That(value, Is.EqualTo(new[] { "1", "2" }));
    }

    [UpstreamTest("SE-112 should not allow dangling enter-frame")]
    public async Task ShouldNotAllowDanglingEnterFrame(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.Locator("iframe >> internal:control=enter-frame");
        var error = await Catch(() => button.ClickAsync());
        Assert.That(error.Message, Does.Contain("Selector cannot end with"));
        Assert.That(error.Message, Does.Contain("iframe >> internal:control=enter-frame"));
    }

    [UpstreamTest("SE-113 should not allow leading enter-frame")]
    public async Task ShouldNotAllowLeadingEnterFrame(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var error = await Catch(() => page.WaitForSelectorAsync("internal:control=enter-frame >> button"));
        Assert.That(error.Message, Does.Contain("Selector cannot start with"));
    }

    [UpstreamTest("SE-114 should not allow capturing before enter-frame")]
    public async Task ShouldNotAllowCapturingBeforeEnterFrame(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.Locator("*css=iframe >> internal:control=enter-frame >> div");
        var error = await Catch(() => button.ClickAsync());
        Assert.That(error.Message, Does.Contain("Can not capture the selector before diving into the frame"));
    }

    [UpstreamTest("SE-115 should capture after the enter-frame")]
    public async Task ShouldCaptureAfterTheEnterFrame(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var div = page.Locator("iframe >> internal:control=enter-frame >> *css=div >> button");
        Assert.That(await div.InnerHTMLAsync(), Does.Contain("<button>"));
    }

    [UpstreamTest("SE-116 should click in lazy iframe")]
    public async Task ShouldClickInLazyIframe(IPage page, Server server)
    {
        await page.RouteAsync("**/iframe.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = "<html><button>Hello iframe</button></html>",
                ContentType = "text/html",
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
        var button = page.Locator("iframe >> internal:control=enter-frame >> button");
        // .NET: const [, text] = await Promise.all([...]);
        Task<string> textPromise;
        await PromiseAll(
            button.ClickAsync(),
            textPromise = button.InnerTextAsync(),
            Expect(button).ToHaveTextAsync("Hello iframe"));
        var text = await textPromise;
        Assert.That(text, Is.EqualTo("Hello iframe"));
    }

    [UpstreamTest("SE-117 waitFor should survive frame reattach")]
    public async Task WaitForShouldSurviveFrameReattach(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.Locator("iframe >> internal:control=enter-frame >> button:has-text(\"Hello nested iframe\")");
        var promise = button.WaitForAsync();
        await page.Locator("iframe").EvaluateAsync("e => e.remove()");
        await page.EvaluateAsync(@"() => {
    const iframe = document.createElement('iframe');
    iframe.src = 'iframe-2.html';
    document.body.appendChild(iframe);
  }");
        await promise;
    }

    [UpstreamTest("SE-118 waitForSelector should survive frame reattach (handle)")]
    public async Task WaitForSelectorShouldSurviveFrameReattachHandle(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var body = await page.QuerySelectorAsync("body");
        var promise = body.WaitForSelectorAsync("iframe >> internal:control=enter-frame >> button:has-text(\"Hello nested iframe\")");
        await page.Locator("iframe").EvaluateAsync("e => e.remove()");
        await page.EvaluateAsync(@"() => {
    const iframe = document.createElement('iframe');
    iframe.src = 'iframe-2.html';
    document.body.appendChild(iframe);
  }");
        await promise;
    }

    [UpstreamTest("SE-119 waitForSelector should survive iframe navigation (handle)")]
    public async Task WaitForSelectorShouldSurviveIframeNavigationHandle(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var body = await page.QuerySelectorAsync("body");
        var promise = body.WaitForSelectorAsync("iframe >> internal:control=enter-frame >> button:has-text(\"Hello nested iframe\")");
        IgnoreErrors(page.Locator("iframe").EvaluateAsync("e => e.src = 'iframe-2.html'"));
        await promise;
    }

    [UpstreamTest("SE-120 click should survive frame reattach")]
    public async Task ClickShouldSurviveFrameReattach(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.Locator("iframe >> internal:control=enter-frame >> button:has-text(\"Hello nested iframe\")");
        var promise = button.ClickAsync();
        await page.Locator("iframe").EvaluateAsync("e => e.remove()");
        await page.EvaluateAsync(@"() => {
    const iframe = document.createElement('iframe');
    iframe.src = 'iframe-2.html';
    document.body.appendChild(iframe);
  }");
        await promise;
    }

    [UpstreamTest("SE-121 click should survive iframe navigation")]
    public async Task ClickShouldSurviveIframeNavigation(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.Locator("iframe >> internal:control=enter-frame >> button:has-text(\"Hello nested iframe\")");
        var promise = button.ClickAsync();
        IgnoreErrors(page.Locator("iframe").EvaluateAsync("e => e.src = 'iframe-2.html'"));
        await promise;
    }

    [UpstreamTest("SE-122 click should survive navigation")]
    public async Task ClickShouldSurviveNavigation(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.Prefix + "/iframe.html");
        var promise = page.ClickAsync("button:has-text(\"Hello nested iframe\")");
        await page.WaitForTimeoutAsync(100);
        await page.GotoAsync(server.Prefix + "/iframe-2.html");
        await promise;
    }

    [UpstreamTest("SE-123 should non work for non-frame")]
    public async Task ShouldNonWorkForNonFrame(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.SetContentAsync("<div></div>");
        var button = page.Locator("div >> internal:control=enter-frame >> button");
        var error = await Catch(() => button.WaitForAsync());
        Assert.That(error.Message, Does.Contain("<div></div>"));
        Assert.That(error.Message, Does.Contain("<iframe> was expected"));
    }

    [UpstreamTest("SE-124 should match in a descendant frame")]
    public async Task ShouldMatchInADescendantFrame(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        // The main frame has no <div>; only the iframe.html child frame does.
        var div = page.Locator("internal:control=any-frame >> div");
        await div.WaitForAsync();
        await Expect(div).ToHaveCountAsync(1);
        Assert.That(await div.InnerHTMLAsync(), Does.Contain("<button>Hello iframe</button>"));
    }

    [UpstreamTest("SE-125 should match in a deeply nested frame")]
    public async Task ShouldMatchInADeeplyNestedFrame(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        // The main frame has no <div>; only the iframe.html child frame does.
        var button = page.Locator("internal:control=any-frame >> button[tag=\"iframe2\"]");
        await button.WaitForAsync();
        await Expect(button).ToHaveCountAsync(1);
        Assert.That(await button.TextContentAsync(), Is.EqualTo("Hello nested iframe"));
    }

    [UpstreamTest("SE-126 should not match a chain across frames")]
    public async Task ShouldNotMatchAChainAcrossFrames(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        // The <div> lives in the iframe.html frame, while the button lives in the frame nested below it.
        await Expect(page.Locator("internal:control=any-frame >> div >> button[tag=\"iframe2\"]")).ToHaveCountAsync(0);
        await Expect(page.Locator("internal:control=any-frame >> div >> button")).ToHaveTextAsync("Hello iframe");
    }

    [UpstreamTest("SE-127 should match multiple elements")]
    public async Task ShouldMatchMultipleElements(IPage page, Server server)
    {
        await page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>", ContentType = "text/html" }));
        });
        await page.RouteAsync("**/a.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<div>one</div>", ContentType = "text/html" }));
        });
        await page.RouteAsync("**/b.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<span>two</span><span>three</span>", ContentType = "text/html" }));
        });
        await page.GotoAsync(server.EmptyPage);

        var texts = await page.EvalOnSelectorAllAsync<string[]>("internal:control=any-frame >> span", "els => els.map(e => e.textContent)");
        Assert.That(texts, Is.EqualTo(new[] { "two", "three" }));
    }

    [UpstreamTest("SE-128 should throw when matching elements in multiple frames")]
    public async Task ShouldThrowWhenMatchingElementsInMultipleFrames(IPage page, Server server)
    {
        await page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>", ContentType = "text/html" }));
        });
        await page.RouteAsync("**/a.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<div>one</div>", ContentType = "text/html" }));
        });
        await page.RouteAsync("**/b.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<div>two</div>", ContentType = "text/html" }));
        });
        await page.GotoAsync(server.EmptyPage);

        // Make sure both child frames have their <div> before matching, otherwise resolution
        // may transiently collapse to a single frame.
        await Expect(() => Assert.That(page.Frames.Count, Is.EqualTo(3))).ToPassAsync();
        foreach (var frame in page.Frames)
        {
            if (frame != page.MainFrame)
            {
                await frame.WaitForSelectorAsync("div");
            }
        }

        var error = await Catch(() => page.Locator("internal:control=any-frame >> div").InnerHTMLAsync());
        Assert.That(error.Message, Does.Contain("frameLocator() matched elements in multiple frames"));
    }

    [UpstreamTest("SE-129 should not allow any-frame in the middle of a selector")]
    public async Task ShouldNotAllowAnyFrameInTheMiddleOfASelector(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var error = await Catch(() => page.Locator("iframe >> internal:control=any-frame >> div").WaitForAsync());
        Assert.That(error.Message, Does.Contain("\"any-frame\" is only allowed as the first selector token"));
    }

    [UpstreamTest("SE-130 should allow entering frames from any frame")]
    public async Task ShouldAllowEnteringFramesFromAnyFrame(IPage page, Server server)
    {
        await RouteIframe(page);
        await page.GotoAsync(server.EmptyPage);
        var button = page.Locator("internal:control=any-frame >> iframe[src=\"iframe-2.html\"] >> internal:control=enter-frame >> button");
        await button.WaitForAsync();
        Assert.That(await button.InnerTextAsync(), Is.EqualTo("Hello nested iframe"));
    }

    [UpstreamTest("SE-131 should not allow any-frame after entering a frame")]
    public async Task ShouldNotAllowAnyFrameAfterEnteringAFrame(IPage page)
    {
        var error = await Catch(() => page.Locator("iframe >> internal:control=enter-frame >> internal:control=any-frame >> button").CountAsync());
        Assert.That(error.Message, Does.Contain("\"any-frame\" is only allowed as the first selector token"));
    }

    [UpstreamTest("SE-132 should not allow dangling enter-frame after any-frame")]
    public async Task ShouldNotAllowDanglingEnterFrameAfterAnyFrame(IPage page)
    {
        var error = await Catch(() => page.Locator("internal:control=any-frame >> iframe >> internal:control=enter-frame").CountAsync());
        Assert.That(error.Message, Does.Contain("Selector cannot end with entering frame"));
    }
}
