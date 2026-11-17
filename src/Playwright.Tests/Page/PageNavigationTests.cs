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

/// <summary>Port of tests/page/page-navigation.spec.ts.</summary>
public class PageNavigationTests : UpstreamPageTest
{
    [UpstreamTest("NA-067 should work with _blank target")]
    public async Task ShouldWorkWithBlankTarget()
    {
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.End($"<a href=\"{Server.EmptyPage}\" target=\"_blank\">Click me</a>");
        });
        await Page.GotoAsync(Server.EmptyPage);
        await Page.ClickAsync("\"Click me\"");
    }

    [UpstreamTest("NA-068 should work with cross-process _blank target")]
    public async Task ShouldWorkWithCrossProcessBlankTarget()
    {
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.End($"<a href=\"{Server.CrossProcessPrefix}/empty.html\" target=\"_blank\">Click me</a>");
        });
        await Page.GotoAsync(Server.EmptyPage);
        await Page.ClickAsync("\"Click me\"");
    }

    [UpstreamTest("NA-069 should work with _blank target in form")]
    public async Task ShouldWorkWithBlankTargetInForm()
    {
        Server.SetRoute("/done.html?", (req, res) =>
        {
            res.End("Done");
        });
        Server.SetRoute("/done.html", (req, res) =>
        {
            res.End("Done");
        });
        await Page.GotoAsync(Server.EmptyPage);

        await Page.SetContentAsync(@"<form target=""_blank"" action=""done.html"" >
    <input type=""submit"" value=""Click me"">
  </form>");
        await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.ClickAsync("\"Click me\""));

        await Page.SetContentAsync(@"<form target=""_blank"" action=""done.html"" method=""post"">
    <input type=""submit"" value=""Click me"">
  </form>");
        await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.ClickAsync("\"Click me\""));
    }

    [UpstreamTest("NA-070 should not throw TargetClosedException on cross-origin redirect after click")]
    public async Task ShouldNotThrowTargetClosedExceptionOnCrossOriginRedirectAfterClick()
    {
        Server.SetRoute("/target.html", (req, res) => res.End("<title>final page</title>"));
        Server.SetRedirect("/redirect", Server.CrossProcessPrefix + "/target.html");
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        await Page.SetContentAsync($@"
    <form action=""{Server.Prefix}/redirect"" method=""POST"">
      <button type=""submit"">Submit</button>
    </form>
  ");
        await PromiseAll(
            Page.WaitForNavigationAsync(),
            // triggers POST -> 302 -> cross-origin
            Page.ClickAsync("button"));
        await Expect(Page).ToHaveURLAsync(Server.CrossProcessPrefix + "/target.html");
        await Expect(Page).ToHaveTitleAsync("final page");
    }
}
