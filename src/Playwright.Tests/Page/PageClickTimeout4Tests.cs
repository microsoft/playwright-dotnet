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

/// <summary>Port of tests/page/page-click-timeout-4.spec.ts.</summary>
public class PageClickTimeout4Tests : UpstreamPageTest
{
    [UpstreamTest("CL-120 should timeout waiting for stable position")]
    [LocalFixme("dotnet: the message is 'Timeout 3000ms exceeded.', without the 'elementHandle.click: ' prefix of the call")]
    public async Task ShouldTimeoutWaitingForStablePosition()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await button.EvaluateAsync(@"button => {
    button.style.transition = 'margin 5s linear 0s';
    button.style.marginLeft = '200px';
  }");
        // rafraf for Firefox to kick in the animation.
        await Rafraf(Page);
        var error = await Catch(() => button.ClickAsync(new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("elementHandle.click: Timeout 3000ms exceeded."));
        Assert.That(error.Message, Does.Contain("waiting for element to be visible, enabled and stable"));
        Assert.That(error.Message, Does.Contain("element is not stable"));
        Assert.That(error.Message, Does.Contain("retrying click action"));
    }

    [UpstreamTest("CL-122 should fail to click the button behind a large header after scrolling around")]
    public async Task ShouldFailToClickTheButtonBehindALargeHeaderAfterScrollingAround()
    {
        Info.Skip(IsAndroid, "Different viewport size");
        await Page.SetViewportSizeAsync(500, 240);
        await Page.SetContentAsync(@"
    <style>
    * {
      padding: 0;
      margin: 0;
    }
    li {
      height: 80px;
      border: 1px solid black;
    }
    ol {
      padding-top: 160px;
    }
    div.fixed {
      position: fixed;
      z-index: 1001;
      width: 100%;
      background: rgba(255, 0, 0, 0.2);
      height: 2000px;
    }
    </style>

    <div class=fixed></div>

    <ol>
    <li>hi1</li><li>hi2</li><li>hi3</li><li>hi4</li><li>hi5</li><li>hi6</li><li>hi7</li><li>hi8</li>
    <li id=target onclick=""window.__clicked = true"">hi9</li>
    <li>hi10</li><li>hi11</li><li>hi12</li><li>hi13</li><li id=li14>hi14</li>
    </ol>

    <script>
      window.scrollTops = [];
      window.addEventListener('scroll', () => {
        window.scrollTops.push(window.scrollY);
      });
    </script>
  ");
        await Page.EvalOnSelectorAsync("#li14", "e => e.scrollIntoView()");
        var error = await Catch(() => Page.ClickAsync("#target", new() { Timeout = 3500 }));
        Assert.That(error.Message, Does.Contain("<div class=\"fixed\"></div> intercepts pointer events"));
        Assert.That(await Page.EvaluateAsync<object>("() => window['__clicked']"), Is.Null);
        var scrollTops = await Page.EvaluateAsync<double[]>("() => window['scrollTops']");
        var distinct = new HashSet<double>(scrollTops);
        Assert.That(distinct.Count, Is.GreaterThan(2));  // At least three different scroll positions.
    }
}
