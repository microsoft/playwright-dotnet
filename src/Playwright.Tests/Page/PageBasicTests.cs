/**
 * Copyright 2017 Google Inc. All rights reserved.
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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-basic.spec.ts.</summary>
public class PageBasicTests
{
    [UpstreamTest("PA-001 should fire load when expected")]
    public async Task ShouldFireLoadWhenExpected(IPage page)
    {
        await PromiseAll(
            page.GotoAsync("about:blank"),
            Once<IPage>(h => page.Load += h, h => page.Load -= h));
    }

    [UpstreamTest("PA-002 async stacks should work")]
    public async Task AsyncStacksShouldWork(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            req.Socket.End();
        });
        Exception error = null;
        error = await Catch(() => page.GotoAsync(server.EmptyPage));
        Assert.That(error, Is.Not.Null);
        // .NET: expect(error.stack).toContain(__filename) is left out: a .NET stack trace has the frames of the library.
    }

    [UpstreamTest("PA-003 should provide access to the opener page")]
    public async Task ShouldProvideAccessToTheOpenerPage(IPage page)
    {
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window.open('about:blank')"));
        var opener = await popup.OpenerAsync();
        Assert.That(opener, Is.SameAs(page));
    }

    [UpstreamTest("PA-004 should fire domcontentloaded when expected")]
    public async Task ShouldFireDomcontentloadedWhenExpected(IPage page)
    {
        var navigatedPromise = page.GotoAsync("about:blank");
        await Once<IPage>(h => page.DOMContentLoaded += h, h => page.DOMContentLoaded -= h);
        await navigatedPromise;
    }

    [UpstreamTest("PA-005 should pass self as argument to domcontentloaded event")]
    public async Task ShouldPassSelfAsArgumentToDomcontentloadedEvent(IPage page)
    {
        var (eventArg, _) = await PromiseAll(
            Once<IPage>(h => page.DOMContentLoaded += h, h => page.DOMContentLoaded -= h),
            page.GotoAsync("about:blank"));
        Assert.That(eventArg, Is.SameAs(page));
    }

    [UpstreamTest("PA-006 should pass self as argument to load event")]
    public async Task ShouldPassSelfAsArgumentToLoadEvent(IPage page)
    {
        var (eventArg, _) = await PromiseAll(
            Once<IPage>(h => page.Load += h, h => page.Load -= h),
            page.GotoAsync("about:blank"));
        Assert.That(eventArg, Is.SameAs(page));
    }

    [UpstreamTest("PA-007 page.url should work")]
    public async Task PageUrlShouldWork(IPage page, Server server)
    {
        Assert.That(page.Url, Is.EqualTo("about:blank"));
        await page.GotoAsync(server.EmptyPage);
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("PA-008 page.url should include hashes")]
    public async Task PageUrlShouldIncludeHashes(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage + "#hash");
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage + "#hash"));
        await page.EvaluateAsync(@"() => {
    window.location.hash = 'dynamic';
  }");
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage + "#dynamic"));
    }

    [UpstreamTest("PA-009 page.title should return the page title")]
    public async Task PageTitleShouldReturnThePageTitle(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/title.html");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Woof-Woof"));
    }

    [UpstreamTest("PA-010 page.title should not throw during navigation")]
    public async Task PageTitleShouldNotThrowDuringNavigation(IPage page, Server server)
    {
        await page.SetContentAsync("<title>hello</title>");
        var promise = page.GotoAsync(server.Prefix + "/title.html");
        // .NET: page.title().catch(e => e), the title or the error, is a local function that is called in its place.
        async Task<object> TitleOrError()
        {
            try
            {
                return await page.TitleAsync();
            }
            catch (Exception e)
            {
                return e;
            }
        }
        var (titleOrError, _) = await PromiseAll(
            TitleOrError(),
            promise);
        Assert.That(titleOrError, Is.InstanceOf<string>());
        Assert.That(titleOrError, Does.Match("^(hello|Loading http.*title.html||Woof-Woof)$"));
        await Expect(page).ToHaveTitleAsync("Woof-Woof");
    }

    [UpstreamTest("PA-011 page.close should work with window.close")]
    public async Task PageCloseShouldWorkWithWindowClose(IPage page)
    {
        var newPagePromise = page.WaitForPopupAsync();
        await page.EvaluateAsync("() => window['newPage'] = window.open('about:blank')");
        var newPage = await newPagePromise;
        var closedPromise = Once<IPage>(h => newPage.Close += h, h => newPage.Close -= h);
        await page.EvaluateAsync("() => window['newPage'].close()");
        await closedPromise;
    }

    [UpstreamTest("PA-012 page.frame should respect name")]
    public async Task PageFrameShouldRespectName(IPage page, bool isBidi)
    {
        It.Skip(isBidi, "page.frame({ name }) is racy with BiDi");

        await page.SetContentAsync("<iframe name=target></iframe>");
        Assert.That(page.Frame("bogus"), Is.Null);
        var frame = page.Frame("target");
        Assert.That(frame, Is.Not.Null);
        Assert.That(frame == page.MainFrame.ChildFrames[0], Is.True);
    }

    [UpstreamTest("PA-013 page.frame should respect url")]
    public async Task PageFrameShouldRespectUrl(IPage page, Server server)
    {
        await page.SetContentAsync($"<iframe src=\"{server.EmptyPage}\"></iframe>");
        Assert.That(page.FrameByUrl(new Regex("bogus")), Is.Null);
        Assert.That(page.FrameByUrl(new Regex("empty")).Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("PA-014 should have sane user agent")]
    public async Task ShouldHaveSaneUserAgent(IPage page, string browserName, bool isElectron, bool isAndroid)
    {
        It.Skip(isAndroid);
        It.Skip(isElectron);

        var userAgent = await page.EvaluateAsync<string>("() => navigator.userAgent");
        // .NET: an element that the destructured array does not have is undefined upstream, and null here.
        var parts = Regex.Split(userAgent, "[()]").Select(part => part.Trim()).ToArray();
        var part1 = parts.ElementAtOrDefault(0);
        /* part2 */
        var part3 = parts.ElementAtOrDefault(2);
        var part4 = parts.ElementAtOrDefault(3);
        var part5 = parts.ElementAtOrDefault(4);
        // First part is always "Mozilla/5.0"
        Assert.That(part1, Is.EqualTo("Mozilla/5.0"));
        // Second part in parenthesis is platform - ignore it.

        // Third part for Firefox is the last one and encodes engine and browser versions.
        if (browserName == "firefox")
        {
            var engineAndBrowser = part3.Split(' ');
            var (engine, browser) = (engineAndBrowser[0], engineAndBrowser[1]);
            Assert.That(engine.StartsWith("Gecko", StringComparison.Ordinal), Is.EqualTo(true));
            Assert.That(browser.StartsWith("Firefox", StringComparison.Ordinal), Is.EqualTo(true));
            Assert.That(part4, Is.Null);
            Assert.That(part5, Is.Null);
            return;
        }
        // For both options.CHROMIUM and options.WEBKIT, third part is the AppleWebKit version.
        Assert.That(part3.StartsWith("AppleWebKit/", StringComparison.Ordinal), Is.EqualTo(true));
        Assert.That(part4, Is.EqualTo("KHTML, like Gecko"));
        // .NET: a block, because C# does not let `engine` and `browser` be declared again next to the block of Firefox.
        {
            // 5th part encodes real browser name and engine version.
            var tokens = part5.Split(' ');
            var engine = tokens[0];
            var browser = tokens.FirstOrDefault(t => t.StartsWith("Safari/", StringComparison.Ordinal));
            Assert.That(browser?.StartsWith("Safari/", StringComparison.Ordinal), Is.EqualTo(true));
            if (browserName == "chromium")
            {
                Assert.That(engine.Contains("Chrome/", StringComparison.Ordinal), Is.EqualTo(true));
            }
            else
            {
                Assert.That(engine.StartsWith("Version/", StringComparison.Ordinal), Is.EqualTo(true));
            }
        }
    }

    [UpstreamTest("PA-015 page.press should work")]
    public async Task PagePressShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.PressAsync("textarea", "a");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("a"));
    }

    [UpstreamTest("PA-016 page.press should work for Enter")]
    public async Task PagePressShouldWorkForEnter(IPage page)
    {
        await page.SetContentAsync("<input onkeypress=\"console.log('press')\"></input>");
        var messages = new List<IConsoleMessage>();
        page.Console += (_, message) => messages.Add(message);
        await page.PressAsync("input", "Enter");
        Assert.That(messages[0].Text, Is.EqualTo("press"));
    }

    [UpstreamTest("PA-017 frame.press should work")]
    public async Task FramePressShouldWork(IPage page, Server server)
    {
        await page.SetContentAsync($"<iframe name=inner src=\"{server.Prefix}/input/textarea.html\"></iframe>");
        var frame = page.Frame("inner");
        await frame.PressAsync("textarea", "a");
        Assert.That(await frame.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("a"));
    }

    [UpstreamTest("PA-018 has navigator.webdriver set to true")]
    public async Task HasNavigatorWebdriverSetToTrue(IPage page, bool isAndroid)
    {
        It.Fixme(isAndroid);

        Assert.That(await page.EvaluateAsync<bool>("() => navigator.webdriver"), Is.EqualTo(true));
    }
}
