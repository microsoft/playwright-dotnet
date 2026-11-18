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
public class PageBasicTests : UpstreamPageTest
{
    [UpstreamTest("PA-001 should fire load when expected")]
    public async Task ShouldFireLoadWhenExpected()
    {
        await PromiseAll(
            Page.GotoAsync("about:blank"),
            Once<IPage>(h => Page.Load += h, h => Page.Load -= h));
    }

    [UpstreamTest("PA-002 async stacks should work")]
    public async Task AsyncStacksShouldWork()
    {
        Server.SetRoute("/empty.html", (req, res) =>
        {
            req.Socket.End();
        });
        Exception error = null;
        error = await Catch(() => Page.GotoAsync(Server.EmptyPage));
        Assert.That(error, Is.Not.Null);
        // .NET: expect(error.stack).toContain(__filename) is left out: a .NET stack trace has the frames of the library.
    }

    [UpstreamTest("PA-003 should provide access to the opener page")]
    public async Task ShouldProvideAccessToTheOpenerPage()
    {
        var (popup, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open('about:blank')"));
        var opener = await popup.OpenerAsync();
        Assert.That(opener, Is.SameAs(Page));
    }

    [UpstreamTest("PA-004 should fire domcontentloaded when expected")]
    public async Task ShouldFireDomcontentloadedWhenExpected()
    {
        var navigatedPromise = Page.GotoAsync("about:blank");
        await Once<IPage>(h => Page.DOMContentLoaded += h, h => Page.DOMContentLoaded -= h);
        await navigatedPromise;
    }

    [UpstreamTest("PA-005 should pass self as argument to domcontentloaded event")]
    public async Task ShouldPassSelfAsArgumentToDomcontentloadedEvent()
    {
        var (eventArg, _) = await PromiseAll(
            Once<IPage>(h => Page.DOMContentLoaded += h, h => Page.DOMContentLoaded -= h),
            Page.GotoAsync("about:blank"));
        Assert.That(eventArg, Is.SameAs(Page));
    }

    [UpstreamTest("PA-006 should pass self as argument to load event")]
    public async Task ShouldPassSelfAsArgumentToLoadEvent()
    {
        var (eventArg, _) = await PromiseAll(
            Once<IPage>(h => Page.Load += h, h => Page.Load -= h),
            Page.GotoAsync("about:blank"));
        Assert.That(eventArg, Is.SameAs(Page));
    }

    [UpstreamTest("PA-007 page.url should work")]
    public async Task PageUrlShouldWork()
    {
        Assert.That(Page.Url, Is.EqualTo("about:blank"));
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("PA-008 page.url should include hashes")]
    public async Task PageUrlShouldIncludeHashes()
    {
        await Page.GotoAsync(Server.EmptyPage + "#hash");
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage + "#hash"));
        await Page.EvaluateAsync(@"() => {
    window.location.hash = 'dynamic';
  }");
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage + "#dynamic"));
    }

    [UpstreamTest("PA-009 page.title should return the page title")]
    public async Task PageTitleShouldReturnThePageTitle()
    {
        await Page.GotoAsync(Server.Prefix + "/title.html");
        Assert.That(await Page.TitleAsync(), Is.EqualTo("Woof-Woof"));
    }

    [UpstreamTest("PA-010 page.title should not throw during navigation")]
    public async Task PageTitleShouldNotThrowDuringNavigation()
    {
        await Page.SetContentAsync("<title>hello</title>");
        var promise = Page.GotoAsync(Server.Prefix + "/title.html");
        // .NET: page.title().catch(e => e), the title or the error, is a local function that is called in its place.
        async Task<object> TitleOrError()
        {
            try
            {
                return await Page.TitleAsync();
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
        await Expect(Page).ToHaveTitleAsync("Woof-Woof");
    }

    [UpstreamTest("PA-011 page.close should work with window.close")]
    public async Task PageCloseShouldWorkWithWindowClose()
    {
        var newPagePromise = Page.WaitForPopupAsync();
        await Page.EvaluateAsync("() => window['newPage'] = window.open('about:blank')");
        var newPage = await newPagePromise;
        var closedPromise = Once<IPage>(h => newPage.Close += h, h => newPage.Close -= h);
        await Page.EvaluateAsync("() => window['newPage'].close()");
        await closedPromise;
    }

    [UpstreamTest("PA-012 page.frame should respect name")]
    public async Task PageFrameShouldRespectName()
    {
        Info.Skip(IsBidi, "page.frame({ name }) is racy with BiDi");

        await Page.SetContentAsync("<iframe name=target></iframe>");
        Assert.That(Page.Frame("bogus"), Is.Null);
        var frame = Page.Frame("target");
        Assert.That(frame, Is.Not.Null);
        Assert.That(frame == Page.MainFrame.ChildFrames[0], Is.True);
    }

    [UpstreamTest("PA-013 page.frame should respect url")]
    public async Task PageFrameShouldRespectUrl()
    {
        await Page.SetContentAsync($"<iframe src=\"{Server.EmptyPage}\"></iframe>");
        Assert.That(Page.FrameByUrl(new Regex("bogus")), Is.Null);
        Assert.That(Page.FrameByUrl(new Regex("empty")).Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("PA-014 should have sane user agent")]
    public async Task ShouldHaveSaneUserAgent()
    {
        Info.Skip(IsAndroid);
        Info.Skip(IsElectron);

        var userAgent = await Page.EvaluateAsync<string>("() => navigator.userAgent");
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
        if (BrowserName == "firefox")
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
            if (BrowserName == "chromium")
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
    public async Task PagePressShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.PressAsync("textarea", "a");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("a"));
    }

    [UpstreamTest("PA-016 page.press should work for Enter")]
    public async Task PagePressShouldWorkForEnter()
    {
        await Page.SetContentAsync("<input onkeypress=\"console.log('press')\"></input>");
        var messages = new List<IConsoleMessage>();
        Page.Console += (_, message) => messages.Add(message);
        await Page.PressAsync("input", "Enter");
        Assert.That(messages[0].Text, Is.EqualTo("press"));
    }

    [UpstreamTest("PA-017 frame.press should work")]
    public async Task FramePressShouldWork()
    {
        await Page.SetContentAsync($"<iframe name=inner src=\"{Server.Prefix}/input/textarea.html\"></iframe>");
        var frame = Page.Frame("inner");
        await frame.PressAsync("textarea", "a");
        Assert.That(await frame.EvaluateAsync<string>("() => document.querySelector('textarea').value"), Is.EqualTo("a"));
    }

    [UpstreamTest("PA-018 has navigator.webdriver set to true")]
    public async Task HasNavigatorWebdriverSetToTrue()
    {
        Info.Fixme(IsAndroid);

        Assert.That(await Page.EvaluateAsync<bool>("() => navigator.webdriver"), Is.EqualTo(true));
    }

    [UpstreamTest("PA-057 should prefix the error message with the api name")]
    [LocalFixme("dotnet: the message is 'Timeout 1ms exceeded.', without the 'page.goto: ' prefix of the call")]
    public async Task ShouldPrefixTheErrorMessageWithTheApiName()
    {
        var error = await Catch(() => Page.GotoAsync(Server.Prefix + "/empty.html", new() { Timeout = 1 }));
        Assert.That(error.Message, Does.Contain("page.goto: Timeout 1ms exceeded."));
    }
}
