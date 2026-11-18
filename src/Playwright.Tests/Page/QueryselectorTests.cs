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

/// <summary>Port of tests/page/queryselector.spec.ts.</summary>
public class QueryselectorTests : UpstreamPageTest
{
    [UpstreamTest("QS-002 should query existing element with css selector @smoke")]
    public async Task ShouldQueryExistingElementWithCssSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("css=section");
        Assert.That(element, Is.Not.Null);
    }

    [UpstreamTest("QS-003 should query existing element with text selector")]
    public async Task ShouldQueryExistingElementWithTextSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("text=\"test\"");
        Assert.That(element, Is.Not.Null);
    }

    [UpstreamTest("QS-004 should query existing element with xpath selector")]
    public async Task ShouldQueryExistingElementWithXpathSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("xpath=/html/body/section");
        Assert.That(element, Is.Not.Null);
    }

    [UpstreamTest("QS-005 should return null for non-existing element")]
    public async Task ShouldReturnNullForNonExistingElement()
    {
        var element = await Page.QuerySelectorAsync("non-existing-element");
        Assert.That(element, Is.Null);
    }

    [UpstreamTest("QS-006 should auto-detect xpath selector")]
    public async Task ShouldAutoDetectXpathSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("//html/body/section");
        Assert.That(element, Is.Not.Null);
    }

    [UpstreamTest("QS-007 should auto-detect xpath selector with starting parenthesis")]
    public async Task ShouldAutoDetectXpathSelectorWithStartingParenthesis()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("(//section)[1]");
        Assert.That(element, Is.Not.Null);
    }

    [UpstreamTest("QS-008 should auto-detect xpath selector starting with ..")]
    public async Task ShouldAutoDetectXpathSelectorStartingWith()
    {
        await Page.SetContentAsync("<div><section>test</section><span></span></div>");
        var span = await Page.QuerySelectorAsync("\"test\" >> ../span");
        Assert.That(await span.EvaluateAsync<string>("e => e.nodeName"), Is.EqualTo("SPAN"));
        var div = await Page.QuerySelectorAsync("\"test\" >> ..");
        Assert.That(await div.EvaluateAsync<string>("e => e.nodeName"), Is.EqualTo("DIV"));
    }

    [UpstreamTest("QS-009 should auto-detect text selector")]
    public async Task ShouldAutoDetectTextSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("\"test\"");
        Assert.That(element, Is.Not.Null);
    }

    [UpstreamTest("QS-010 should auto-detect css selector")]
    public async Task ShouldAutoDetectCssSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("section");
        Assert.That(element, Is.Not.Null);
    }

    [UpstreamTest("QS-011 should support >> syntax")]
    public async Task ShouldSupportSyntax()
    {
        await Page.SetContentAsync("<section><div>test</div></section>");
        var element = await Page.QuerySelectorAsync("css=section >> css=div");
        Assert.That(element, Is.Not.Null);
    }

    [UpstreamTest("QS-012 should query existing elements")]
    public async Task ShouldQueryExistingElements()
    {
        await Page.SetContentAsync("<div>A</div><br/><div>B</div>");
        var elements = await Page.QuerySelectorAllAsync("div");
        Assert.That(elements.Count, Is.EqualTo(2));
        var promises = elements.Select(element => Page.EvaluateAsync<string>("e => e.textContent", element));
        Assert.That(await PromiseAll(promises), Is.EqualTo(new[] { "A", "B" }));
    }

    [UpstreamTest("QS-013 should return empty array if nothing is found")]
    public async Task ShouldReturnEmptyArrayIfNothingIsFound()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var elements = await Page.QuerySelectorAllAsync("div");
        Assert.That(elements.Count, Is.EqualTo(0));
    }

    [UpstreamTest("QS-014 xpath should query existing element")]
    public async Task XpathShouldQueryExistingElement()
    {
        await Page.SetContentAsync("<section>test</section>");
        var elements = await Page.QuerySelectorAllAsync("xpath=/html/body/section");
        Assert.That(elements[0], Is.Not.Null);
        Assert.That(elements.Count, Is.EqualTo(1));
    }

    [UpstreamTest("QS-015 xpath should return empty array for non-existing element")]
    public async Task XpathShouldReturnEmptyArrayForNonExistingElement()
    {
        var element = await Page.QuerySelectorAllAsync("//html/body/non-existing-element");
        Assert.That(element, Is.EqualTo(Array.Empty<IElementHandle>()));
    }

    [UpstreamTest("QS-016 xpath should return multiple elements")]
    public async Task XpathShouldReturnMultipleElements()
    {
        await Page.SetContentAsync("<div></div><div></div>");
        var elements = await Page.QuerySelectorAllAsync("xpath=/html/body/div");
        Assert.That(elements.Count, Is.EqualTo(2));
    }

    [UpstreamTest("QS-017 $$ should work with bogus Array.from")]
    public async Task ShouldWorkWithBogusArrayFrom()
    {
        await Page.SetContentAsync("<div>hello</div><div></div>");
        var div1 = await Page.EvaluateHandleAsync(@"() => {
    Array.from = () => [];
    return document.querySelector('div');
  }");
        var elements = await Page.QuerySelectorAllAsync("div");
        Assert.That(elements.Count, Is.EqualTo(2));
        // Check that element handle is functional and belongs to the main world.
        Assert.That(await elements[0].EvaluateAsync<bool>("(div, div1) => div === div1", div1), Is.EqualTo(true));
    }
}
