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

/// <summary>Port of tests/page/jshandle-to-string.spec.ts.</summary>
public class JshandleToStringTests : UpstreamPageTest
{
    [UpstreamTest("JH-018 should work for primitives")]
    public async Task ShouldWorkForPrimitives()
    {
        var numberHandle = await Page.EvaluateHandleAsync("() => 2");
        Assert.That(numberHandle.ToString(), Is.EqualTo("2"));
        var stringHandle = await Page.EvaluateHandleAsync("() => 'a'");
        Assert.That(stringHandle.ToString(), Is.EqualTo("a"));
    }

    [UpstreamTest("JH-019 should work for complicated objects")]
    public async Task ShouldWorkForComplicatedObjects()
    {
        var aHandle = await Page.EvaluateHandleAsync("() => window");
        if (BrowserName != "firefox" || Channel?.StartsWith("moz-firefox", StringComparison.Ordinal) == true)
        {
            Assert.That(aHandle.ToString(), Is.EqualTo("Window"));
        }
        else
        {
            Assert.That(aHandle.ToString(), Is.EqualTo("JSHandle@object"));
        }
    }

    [UpstreamTest("JH-020 should beautifully render sparse arrays")]
    public async Task ShouldBeautifullyRenderSparseArrays()
    {
        var (msg, _) = await PromiseAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateHandleAsync(@"() => {
      const a = [];
      a[1] = 1;
      a[10] = 2;
      a[100] = 3;
      console.log(a);
    }"));
        if (BrowserName == "firefox")
        {
            Assert.That(msg.Text, Is.EqualTo("Array"));
        }
        else
        {
            Assert.That(msg.Text, Is.EqualTo("[empty, 1, empty x 8, 2, empty x 89, 3]"));
        }
    }

    [UpstreamTest("JH-021 should work for promises")]
    public async Task ShouldWorkForPromises()
    {
        // wrap the promise in an object, otherwise we will await.
        var wrapperHandle = await Page.EvaluateHandleAsync("() => ({ b: Promise.resolve(123) })");
        var bHandle = await wrapperHandle.GetPropertyAsync("b");
        Assert.That(bHandle.ToString(), Is.EqualTo("Promise"));
    }

    [UpstreamTest("JH-022 should work with different subtypes @smoke")]
    [LocalFixme("dotnet: EvaluateHandleAsync(\"(function(){})\") calls the function and gives a handle of 'undefined': the client does not send isFunction, so the driver calls an expression whose value is a function; upstream sends isFunction: false for a string and gets a handle of the function")]
    public async Task ShouldWorkWithDifferentSubtypes()
    {
        var isBiDi = Channel?.StartsWith("bidi-chrom", StringComparison.Ordinal) == true || Channel?.StartsWith("moz-firefox", StringComparison.Ordinal) == true;
        if (!isBiDi)
        {
            Assert.That((await Page.EvaluateHandleAsync("(function(){})")).ToString(), Does.Contain("function"));
        }
        else
        {
            Assert.That((await Page.EvaluateHandleAsync("(function(){})")).ToString(), Is.EqualTo("Function"));
        }
        Assert.That((await Page.EvaluateHandleAsync("12")).ToString(), Is.EqualTo("12"));
        Assert.That((await Page.EvaluateHandleAsync("true")).ToString(), Is.EqualTo("true"));
        Assert.That((await Page.EvaluateHandleAsync("undefined")).ToString(), Is.EqualTo("undefined"));
        Assert.That((await Page.EvaluateHandleAsync("\"foo\"")).ToString(), Is.EqualTo("foo"));
        Assert.That((await Page.EvaluateHandleAsync("Symbol()")).ToString(), Is.EqualTo("Symbol()"));
        Assert.That((await Page.EvaluateHandleAsync("new Map()")).ToString(), Does.Contain("Map"));
        Assert.That((await Page.EvaluateHandleAsync("new Set()")).ToString(), Does.Contain("Set"));
        Assert.That((await Page.EvaluateHandleAsync("[]")).ToString(), Does.Contain("Array"));
        Assert.That((await Page.EvaluateHandleAsync("null")).ToString(), Is.EqualTo("null"));
        var bodyHandle = await Page.EvaluateHandleAsync("document.body");
        await Expect(async () => Assert.That(bodyHandle.ToString(), Is.EqualTo("JSHandle@<body></body>"))).ToPassAsync();
        Assert.That((await Page.EvaluateHandleAsync("new WeakMap()")).ToString(), Is.EqualTo("WeakMap"));
        Assert.That((await Page.EvaluateHandleAsync("new WeakSet()")).ToString(), Is.EqualTo("WeakSet"));
        Assert.That((await Page.EvaluateHandleAsync("new Error()")).ToString(), Does.Contain("Error"));
        Assert.That((await Page.EvaluateHandleAsync("new Proxy({}, {})")).ToString(), Is.EqualTo((BrowserName == "chromium" && !isBiDi) ? "Proxy(Object)" : "Proxy"));
    }

    [UpstreamTest("JH-023 should work with previewable subtypes")]
    public async Task ShouldWorkWithPreviewableSubtypes()
    {
        var isBiDi = Channel?.StartsWith("bidi-chrom", StringComparison.Ordinal) == true || Channel?.StartsWith("moz-firefox", StringComparison.Ordinal) == true;
        Info.Skip(BrowserName == "firefox" && !isBiDi);
        Assert.That((await Page.EvaluateHandleAsync("/foo/")).ToString(), Is.EqualTo("/foo/"));
        Assert.That((await Page.EvaluateHandleAsync("new Date(0)")).ToString(), Does.Contain("GMT"));
        if (!isBiDi)
        {
            Assert.That((await Page.EvaluateHandleAsync("new Int32Array()")).ToString(), Does.Contain("Int32Array"));
        }
        else
        {
            Assert.That((await Page.EvaluateHandleAsync("new Int32Array()")).ToString(), Is.EqualTo("TypedArray"));
        }
    }
}
