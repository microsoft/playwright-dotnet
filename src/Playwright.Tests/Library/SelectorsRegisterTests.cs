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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/selectors-register.spec.ts.</summary>
public class SelectorsRegisterTests
{
    private const string createTagSelector = @"() => ({
  query(root, selector) {
    return root.querySelector(selector);
  },
  queryAll(root, selector) {
    return Array.from(root.querySelectorAll(selector));
  }
})";

    [UpstreamTest("SE-160 should work")]
    public async Task ShouldWork(IPlaywright playwright, IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        // Register one engine before creating context.
        await playwright.Selectors.RegisterAsync("tag", new() { Script = $"({createTagSelector})()" });

        var context = await browser.NewContextAsync(contextOptions);
        // Register another engine after creating context.
        await playwright.Selectors.RegisterAsync("tag2", new() { Script = $"({createTagSelector})()" });

        var page = await context.NewPageAsync();
        await page.SetContentAsync("<div><span></span></div><div></div>");

        Assert.That(await page.EvalOnSelectorAsync<string>("tag=DIV", "e => e.nodeName"), Is.EqualTo("DIV"));
        Assert.That(await page.EvalOnSelectorAsync<string>("tag=SPAN", "e => e.nodeName"), Is.EqualTo("SPAN"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("tag=DIV", "es => es.length"), Is.EqualTo(2));

        Assert.That(await page.EvalOnSelectorAsync<string>("tag2=DIV", "e => e.nodeName"), Is.EqualTo("DIV"));
        Assert.That(await page.EvalOnSelectorAsync<string>("tag2=SPAN", "e => e.nodeName"), Is.EqualTo("SPAN"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("tag2=DIV", "es => es.length"), Is.EqualTo(2));

        // Selector names are case-sensitive.
        var error = await Catch(() => page.QuerySelectorAsync("tAG=DIV"));
        Assert.That(error.Message, Does.Contain("Unknown engine \"tAG\" while parsing selector tAG=DIV"));

        await context.CloseAsync();
    }

    [UpstreamTest("SE-162 should work with path")]
    public async Task ShouldWorkWithPath(IPlaywright playwright, IBrowser browser, Func<string, string> asset, BrowserNewPageOptions newPageOptions)
    {
        await playwright.Selectors.RegisterAsync("foo", new() { Path = asset("sectionselectorengine.js") });
        var page = await browser.NewPageAsync(newPageOptions);
        await page.SetContentAsync("<section></section>");
        Assert.That(await page.EvalOnSelectorAsync<string>("foo=whatever", "e => e.nodeName"), Is.EqualTo("SECTION"));
        await page.CloseAsync();
    }

    [UpstreamTest("SE-163 should work in main and isolated world")]
    public async Task ShouldWorkInMainAndIsolatedWorld(IPlaywright playwright, IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        const string createDummySelector = @"() => ({
    query(root, selector) {
      return window['__answer'];
    },
    queryAll(root, selector) {
      return window['__answer'] ? [window['__answer'], document.body, document.documentElement] : [];
    }
  })";
        await playwright.Selectors.RegisterAsync("main", new() { Script = "(" + createDummySelector + ")()" });
        await playwright.Selectors.RegisterAsync("isolated", new() { Script = "(" + createDummySelector + ")()", ContentScript = true });
        var page = await browser.NewPageAsync(newPageOptions);
        await page.SetContentAsync("<div><span><section></section></span></div>");
        await page.EvaluateAsync("() => window['__answer'] = document.querySelector('span')");
        // Works in main if asked.
        Assert.That(await page.EvalOnSelectorAsync<string>("main=ignored", "e => e.nodeName"), Is.EqualTo("SPAN"));
        Assert.That(await page.EvalOnSelectorAsync<string>("css=div >> main=ignored", "e => e.nodeName"), Is.EqualTo("SPAN"));
        Assert.That(await page.EvalOnSelectorAllAsync<bool>("main=ignored", "es => window['__answer'] !== undefined"), Is.EqualTo(true));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("main=ignored", "es => es.filter(e => e).length"), Is.EqualTo(3));
        // Works in isolated by default.
        Assert.That(await page.QuerySelectorAsync("isolated=ignored"), Is.Null);
        Assert.That(await page.QuerySelectorAsync("css=div >> isolated=ignored"), Is.Null);
        // $$eval always works in main, to avoid adopting nodes one by one.
        Assert.That(await page.EvalOnSelectorAllAsync<bool>("isolated=ignored", "es => window['__answer'] !== undefined"), Is.EqualTo(true));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("isolated=ignored", "es => es.filter(e => e).length"), Is.EqualTo(3));
        // At least one engine in main forces all to be in main.
        Assert.That(await page.EvalOnSelectorAsync<string>("main=ignored >> isolated=ignored", "e => e.nodeName"), Is.EqualTo("SPAN"));
        Assert.That(await page.EvalOnSelectorAsync<string>("isolated=ignored >> main=ignored", "e => e.nodeName"), Is.EqualTo("SPAN"));
        // Can be chained to css.
        Assert.That(await page.EvalOnSelectorAsync<string>("main=ignored >> css=section", "e => e.nodeName"), Is.EqualTo("SECTION"));
        await page.CloseAsync();
    }

    [UpstreamTest("SE-164 should handle errors")]
    [Fixme("dotnet: the message is 'Selector engine name may only contain [a-zA-Z0-9_] characters', without the 'selectors.register: ' prefix of the call")]
    public async Task ShouldHandleErrors(IPlaywright playwright, IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        var page = await browser.NewPageAsync(newPageOptions);
        var error = await Catch(() => page.QuerySelectorAsync("neverregister=ignored"));
        Assert.That(error.Message, Does.Contain("Unknown engine \"neverregister\" while parsing selector neverregister=ignored"));

        const string createDummySelector = @"() => ({
    query(root, selector) {
      return root.querySelector('dummy');
    },
    queryAll(root, selector) {
      return Array.from(root.querySelectorAll('dummy'));
    }
  })";

        error = await Catch(() => playwright.Selectors.RegisterAsync("$", new() { Script = "(" + createDummySelector + ")()" }));
        Assert.That(error.Message, Is.EqualTo("selectors.register: Selector engine name may only contain [a-zA-Z0-9_] characters"));

        // Selector names are case-sensitive.
        await playwright.Selectors.RegisterAsync("dummy", new() { Script = "(" + createDummySelector + ")()" });
        await playwright.Selectors.RegisterAsync("duMMy", new() { Script = "(" + createDummySelector + ")()" });

        error = await Catch(() => playwright.Selectors.RegisterAsync("dummy", new() { Script = "(" + createDummySelector + ")()" }));
        Assert.That(error.Message, Is.EqualTo("selectors.register: \"dummy\" selector engine has been already registered"));

        error = await Catch(() => playwright.Selectors.RegisterAsync("css", new() { Script = "(" + createDummySelector + ")()" }));
        Assert.That(error.Message, Is.EqualTo("selectors.register: \"css\" is a predefined selector engine"));
        await page.CloseAsync();
    }

    [UpstreamTest("SE-165 should throw \"already registered\" error when registering")]
    [Fixme("dotnet: the message is '\"alreadyRegistered\" selector engine has been already registered', without the 'selectors.register: ' prefix of the call")]
    public async Task ShouldThrowAlreadyRegisteredErrorWhenRegistering(IPlaywright playwright)
    {
        await playwright.Selectors.RegisterAsync("alreadyRegistered", new() { Script = "(" + createTagSelector + ")()" });
        var error = await ThrowsAsync<PlaywrightException>(() => playwright.Selectors.RegisterAsync("alreadyRegistered", new() { Script = "(" + createTagSelector + ")()" }));
        Assert.That(error.Message, Does.Contain("selectors.register: \"alreadyRegistered\" selector engine has been already registered"));
    }

    [UpstreamTest("SE-166 should not rely on engines working from the root")]
    public async Task ShouldNotRelyOnEnginesWorkingFromTheRoot(IPlaywright playwright, IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        const string createValueEngine = @"() => ({
    query(root, selector) {
      return root && root.value.includes(selector) ? root : undefined;
    },
    queryAll(root, selector) {
      return root && root.value.includes(selector) ? [root] : [];
    },
  })";
        await playwright.Selectors.RegisterAsync("__value", new() { Script = "(" + createValueEngine + ")()" });
        var page = await browser.NewPageAsync(newPageOptions);
        await page.SetContentAsync("<input id=input1 value=value1><input id=input2 value=value2>");
        Assert.That(await page.EvalOnSelectorAsync<string>("input >> __value=value2", "e => e.id"), Is.EqualTo("input2"));
        await page.CloseAsync();
    }

    [UpstreamTest("SE-167 should throw a nice error if the selector returns a bad value")]
    public async Task ShouldThrowANiceErrorIfTheSelectorReturnsABadValue(IPlaywright playwright, IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        const string createFakeEngine = @"() => ({
    query(root, selector) {
      return [document.body];
    },
    queryAll(root, selector) {
      return [[document.body]];
    },
  })";

        await playwright.Selectors.RegisterAsync("__fake", new() { Script = "(" + createFakeEngine + ")()" });
        var page = await browser.NewPageAsync(newPageOptions);
        var error = await Catch(() => page.QuerySelectorAsync("__fake=value2"));
        Assert.That(error.Message, Does.Contain("Expected a Node but got [object Array]"));
        await page.CloseAsync();
    }
}
