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

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/eval-on-selector-all.spec.ts.</summary>
public class EvalOnSelectorAllTests
{
    [UpstreamTest("ES-027 should work with css selector")]
    public async Task ShouldWorkWithCssSelector(IPage page, Server server)
    {
        await page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
        var divsCount = await page.EvalOnSelectorAllAsync<int>("css=div", "divs => divs.length");
        Assert.That(divsCount, Is.EqualTo(3));
    }

    [UpstreamTest("ES-028 should work with text selector")]
    public async Task ShouldWorkWithTextSelector(IPage page, Server server)
    {
        await page.SetContentAsync("<div>hello</div><div>beautiful</div><div>beautiful</div><div>world!</div>");
        var divsCount = await page.EvalOnSelectorAllAsync<int>("text=\"beautiful\"", "divs => divs.length");
        Assert.That(divsCount, Is.EqualTo(2));
    }

    [UpstreamTest("ES-029 should work with xpath selector")]
    public async Task ShouldWorkWithXpathSelector(IPage page, Server server)
    {
        await page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
        var divsCount = await page.EvalOnSelectorAllAsync<int>("xpath=/html/body/div", "divs => divs.length");
        Assert.That(divsCount, Is.EqualTo(3));
    }

    [UpstreamTest("ES-030 should auto-detect css selector")]
    public async Task ShouldAutoDetectCssSelector(IPage page, Server server)
    {
        await page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
        var divsCount = await page.EvalOnSelectorAllAsync<int>("div", "divs => divs.length");
        Assert.That(divsCount, Is.EqualTo(3));
    }

    [UpstreamTest("ES-031 should support >> syntax")]
    public async Task ShouldSupportSyntax(IPage page, Server server)
    {
        await page.SetContentAsync("<div><span>hello</span></div><div>beautiful</div><div><span>wo</span><span>rld!</span></div><span>Not this one</span>");
        var spansCount = await page.EvalOnSelectorAllAsync<int>("css=div >> css=span", "spans => spans.length");
        Assert.That(spansCount, Is.EqualTo(3));
    }

    [UpstreamTest("ES-032 should support * capture")]
    public async Task ShouldSupportCapture(IPage page, Server server)
    {
        await page.SetContentAsync("<section><div><span>a</span></div></section><section><div><span>b</span></div></section>");
        Assert.That(await page.EvalOnSelectorAllAsync<int>("*css=div >> \"b\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("section >> *css=div >> \"b\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("section >> *", "els => els.length"), Is.EqualTo(4));

        await page.SetContentAsync("<section><div><span>a</span><span>a</span></div></section>");
        Assert.That(await page.EvalOnSelectorAllAsync<int>("*css=div >> \"a\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("section >> *css=div >> \"a\"", "els => els.length"), Is.EqualTo(1));

        await page.SetContentAsync("<div><span>a</span></div><div><span>a</span></div><section><div><span>a</span></div></section>");
        Assert.That(await page.EvalOnSelectorAllAsync<int>("*css=div >> \"a\"", "els => els.length"), Is.EqualTo(3));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("section >> *css=div >> \"a\"", "els => els.length"), Is.EqualTo(1));
    }

    [UpstreamTest("ES-033 should support * capture when multiple paths match")]
    public async Task ShouldSupportCaptureWhenMultiplePathsMatch(IPage page, Server server)
    {
        await page.SetContentAsync("<div><div><span></span></div></div><div></div>");
        Assert.That(await page.EvalOnSelectorAllAsync<int>("*css=div >> span", "els => els.length"), Is.EqualTo(2));
        await page.SetContentAsync("<div><div><span></span></div><span></span><span></span></div><div></div>");
        Assert.That(await page.EvalOnSelectorAllAsync<int>("*css=div >> span", "els => els.length"), Is.EqualTo(2));
    }

    [UpstreamTest("ES-034 should return complex values")]
    public async Task ShouldReturnComplexValues(IPage page, Server server)
    {
        await page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
        var texts = await page.EvalOnSelectorAllAsync<string[]>("css=div", "divs => divs.map(div => div.textContent)");
        Assert.That(texts, Is.EqualTo(new[] { "hello", "beautiful", "world!" }));
    }

    [UpstreamTest("ES-035 should work with bogus Array.from")]
    public async Task ShouldWorkWithBogusArrayFrom(IPage page, Server server)
    {
        await page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
        await page.EvaluateAsync(@"() => {
    Array.from = () => [];
  }");
        var divsCount = await page.EvalOnSelectorAllAsync<int>("css=div", "divs => divs.length");
        Assert.That(divsCount, Is.EqualTo(3));
    }
}
