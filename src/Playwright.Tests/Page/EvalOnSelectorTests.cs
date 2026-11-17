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

/// <summary>Port of tests/page/eval-on-selector.spec.ts.</summary>
public class EvalOnSelectorTests : UpstreamPageTest
{
    [UpstreamTest("ES-001 should work with css selector")]
    public async Task ShouldWorkWithCssSelector()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("css=section", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("testAttribute"));
    }

    [UpstreamTest("ES-002 should work with id selector")]
    public async Task ShouldWorkWithIdSelector()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("id=testAttribute", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("testAttribute"));
    }

    [UpstreamTest("ES-003 should work with data-test selector")]
    public async Task ShouldWorkWithDataTestSelector()
    {
        await Page.SetContentAsync("<section data-test=foo id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("data-test=foo", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("testAttribute"));
    }

    [UpstreamTest("ES-004 should work with data-testid selector")]
    public async Task ShouldWorkWithDataTestidSelector()
    {
        await Page.SetContentAsync("<section data-testid=foo id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("data-testid=foo", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("testAttribute"));
    }

    [UpstreamTest("ES-005 should work with data-test-id selector")]
    public async Task ShouldWorkWithDataTestIdSelector()
    {
        await Page.SetContentAsync("<section data-test-id=foo id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("data-test-id=foo", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("testAttribute"));
    }

    [UpstreamTest("ES-006 should work with text selector in quotes")]
    public async Task ShouldWorkWithTextSelectorInQuotes()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("text=\"43543\"", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("testAttribute"));
    }

    [UpstreamTest("ES-007 should work with xpath selector")]
    public async Task ShouldWorkWithXpathSelector()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("xpath=/html/body/section", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("testAttribute"));
    }

    [UpstreamTest("ES-008 should work with text selector")]
    public async Task ShouldWorkWithTextSelector()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("text=43543", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("testAttribute"));
    }

    [UpstreamTest("ES-009 should auto-detect css selector")]
    public async Task ShouldAutoDetectCssSelector()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("section", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("testAttribute"));
    }

    [UpstreamTest("ES-010 should auto-detect css selector with attributes")]
    public async Task ShouldAutoDetectCssSelectorWithAttributes()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("section[id=\"testAttribute\"]", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("testAttribute"));
    }

    [UpstreamTest("ES-011 should auto-detect nested selectors")]
    public async Task ShouldAutoDetectNestedSelectors()
    {
        await Page.SetContentAsync("<div foo=bar><section>43543<span>Hello<div id=target></div></span></section></div>");
        var idAttribute = await Page.EvalOnSelectorAsync<string>("div[foo=bar] > section >> \"Hello\" >> div", "e => e.id");
        Assert.That(idAttribute, Is.EqualTo("target"));
    }

    [UpstreamTest("ES-012 should accept arguments")]
    public async Task ShouldAcceptArguments()
    {
        await Page.SetContentAsync("<section>hello</section>");
        var text = await Page.EvalOnSelectorAsync<string>("section", "(e, suffix) => e.textContent + suffix", " world!");
        Assert.That(text, Is.EqualTo("hello world!"));
    }

    [UpstreamTest("ES-013 should accept ElementHandles as arguments")]
    public async Task ShouldAcceptElementHandlesAsArguments()
    {
        await Page.SetContentAsync("<section>hello</section><div> world</div>");
        var divHandle = await Page.QuerySelectorAsync("div");
        var text = await Page.EvalOnSelectorAsync<string>("section", "(e, div) => e.textContent + div.textContent", divHandle);
        Assert.That(text, Is.EqualTo("hello world"));
    }

    [UpstreamTest("ES-014 should throw error if no element is found")]
    public async Task ShouldThrowErrorIfNoElementIsFound()
    {
        Exception error = null;
        error = await Catch(() => Page.EvalOnSelectorAsync<string>("section", "e => e.id"));
        Assert.That(error.Message, Does.Contain("Failed to find element matching selector \"section\""));
    }

    [UpstreamTest("ES-015 should support >> syntax")]
    public async Task ShouldSupportSyntax()
    {
        await Page.SetContentAsync("<section><div>hello</div></section>");
        var text = await Page.EvalOnSelectorAsync<string>("css=section >> css=div", "(e, suffix) => e.textContent + suffix", " world!");
        Assert.That(text, Is.EqualTo("hello world!"));
    }

    [UpstreamTest("ES-016 should support >> syntax with different engines")]
    public async Task ShouldSupportSyntaxWithDifferentEngines()
    {
        await Page.SetContentAsync("<section><div><span>hello</span></div></section>");
        var text = await Page.EvalOnSelectorAsync<string>("xpath=/html/body/section >> css=div >> text=\"hello\"", "(e, suffix) => e.textContent + suffix", " world!");
        Assert.That(text, Is.EqualTo("hello world!"));
    }

    [UpstreamTest("ES-017 should support spaces with >> syntax")]
    public async Task ShouldSupportSpacesWithSyntax()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        var text = await Page.EvalOnSelectorAsync<string>(" css = div >>css=div>>css   = span  ", "e => e.textContent");
        Assert.That(text, Is.EqualTo("Hello from root2"));
    }

    [UpstreamTest("ES-018 should not stop at first failure with >> syntax")]
    public async Task ShouldNotStopAtFirstFailureWithSyntax()
    {
        await Page.SetContentAsync("<div><span>Next</span><button>Previous</button><button>Next</button></div>");
        var html = await Page.EvalOnSelectorAsync<string>("button >> \"Next\"", "e => e.outerHTML");
        Assert.That(html, Is.EqualTo("<button>Next</button>"));
    }

    [UpstreamTest("ES-019 should support * capture")]
    public async Task ShouldSupportCapture()
    {
        await Page.SetContentAsync("<section><div><span>a</span></div></section><section><div><span>b</span></div></section>");
        Assert.That(await Page.EvalOnSelectorAsync<string>("*css=div >> \"b\"", "e => e.outerHTML"), Is.EqualTo("<div><span>b</span></div>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("section >> *css=div >> \"b\"", "e => e.outerHTML"), Is.EqualTo("<div><span>b</span></div>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=div >> *text=\"b\"", "e => e.outerHTML"), Is.EqualTo("<span>b</span>"));
        Assert.That(await Page.QuerySelectorAsync("*"), Is.Not.Null);
    }

    [UpstreamTest("ES-020 should throw on multiple * captures")]
    public async Task ShouldThrowOnMultipleCaptures()
    {
        var error = await Catch(() => Page.EvalOnSelectorAsync<string>("*css=div >> *css=span", "e => e.outerHTML"));
        Assert.That(error.Message, Does.Contain("Only one of the selectors can capture using * modifier"));
    }

    [UpstreamTest("ES-021 should throw on malformed * capture")]
    public async Task ShouldThrowOnMalformedCapture()
    {
        var error = await Catch(() => Page.EvalOnSelectorAsync<string>("*=div", "e => e.outerHTML"));
        Assert.That(error.Message, Does.Contain("Unknown engine \"\" while parsing selector *=div"));
    }

    [UpstreamTest("ES-022 should work with spaces in css attributes")]
    public async Task ShouldWorkWithSpacesInCssAttributes()
    {
        await Page.SetContentAsync("<div><input placeholder=\"Select date\"></div>");
        Assert.That(await Page.WaitForSelectorAsync("[placeholder=\"Select date\"]"), Is.Not.Null);
        Assert.That(await Page.WaitForSelectorAsync("[placeholder='Select date']"), Is.Not.Null);
        Assert.That(await Page.WaitForSelectorAsync("input[placeholder=\"Select date\"]"), Is.Not.Null);
        Assert.That(await Page.WaitForSelectorAsync("input[placeholder='Select date']"), Is.Not.Null);
        Assert.That(await Page.QuerySelectorAsync("[placeholder=\"Select date\"]"), Is.Not.Null);
        Assert.That(await Page.QuerySelectorAsync("[placeholder='Select date']"), Is.Not.Null);
        Assert.That(await Page.QuerySelectorAsync("input[placeholder=\"Select date\"]"), Is.Not.Null);
        Assert.That(await Page.QuerySelectorAsync("input[placeholder='Select date']"), Is.Not.Null);
        Assert.That(await Page.EvalOnSelectorAsync<string>("[placeholder=\"Select date\"]", "e => e.outerHTML"), Is.EqualTo("<input placeholder=\"Select date\">"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("[placeholder='Select date']", "e => e.outerHTML"), Is.EqualTo("<input placeholder=\"Select date\">"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("input[placeholder=\"Select date\"]", "e => e.outerHTML"), Is.EqualTo("<input placeholder=\"Select date\">"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("input[placeholder='Select date']", "e => e.outerHTML"), Is.EqualTo("<input placeholder=\"Select date\">"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=[placeholder=\"Select date\"]", "e => e.outerHTML"), Is.EqualTo("<input placeholder=\"Select date\">"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=[placeholder='Select date']", "e => e.outerHTML"), Is.EqualTo("<input placeholder=\"Select date\">"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=input[placeholder=\"Select date\"]", "e => e.outerHTML"), Is.EqualTo("<input placeholder=\"Select date\">"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=input[placeholder='Select date']", "e => e.outerHTML"), Is.EqualTo("<input placeholder=\"Select date\">"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> [placeholder=\"Select date\"]", "e => e.outerHTML"), Is.EqualTo("<input placeholder=\"Select date\">"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> [placeholder='Select date']", "e => e.outerHTML"), Is.EqualTo("<input placeholder=\"Select date\">"));
    }

    [UpstreamTest("ES-023 should work with quotes in css attributes")]
    public async Task ShouldWorkWithQuotesInCssAttributes()
    {
        await Page.SetContentAsync("<div><input placeholder=\"Select&quot;date\"></div>");
        Assert.That(await Page.QuerySelectorAsync("[placeholder=\"Select\\\"date\"]"), Is.Not.Null);
        Assert.That(await Page.QuerySelectorAsync("[placeholder='Select\"date']"), Is.Not.Null);
        await Page.SetContentAsync("<div><input placeholder=\"Select &quot; date\"></div>");
        Assert.That(await Page.QuerySelectorAsync("[placeholder=\"Select \\\" date\"]"), Is.Not.Null);
        Assert.That(await Page.QuerySelectorAsync("[placeholder='Select \" date']"), Is.Not.Null);
        await Page.SetContentAsync("<div><input placeholder=\"Select&apos;date\"></div>");
        Assert.That(await Page.QuerySelectorAsync("[placeholder=\"Select'date\"]"), Is.Not.Null);
        Assert.That(await Page.QuerySelectorAsync("[placeholder='Select\\'date']"), Is.Not.Null);
        await Page.SetContentAsync("<div><input placeholder=\"Select &apos; date\"></div>");
        Assert.That(await Page.QuerySelectorAsync("[placeholder=\"Select ' date\"]"), Is.Not.Null);
        Assert.That(await Page.QuerySelectorAsync("[placeholder='Select \\' date']"), Is.Not.Null);
    }

    [UpstreamTest("ES-024 should work with spaces in css attributes when missing")]
    public async Task ShouldWorkWithSpacesInCssAttributesWhenMissing()
    {
        var inputPromise = Page.WaitForSelectorAsync("[placeholder=\"Select date\"]");
        Assert.That(await Page.QuerySelectorAsync("[placeholder=\"Select date\"]"), Is.Null);
        await Page.SetContentAsync("<div><input placeholder=\"Select date\"></div>");
        await inputPromise;
    }

    [UpstreamTest("ES-025 should work with quotes in css attributes when missing")]
    public async Task ShouldWorkWithQuotesInCssAttributesWhenMissing()
    {
        var inputPromise = Page.WaitForSelectorAsync("[placeholder=\"Select\\\"date\"]");
        Assert.That(await Page.QuerySelectorAsync("[placeholder=\"Select\\\"date\"]"), Is.Null);
        await Page.SetContentAsync("<div><input placeholder=\"Select&quot;date\"></div>");
        await inputPromise;
    }

    [UpstreamTest("ES-026 should return complex values")]
    public async Task ShouldReturnComplexValues()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        var idAttribute = await Page.EvalOnSelectorAsync<object>("css=section", "e => [{ id: e.id }]");
        ExpectToEqual(idAttribute, new[] { new { id = "testAttribute" } });
    }
}
