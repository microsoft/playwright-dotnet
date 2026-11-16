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

/// <summary>Port of tests/page/selectors-register.spec.ts.</summary>
public class SelectorsRegisterTests
{
    [BeforeEach]
    public static void Skip() => It.Skip(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PW_TEST_CONNECT_WS_ENDPOINT")), "selectors.register does not support reuse");

    [UpstreamTest("SE-154 textContent should be atomic")]
    public async Task TextContentShouldBeAtomic(IPlaywright playwright, IPage page)
    {
        const string createDummySelector = @"() => ({
    query(root, selector) {
      const result = root.querySelector(selector);
      if (result)
        void Promise.resolve().then(() => result.textContent = 'modified');
      return result;
    },
    queryAll(root, selector) {
      const result = Array.from(root.querySelectorAll(selector));
      for (const e of result)
        void Promise.resolve().then(() => e.textContent = 'modified');
      return result;
    }
  })";
        // .NET: a function is the script that the Node.js client makes of it, `(${fun})(undefined)`.
        await playwright.Selectors.RegisterAsync("textContent", new() { Script = $"({createDummySelector})(undefined)" });
        await page.GotoAsync("data:text/html,<div>Hello</div>");
        var tc = await page.TextContentAsync("textContent=div");
        Assert.That(tc, Is.EqualTo("Hello"));
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('div').textContent"), Is.EqualTo("modified"));
    }

    [UpstreamTest("SE-155 innerText should be atomic")]
    public async Task InnerTextShouldBeAtomic(IPlaywright playwright, IPage page)
    {
        const string createDummySelector = @"() => ({
    query(root, selector) {
      const result = root.querySelector(selector);
      if (result)
        void Promise.resolve().then(() => result.textContent = 'modified');
      return result;
    },
    queryAll(root, selector) {
      const result = Array.from(root.querySelectorAll(selector));
      for (const e of result)
        void Promise.resolve().then(() => e.textContent = 'modified');
      return result;
    }
  })";
        // .NET: a function is the script that the Node.js client makes of it, `(${fun})(undefined)`.
        await playwright.Selectors.RegisterAsync("innerText", new() { Script = $"({createDummySelector})(undefined)" });
        await page.GotoAsync("data:text/html,<div>Hello</div>");
        var tc = await page.InnerTextAsync("innerText=div");
        Assert.That(tc, Is.EqualTo("Hello"));
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('div').innerText"), Is.EqualTo("modified"));
    }

    [UpstreamTest("SE-156 innerHTML should be atomic")]
    public async Task InnerHTMLShouldBeAtomic(IPlaywright playwright, IPage page)
    {
        const string createDummySelector = @"() => ({
    query(root, selector) {
      const result = root.querySelector(selector);
      if (result)
        void Promise.resolve().then(() => result.textContent = 'modified');
      return result;
    },
    queryAll(root, selector) {
      const result = Array.from(root.querySelectorAll(selector));
      for (const e of result)
        void Promise.resolve().then(() => e.textContent = 'modified');
      return result;
    }
  })";
        // .NET: a function is the script that the Node.js client makes of it, `(${fun})(undefined)`.
        await playwright.Selectors.RegisterAsync("innerHTML", new() { Script = $"({createDummySelector})(undefined)" });
        await page.GotoAsync("data:text/html,<div>Hello<span>world</span></div>");
        var tc = await page.InnerHTMLAsync("innerHTML=div");
        Assert.That(tc, Is.EqualTo("Hello<span>world</span>"));
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('div').innerHTML"), Is.EqualTo("modified"));
    }

    [UpstreamTest("SE-157 getAttribute should be atomic")]
    public async Task GetAttributeShouldBeAtomic(IPlaywright playwright, IPage page)
    {
        const string createDummySelector = @"() => ({
    query(root, selector) {
      const result = root.querySelector(selector);
      if (result)
        void Promise.resolve().then(() => result.setAttribute('foo', 'modified'));
      return result;
    },
    queryAll(root, selector) {
      const result = Array.from(root.querySelectorAll(selector));
      for (const e of result)
        void Promise.resolve().then(() => e.setAttribute('foo', 'modified'));
      return result;
    }
  })";
        // .NET: a function is the script that the Node.js client makes of it, `(${fun})(undefined)`.
        await playwright.Selectors.RegisterAsync("getAttribute", new() { Script = $"({createDummySelector})(undefined)" });
        await page.GotoAsync("data:text/html,<div foo=hello></div>");
        var tc = await page.GetAttributeAsync("getAttribute=div", "foo");
        Assert.That(tc, Is.EqualTo("hello"));
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('div').getAttribute('foo')"), Is.EqualTo("modified"));
    }

    [UpstreamTest("SE-158 isVisible should be atomic")]
    public async Task IsVisibleShouldBeAtomic(IPlaywright playwright, IPage page)
    {
        const string createDummySelector = @"() => ({
    query(root, selector) {
      const result = root.querySelector(selector);
      if (result)
        void Promise.resolve().then(() => result.style.display = 'none');
      return result;
    },
    queryAll(root, selector) {
      const result = Array.from(root.querySelectorAll(selector));
      for (const e of result)
        void Promise.resolve().then(() => e.style.display = 'none');
      return result;
    }
  })";
        // .NET: a function is the script that the Node.js client makes of it, `(${fun})(undefined)`.
        await playwright.Selectors.RegisterAsync("isVisible", new() { Script = $"({createDummySelector})(undefined)" });
        await page.GotoAsync("data:text/html,<div>Hello</div>");
        var result = await page.IsVisibleAsync("isVisible=div");
        Assert.That(result, Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('div').style.display"), Is.EqualTo("none"));
    }

    [UpstreamTest("SE-159 should take java-style string")]
    public async Task ShouldTakeJavaStyleString(IPlaywright playwright, IPage page)
    {
        var createDummySelector = @"{
    query(root, selector) {
      return root.querySelector(selector);
    },
    queryAll(root, selector) {
      return root.querySelectorAll(selector);
    }
  }";
        await playwright.Selectors.RegisterAsync("objectLiteral", new() { Script = createDummySelector });
        await page.GotoAsync("data:text/html,<div>Hello</div>");
        await page.TextContentAsync("objectLiteral=div");
    }
}
