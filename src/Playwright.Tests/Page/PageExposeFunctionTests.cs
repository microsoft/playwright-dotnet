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

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-expose-function.spec.ts.</summary>
public class PageExposeFunctionTests : UpstreamPageTest
{
    [UpstreamTest("EF-001 exposeBinding should work @smoke")]
    public async Task ExposeBindingShouldWork()
    {
        BindingSource bindingSource = null;
        await Page.ExposeBindingAsync("add", (BindingSource source, int a, int b) =>
        {
            bindingSource = source;
            return a + b;
        });
        var result = await Page.EvaluateAsync<int>(@"async function() {
    return window['add'](5, 6);
  }");
        Assert.That(bindingSource.Context, Is.SameAs(Page.Context));
        Assert.That(bindingSource.Page, Is.SameAs(Page));
        Assert.That(bindingSource.Frame, Is.SameAs(Page.MainFrame));
        Assert.That(result, Is.EqualTo(11));
    }

    [UpstreamTest("EF-002 should work")]
    public async Task ShouldWork()
    {
        await Page.ExposeFunctionAsync("compute", (int a, int b) =>
        {
            return a * b;
        });
        var result = await Page.EvaluateAsync<int>(@"async function() {
    return await window['compute'](9, 4);
  }");
        Assert.That(result, Is.EqualTo(36));
    }

    [UpstreamTest("EF-003 should dispose")]
    public async Task ShouldDispose()
    {
        var binding = await Page.ExposeFunctionAsync("compute", (int a, int b) =>
        {
            return a * b;
        });
        var result = await Page.EvaluateAsync<int>(@"async function() {
    return await window['compute'](9, 4);
  }");
        Assert.That(result, Is.EqualTo(36));
        await binding.DisposeAsync();

        var e = await Catch(() => Page.EvaluateAsync(@"async function() {
    return await window['compute'](9, 4);
  }"));
        Assert.That(e.Message, Does.Contain("is not a function"));
    }

    [UpstreamTest("EF-004 should work with handles and complex objects")]
    public async Task ShouldWorkWithHandlesAndComplexObjects()
    {
        var fooHandle = await Page.EvaluateHandleAsync(@"() => {
    window['fooValue'] = { bar: 2 };
    return window['fooValue'];
  }");
        await Page.ExposeFunctionAsync("handle", () =>
        {
            return new[] { new { foo = fooHandle } };
        });
        var equals = await Page.EvaluateAsync<bool>(@"async function() {
    const value = await window['handle']();
    const [{ foo }] = value;
    return foo === window['fooValue'];
  }");
        Assert.That(equals, Is.EqualTo(true));
    }

    [UpstreamTest("EF-005 should throw exception in page context")]
    public async Task ShouldThrowExceptionInPageContext()
    {
        await Page.ExposeFunctionAsync("woof", () =>
        {
            throw new Exception("WOOF WOOF");
        });
        // .NET: const { message, stack } = await page.evaluate(...)
        var result = await Page.EvaluateAsync<JsonElement>(@"async () => {
    try {
      await window['woof']();
    } catch (e) {
      return { message: e.message, stack: e.stack };
    }
  }");
        var message = result.GetProperty("message").GetString();
        var stack = result.GetProperty("stack").GetString();
        Assert.That(message, Is.EqualTo("WOOF WOOF"));
        // .NET: expect(stack).toContain(__filename) is left out: a .NET stack trace has the frames of the library.
    }

    [UpstreamTest("EF-007 should be callable from-inside addInitScript")]
    public async Task ShouldBeCallableFromInsideAddInitScript()
    {
        var called = false;
        await Page.ExposeFunctionAsync("woof", () =>
        {
            called = true;
        });
        // .NET: an init script is a script, not a function: the call that upstream adds to a function is written out.
        await Page.AddInitScriptAsync("(() => window['woof']())()");
        await Page.ReloadAsync();
        await Expect(() =>
        {
            Assert.That(called, Is.EqualTo(true));
            return Task.CompletedTask;
        }).ToPassAsync();
    }

    [UpstreamTest("EF-008 should survive navigation")]
    public async Task ShouldSurviveNavigation()
    {
        await Page.ExposeFunctionAsync("compute", (int a, int b) =>
        {
            return a * b;
        });

        await Page.GotoAsync(Server.EmptyPage);
        var result = await Page.EvaluateAsync<int>(@"async function() {
    return await window['compute'](9, 4);
  }");
        Assert.That(result, Is.EqualTo(36));
    }

    [UpstreamTest("EF-009 should await returned promise")]
    public async Task ShouldAwaitReturnedPromise()
    {
        await Page.ExposeFunctionAsync("compute", (int a, int b) =>
        {
            return Task.FromResult(a * b);
        });

        var result = await Page.EvaluateAsync<int>(@"async function() {
    return await window['compute'](3, 5);
  }");
        Assert.That(result, Is.EqualTo(15));
    }

    [UpstreamTest("EF-010 should work on frames")]
    public async Task ShouldWorkOnFrames()
    {
        await Page.ExposeFunctionAsync("compute", (int a, int b) =>
        {
            return Task.FromResult(a * b);
        });

        await Page.GotoAsync(Server.Prefix + "/frames/nested-frames.html");
        var frame = Page.Frames[1];
        var result = await frame.EvaluateAsync<int>(@"async function() {
    return await window['compute'](3, 5);
  }");
        Assert.That(result, Is.EqualTo(15));
    }

    [UpstreamTest("EF-011 should work on frames before navigation")]
    public async Task ShouldWorkOnFramesBeforeNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/nested-frames.html");
        await Page.ExposeFunctionAsync("compute", (int a, int b) =>
        {
            return Task.FromResult(a * b);
        });

        var frame = Page.Frames[1];
        var result = await frame.EvaluateAsync<int>(@"async function() {
    return await window['compute'](3, 5);
  }");
        Assert.That(result, Is.EqualTo(15));
    }

