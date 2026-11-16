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

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-evaluate-handle.spec.ts.</summary>
public class PageEvaluateHandleTests
{
    [UpstreamTest("EV-101 should work")]
    public async Task ShouldWork(IPage page)
    {
        var windowHandle = await page.EvaluateHandleAsync("() => window");
        Assert.That(windowHandle, Is.Not.Null);
    }

    [UpstreamTest("EV-102 should accept object handle as an argument")]
    public async Task ShouldAcceptObjectHandleAsAnArgument(IPage page)
    {
        var navigatorHandle = await page.EvaluateHandleAsync("() => navigator");
        var text = await page.EvaluateAsync<string>("e => e.userAgent", navigatorHandle);
        Assert.That(text, Does.Contain("Mozilla"));
    }

    [UpstreamTest("EV-103 should accept object handle to primitive types")]
    public async Task ShouldAcceptObjectHandleToPrimitiveTypes(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync("() => 5");
        var isFive = await page.EvaluateAsync<bool>("e => Object.is(e, 5)", aHandle);
        Assert.That(isFive, Is.True);
    }

    [UpstreamTest("EV-104 should accept nested handle")]
    public async Task ShouldAcceptNestedHandle(IPage page)
    {
        var foo = await page.EvaluateHandleAsync("() => ({ x: 1, y: 'foo' })");
        var result = await page.EvaluateAsync<object>(@"({ foo }) => {
    return foo;
  }", new { foo });
        ExpectToEqual(result, new { x = 1, y = "foo" });
    }

    [UpstreamTest("EV-105 should accept nested window handle")]
    public async Task ShouldAcceptNestedWindowHandle(IPage page)
    {
        var foo = await page.EvaluateHandleAsync("() => window");
        var result = await page.EvaluateAsync<bool>(@"({ foo }) => {
    return foo === window;
  }", new { foo });
        Assert.That(result, Is.EqualTo(true));
    }

    [UpstreamTest("EV-106 should accept multiple nested handles")]
    public async Task ShouldAcceptMultipleNestedHandles(IPage page)
    {
        var foo = await page.EvaluateHandleAsync("() => ({ x: 1, y: 'foo' })");
        var bar = await page.EvaluateHandleAsync("() => 5");
        var baz = await page.EvaluateHandleAsync("() => (['baz'])");
        var result = await page.EvaluateAsync<string>(@"x => {
    return JSON.stringify(x);
  }", new { a1 = new { foo }, a2 = new { bar, arr = new[] { new { baz } } } });
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(result), new
        {
            a1 = new { foo = new { x = 1, y = "foo" } },
            a2 = new { bar = 5, arr = new[] { new { baz = new[] { "baz" } } } },
        });
    }

    [UpstreamTest("EV-107 should accept same handle multiple times")]
    public async Task ShouldAcceptSameHandleMultipleTimes(IPage page)
    {
        var foo = await page.EvaluateHandleAsync("() => 1");
        ExpectToEqual(await page.EvaluateAsync<object>("x => x", new { foo, bar = new[] { foo }, baz = new { foo } }), new { foo = 1, bar = new[] { 1 }, baz = new { foo = 1 } });
    }

    [UpstreamTest("EV-108 should accept same nested object multiple times")]
    public async Task ShouldAcceptSameNestedObjectMultipleTimes(IPage page)
    {
        var foo = new { x = 1 };
        ExpectToEqual(await page.EvaluateAsync<object>("x => x", new { foo, bar = new[] { foo }, baz = new { foo } }), new { foo = new { x = 1 }, bar = new[] { new { x = 1 } }, baz = new { foo = new { x = 1 } } });
    }

    [UpstreamTest("EV-109 should accept object handle to unserializable value")]
    public async Task ShouldAcceptObjectHandleToUnserializableValue(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync("() => Infinity");
        Assert.That(await page.EvaluateAsync<bool>("e => Object.is(e, Infinity)", aHandle), Is.EqualTo(true));
    }

    [UpstreamTest("EV-110 should pass configurable args")]
    public async Task ShouldPassConfigurableArgs(IPage page)
    {
        var result = await page.EvaluateAsync<object>(@"arg => {
    if (arg.foo !== 42)
      throw new Error('Not a 42');
    arg.foo = 17;
    if (arg.foo !== 17)
      throw new Error('Not 17');
    delete arg.foo;
    if (arg.foo === 17)
      throw new Error('Still 17');
    return arg;
  }", new { foo = 42 });
        ExpectToEqual(result, new { });
    }

    [UpstreamTest("EV-111 should work with primitives")]
    public async Task ShouldWorkWithPrimitives(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync(@"() => {
    window['FOO'] = 123;
    return window;
  }");
        Assert.That(await page.EvaluateAsync<int>("e => e['FOO']", aHandle), Is.EqualTo(123));
    }
}
