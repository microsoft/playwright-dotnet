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
public class PageNavigationTests
{
    [UpstreamTest("NA-067 should work with _blank target")]
    public async Task ShouldWorkWithBlankTarget(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.End($"<a href=\"{server.EmptyPage}\" target=\"_blank\">Click me</a>");
        });
        await page.GotoAsync(server.EmptyPage);
        await page.ClickAsync("\"Click me\"");
    }

    [UpstreamTest("NA-068 should work with cross-process _blank target")]
    public async Task ShouldWorkWithCrossProcessBlankTarget(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.End($"<a href=\"{server.CrossProcessPrefix}/empty.html\" target=\"_blank\">Click me</a>");
        });
        await page.GotoAsync(server.EmptyPage);
        await page.ClickAsync("\"Click me\"");
    }

    [UpstreamTest("NA-069 should work with _blank target in form")]
    public async Task ShouldWorkWithBlankTargetInForm(IPage page, Server server)
    {
        server.SetRoute("/done.html?", (req, res) =>
        {
            res.End("Done");
        });
        server.SetRoute("/done.html", (req, res) =>
        {
            res.End("Done");
        });
        await page.GotoAsync(server.EmptyPage);

        await page.SetContentAsync(@"<form target=""_blank"" action=""done.html"" >
    <input type=""submit"" value=""Click me"">
  </form>");
        await PromiseAll(
            page.WaitForPopupAsync(),
            page.ClickAsync("\"Click me\""));

        await page.SetContentAsync(@"<form target=""_blank"" action=""done.html"" method=""post"">
    <input type=""submit"" value=""Click me"">
  </form>");
        await PromiseAll(
            page.WaitForPopupAsync(),
            page.ClickAsync("\"Click me\""));
    }

    [UpstreamTest("NA-070 should not throw TargetClosedException on cross-origin redirect after click")]
    public async Task ShouldNotThrowTargetClosedExceptionOnCrossOriginRedirectAfterClick(IPage page, Server server)
    {
        server.SetRoute("/target.html", (req, res) => res.End("<title>final page</title>"));
        server.SetRedirect("/redirect", server.CrossProcessPrefix + "/target.html");
        await page.GotoAsync(server.Prefix + "/empty.html");
        await page.SetContentAsync($@"
    <form action=""{server.Prefix}/redirect"" method=""POST"">
      <button type=""submit"">Submit</button>
    </form>
  ");
        await PromiseAll(
            page.WaitForNavigationAsync(),
            // triggers POST -> 302 -> cross-origin
            page.ClickAsync("button"));
        await Expect(page).ToHaveURLAsync(server.CrossProcessPrefix + "/target.html");
        await Expect(page).ToHaveTitleAsync("final page");
    }
}