    [UpstreamTest("EF-012 should work after cross origin navigation")]
    public async Task ShouldWorkAfterCrossOriginNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.ExposeFunctionAsync("compute", (int a, int b) =>
        {
            return a * b;
        });

        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        var result = await Page.EvaluateAsync<int>(@"async function() {
    return await window['compute'](9, 4);
  }");
        Assert.That(result, Is.EqualTo(36));
    }

    [UpstreamTest("EF-013 should work with complex objects")]
    public async Task ShouldWorkWithComplexObjects()
    {
        await Page.ExposeFunctionAsync("complexObject", (JsonElement a, JsonElement b) =>
        {
            return new { x = a.GetProperty("x").GetInt32() + b.GetProperty("x").GetInt32() };
        });
        var result = await Page.EvaluateAsync<JsonElement>("async () => window['complexObject']({ x: 5 }, { x: 2 })");
        Assert.That(result.GetProperty("x").GetInt32(), Is.EqualTo(7));
    }

    [UpstreamTest("EF-014 should throw for duplicate registrations")]
    public async Task ShouldThrowForDuplicateRegistrations()
    {
        await Page.ExposeFunctionAsync("foo", () => { });
        var error = await Catch(() => Page.ExposeFunctionAsync("foo", () => { }));
        Assert.That(error.Message, Does.Contain("Function \"foo\" has been already registered"));
    }

    [UpstreamTest("EF-015 should work with setContent")]
    public async Task ShouldWorkWithSetContent()
    {
        await Page.ExposeFunctionAsync("compute", (int a, int b) =>
        {
            return Task.FromResult(a * b);
        });
        await Page.SetContentAsync("<script>window.result = compute(3, 2)</script>");
        Assert.That(await Page.EvaluateAsync<int>("window.result"), Is.EqualTo(6));
    }

    [UpstreamTest("EF-016 should alias Window, Document and Node")]
    public async Task ShouldAliasWindowDocumentAndNode()
    {
        string[] @object = null;
        await Page.ExposeBindingAsync("log", (BindingSource source, string[] obj) => @object = obj);
        await Page.EvaluateAsync("window.log([window, document, document.body])");
        Assert.That(@object, Is.EqualTo(new[] { "ref: <Window>", "ref: <Document>", "ref: <Node>" }));
    }

    [UpstreamTest("EF-017 should serialize cycles")]
    public async Task ShouldSerializeCycles()
    {
        object @object = null;
        await Page.ExposeBindingAsync("log", (BindingSource source, object obj) => @object = obj);
        await Page.EvaluateAsync("const a = {}; a.b = a; window.log(a)");
        var a = new Dictionary<string, object>();
        a["b"] = a;
        ExpectToEqual(@object, a);
    }

    [UpstreamTest("EF-018 should work with overridden console object")]
    public async Task ShouldWorkWithOverriddenConsoleObject()
    {
        await Page.EvaluateAsync("() => window.console = null");
        Assert.That(Page.EvaluateAsync<bool>("() => window.console === null"), Is.Not.Null);
        await Page.ExposeFunctionAsync("add", (int a, int b) => a + b);
        Assert.That(await Page.EvaluateAsync<int>("add(5, 6)"), Is.EqualTo(11));
    }

    [UpstreamTest("EF-019 should work with busted Array.prototype.map/push")]
    public async Task ShouldWorkWithBustedArrayPrototypeMapPush()
    {
        Server.SetRoute("/test", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["content-type"] = "text/html",
            });
            res.End(@"<script>
      Array.prototype.map = null;
      Array.prototype.push = null;
    </script>");
        });
        await Page.GotoAsync(Server.Prefix + "/test");
        await Page.ExposeFunctionAsync("add", (int a, int b) => a + b);
        Assert.That(await Page.EvaluateAsync<int>("add(5, 6)"), Is.EqualTo(11));
    }

    [UpstreamTest("EF-020 should fail with busted Array.prototype.toJSON")]
    public async Task ShouldFailWithBustedArrayPrototypeToJSON()
    {
        await Page.EvaluateHandleAsync("() => Array.prototype.toJSON = () => '\"[]\"'");

        await Page.ExposeFunctionAsync("add", (int a, int b) => a + b);
        var error = await ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync("add(5, 6)"));
        Assert.That(error.Message, Does.Contain("serializedArgs is not an array. This can happen when Array.prototype.toJSON is defined incorrectly"));

        Assert.That(await Page.EvaluateAsync<string>("() => [].toJSON()"), Is.EqualTo("\"[]\""));
    }

    [UpstreamTest("EF-021 exposeBinding should work in parallel")]
    public async Task ExposeBindingShouldWorkInParallel()
    {
        await PromiseAll(
            Page.ExposeBindingAsync("foo", _ => 42),
            Page.ExposeBindingAsync("bar", _ => 42));
        await Page.EvaluateAsync(@"() => {
    window.foo();
    window.bar();
  }");
    }
}
