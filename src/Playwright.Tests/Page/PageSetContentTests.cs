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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-set-content.spec.ts.</summary>
public class PageSetContentTests : UpstreamPageTest
{
    private const string expectedOutput = "<html><head></head><body><div>hello</div></body></html>";

    [UpstreamTest("PA-020 should work @smoke")]
    public async Task ShouldWork()
    {
        await Page.SetContentAsync("<div>hello</div>");
        var result = await Page.ContentAsync();
        Assert.That(result, Is.EqualTo(expectedOutput));
    }

    [UpstreamTest("PA-021 should work with domcontentloaded")]
    public async Task ShouldWorkWithDomcontentloaded()
    {
        await Page.SetContentAsync("<div>hello</div>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var result = await Page.ContentAsync();
        Assert.That(result, Is.EqualTo(expectedOutput));
    }

    [UpstreamTest("PA-022 should work with commit")]
    public async Task ShouldWorkWithCommit()
    {
        await Page.SetContentAsync("<div>hello</div>", new() { WaitUntil = WaitUntilState.Commit });
        var result = await Page.ContentAsync();
        Assert.That(result, Is.EqualTo(expectedOutput));
    }

    [UpstreamTest("PA-023 should work with doctype")]
    public async Task ShouldWorkWithDoctype()
    {
        const string doctype = "<!DOCTYPE html>";
        await Page.SetContentAsync($"{doctype}<div>hello</div>");
        var result = await Page.ContentAsync();
        Assert.That(result, Is.EqualTo($"{doctype}{expectedOutput}"));
    }

    [UpstreamTest("PA-024 should work with HTML 4 doctype")]
    public async Task ShouldWorkWithHTML4Doctype()
    {
        const string doctype = "<!DOCTYPE html PUBLIC \"-//W3C//DTD HTML 4.01//EN\" " +
            "\"http://www.w3.org/TR/html4/strict.dtd\">";
        await Page.SetContentAsync($"{doctype}<div>hello</div>");
        var result = await Page.ContentAsync();
        Assert.That(result, Is.EqualTo($"{doctype}{expectedOutput}"));
    }

    [UpstreamTest("PA-025 should include shadow roots")]
    public async Task ShouldIncludeShadowRoots()
    {
        const string html = "<!DOCTYPE html><html lang=\"en\"><head></head><body><div id=\"host\"><template shadowrootmode=\"open\"><div id=\"nested\"><template shadowrootmode=\"open\"><span>nested</span></template><slot></slot></div></template><span>light</span></div><div id=\"closed\"></div></body></html>";
        // Closed shadow roots are not accessible from script and are never serialized.
        await Page.SetContentAsync(ReplaceFirst(html, "<div id=\"closed\">", "<div id=\"closed\"><template shadowrootmode=\"closed\"><span>closed</span></template>"));
        Assert.That(await Page.ContentAsync(), Is.EqualTo("<!DOCTYPE html><html lang=\"en\"><head></head><body><div id=\"host\"><span>light</span></div><div id=\"closed\"></div></body></html>"));
        Assert.That(await Page.ContentAsync(new() { IncludeShadow = true }), Is.EqualTo(html));
    }

    [UpstreamTest("PA-026 should respect timeout")]
    public async Task ShouldRespectTimeout()
    {
        const string imgPath = "/img.png";
        // stall for image
        Server.SetRoute(imgPath, (req, res) => { });
        Exception error = null;
        error = await Catch(() => Page.SetContentAsync($"<img src=\"{Server.Prefix + imgPath}\"></img>", new() { Timeout = 1 }));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("PA-027 should respect default navigation timeout")]
    [LocalFixme("dotnet: Frame.SetContentAsync takes the default timeout and not the default navigation timeout as upstream does, so it does not time out after SetDefaultNavigationTimeout(1)")]
    public async Task ShouldRespectDefaultNavigationTimeout()
    {
        Page.SetDefaultNavigationTimeout(1);
        const string imgPath = "/img.png";
        // stall for image
        Server.SetRoute(imgPath, (req, res) => { });
        var error = await Catch(() => Page.SetContentAsync($"<img src=\"{Server.Prefix + imgPath}\"></img>"));
        Assert.That(error.Message, Does.Contain("page.setContent: Timeout 1ms exceeded."));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("PA-028 should await resources to load")]
    public async Task ShouldAwaitResourcesToLoad()
    {
        const string imgPath = "/img.png";
        ServerResponse imgResponse = null;
        Server.SetRoute(imgPath, (req, res) => imgResponse = res);
        var loaded = false;
        async Task SetContentThenLoaded()
        {
            await Page.SetContentAsync($"<img src=\"{Server.Prefix + imgPath}\"></img>");
            loaded = true;
        }
        var contentPromise = SetContentThenLoaded();
        await Server.WaitForRequest(imgPath);
        Assert.That(loaded, Is.EqualTo(false));
        imgResponse.End();
        await contentPromise;
    }

    [UpstreamTest("PA-029 should work fast enough")]
    public async Task ShouldWorkFastEnough()
    {
        for (var i = 0; i < 20; ++i)
        {
            await Page.SetContentAsync("<div>yo</div>");
        }
    }

    [UpstreamTest("PA-030 should work with tricky content")]
    public async Task ShouldWorkWithTrickyContent()
    {
        await Page.SetContentAsync("<div>hello world</div>" + "\u007F");
        Assert.That(await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"), Is.EqualTo("hello world"));
    }

    [UpstreamTest("PA-031 should work with accents")]
    public async Task ShouldWorkWithAccents()
    {
        await Page.SetContentAsync("<div>aberración</div>");
        Assert.That(await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"), Is.EqualTo("aberración"));
    }

    [UpstreamTest("PA-032 should work with emojis")]
    public async Task ShouldWorkWithEmojis()
    {
        await Page.SetContentAsync("<div>🐥</div>");
        Assert.That(await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"), Is.EqualTo("🐥"));
    }

    [UpstreamTest("PA-033 should work with newline")]
    public async Task ShouldWorkWithNewline()
    {
        await Page.SetContentAsync("<div>\n</div>");
        Assert.That(await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"), Is.EqualTo("\n"));
    }

    [UpstreamTest("PA-034 content() should throw nice error during navigation")]
    public async Task ContentShouldThrowNiceErrorDuringNavigation()
    {
        for (var timeout = 0; timeout < 200; timeout += 20)
        {
            await Page.SetContentAsync("<div>hello</div>");
            var promise = Page.GotoAsync(Server.EmptyPage);
            await Page.WaitForTimeoutAsync(timeout);
            // .NET: page.content().catch(e => e) is the content or the error, in one object.
            async Task<object> ContentOrError()
            {
                try
                {
                    return await Page.ContentAsync();
                }
                catch (Exception e)
                {
                    return e;
                }
            }
            var (contentOrError, _) = await PromiseAll(
                ContentOrError(),
                promise);
            const string emptyOutput = "<html><head></head><body></body></html>";
            if (!Equals(contentOrError, expectedOutput) && !Equals(contentOrError, emptyOutput))
            {
                Assert.That((contentOrError as Exception)?.Message, Does.Contain("Unable to retrieve content because the page is navigating and changing the content."));
            }
        }
    }

    [UpstreamTest("PA-035 should return empty content there is no iframe src")]
    public async Task ShouldReturnEmptyContentThereIsNoIframeSrc()
    {
        Info.Fixme(BrowserName == "webkit", "there is no utility context");
        await Page.SetContentAsync("<iframe src=\"javascript:console.log(1)\"></iframe>");
        Assert.That(Page.Frames.Count, Is.EqualTo(2));
        Assert.That(await Page.Frames[1].ContentAsync(), Is.EqualTo("<html><head></head><body></body></html>"));
    }
}
