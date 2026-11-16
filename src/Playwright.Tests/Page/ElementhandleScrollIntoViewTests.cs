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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/elementhandle-scroll-into-view.spec.ts.</summary>
public class ElementhandleScrollIntoViewTests
{
    [UpstreamTest("EH-093 should work @smoke")]
    public async Task ShouldWork(IPage page, Server server, bool isAndroid)
    {
        It.Fixme(isAndroid);

        await page.GotoAsync(server.Prefix + "/offscreenbuttons.html");
        for (var i = 0; i < 11; ++i)
        {
            var button = await page.QuerySelectorAsync("#btn" + i);
            var before = await button.EvaluateAsync<double>(@"button => {
      return button.getBoundingClientRect().right - window.innerWidth;
    }");
            Assert.That(before, Is.EqualTo(10 * i));
            await button.ScrollIntoViewIfNeededAsync();
            var after = await button.EvaluateAsync<double>(@"button => {
      return button.getBoundingClientRect().right - window.innerWidth;
    }");
            Assert.That(after <= 0, Is.EqualTo(true));
            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        }
    }

    [UpstreamTest("EH-094 should throw for detached element")]
    public async Task ShouldThrowForDetachedElement(IPage page, Server server)
    {
        await page.SetContentAsync("<div>Hello</div>");
        var div = await page.QuerySelectorAsync("div");
        await div.EvaluateAsync("div => div.remove()");
        var error = await Catch(() => div.ScrollIntoViewIfNeededAsync());
        Assert.That(error.Message, Does.Contain("Element is not attached to the DOM"));
    }

    private static async Task TestWaiting(IPage page, string after)
    {
        var div = await page.QuerySelectorAsync("div");
        var done = false;
        // .NET: const promise = div.scrollIntoViewIfNeeded().then(() => done = true);
        async Task Promise()
        {
            await div.ScrollIntoViewIfNeededAsync();
            done = true;
        }
        var promise = Promise();
        await page.WaitForTimeoutAsync(1000);
        Assert.That(done, Is.EqualTo(false));
        await div.EvaluateAsync(after);
        await promise;
        Assert.That(done, Is.EqualTo(true));
    }

    [UpstreamTest("EH-095 should wait for display:none to become visible")]
    public async Task ShouldWaitForDisplayNoneToBecomeVisible(IPage page, Server server)
    {
        await page.SetContentAsync("<div style=\"display:none\">Hello</div>");
        await TestWaiting(page, "div => div.style.display = 'block'");
    }

    [UpstreamTest("EH-096 should scroll display:contents into view")]
    public async Task ShouldScrollDisplayContentsIntoView(IPage page, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "chromium" && browserMajorVersion < 105, "Needs https://chromium-review.googlesource.com/c/chromium/src/+/3758670");

        await page.SetContentAsync(@"
    <style>
      html, body { margin: 0; padding: 0; width: 100%; height: 100%; }
      ::-webkit-scrollbar { display: none; }
      * { scrollbar-width: none; }
    </style>
    <div id=container style=""width:200px;height:200px;overflow:scroll;border:1px solid black;"">
      <div style=""margin-top:500px;background:red;"">
        <div style=""height:50px;width:100px;background:cyan;"">
          <div id=target style=""display:contents"">Hello</div>
        </div>
      <div>
    </div>
  ");
        var div = await page.QuerySelectorAsync("#target");
        await div.ScrollIntoViewIfNeededAsync();
        var scrollTop = await page.EvalOnSelectorAsync<double>("#container", "e => e.scrollTop");
        // On Android the value is not exact due to various scale conversions.
        Assert.That(Math.Abs(scrollTop - 350), Is.LessThan(1));
    }

    [UpstreamTest("EH-124 should scroll nested display:contents into view")]
    public async Task ShouldScrollNestedDisplayContentsIntoView(IPage page, string browserName, int browserMajorVersion)
    {
        await page.SetContentAsync(@"
    <style>
      html, body { margin: 0; padding: 0; width: 100%; height: 100%; }
      ::-webkit-scrollbar { display: none; }
      * { scrollbar-width: none; }
    </style>
    <div id=container style=""width:200px;height:200px;overflow:scroll;border:1px solid black;"">
      <div style=""margin-top:500px;background:red;"">
        <div id=outer style=""display:contents""><div style=""display:contents""><div id=target style=""height:50px;width:100px;background:cyan;"">Hello</div></div><div style=""height:2000px;""></div><div>Sibling</div></div>
      </div>
    </div>
  ");
        var div = await page.QuerySelectorAsync("#outer");
        await div.ScrollIntoViewIfNeededAsync();
        // The first box in the flattened subtree of #outer is #target, two levels down,
        // not the tall sibling. No whitespace text nodes inside #outer: in Firefox they
        // get boxes of their own, which would hide a fallback that only looks one level down.
        var targetIsVisible = await page.EvalOnSelectorAsync<bool>("#target", @"target => {
    const container = document.getElementById('container').getBoundingClientRect();
    const rect = target.getBoundingClientRect();
    return rect.top >= container.top && rect.bottom <= container.bottom;
  }");
        Assert.That(targetIsVisible, Is.EqualTo(true));
    }

    [UpstreamTest("EH-097 should work for visibility:hidden element")]
    public async Task ShouldWorkForVisibilityHiddenElement(IPage page)
    {
        await page.SetContentAsync("<div style=\"visibility:hidden\">Hello</div>");
        var div = await page.QuerySelectorAsync("div");
        await div.ScrollIntoViewIfNeededAsync();
    }

    [UpstreamTest("EH-098 should work for zero-sized element")]
    public async Task ShouldWorkForZeroSizedElement(IPage page)
    {
        await page.SetContentAsync("<div style=\"height:0\">Hello</div>");
        var div = await page.QuerySelectorAsync("div");
        await div.ScrollIntoViewIfNeededAsync();
    }

    [UpstreamTest("EH-099 should wait for nested display:none to become visible")]
    public async Task ShouldWaitForNestedDisplayNoneToBecomeVisible(IPage page, Server server)
    {
        await page.SetContentAsync("<span style=\"display:none\"><div>Hello</div></span>");
        await TestWaiting(page, "div => div.parentElement.style.display = 'block'");
    }

    [UpstreamTest("EH-100 should wait for element to stop moving")]
    public async Task ShouldWaitForElementToStopMoving(IPage page, Server server)
    {
        await page.SetContentAsync(@"
    <style>
      @keyframes move {
        from { margin-left: 0; }
        to { margin-left: 200px; }
      }
      div.animated {
        animation: 2s linear 0s infinite alternate move;
      }
    </style>
    <div class=animated>moving</div>
    ");
        await TestWaiting(page, "div => div.classList.remove('animated')");
    }

    [UpstreamTest("EH-101 should timeout waiting for visible")]
    public async Task ShouldTimeoutWaitingForVisible(IPage page, Server server)
    {
        await page.SetContentAsync("<div style=\"display:none\">Hello</div>");
        var div = await page.QuerySelectorAsync("div");
        var error = await Catch(() => div.ScrollIntoViewIfNeededAsync(new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("element is not visible"));
        Assert.That(error.Message, Does.Contain("retrying scroll into view action"));
    }
}
