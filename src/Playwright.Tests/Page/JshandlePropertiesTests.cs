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

/// <summary>Port of tests/page/jshandle-properties.spec.ts.</summary>
public class JshandlePropertiesTests
{
    [UpstreamTest("JH-010 should work @smoke")]
    public async Task ShouldWork(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync(@"() => ({
    one: 1,
    two: 2,
    three: 3
  })");
        var twoHandle = await aHandle.GetPropertyAsync("two");
        Assert.That(await twoHandle.JsonValueAsync<int>(), Is.EqualTo(2));
    }

    [UpstreamTest("JH-011 should work with undefined, null, and empty")]
    public async Task ShouldWorkWithUndefinedNullAndEmpty(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync(@"() => ({
    undefined: undefined,
    null: null,
  })");
        var undefinedHandle = await aHandle.GetPropertyAsync("undefined");
        // .NET: expect(String(await undefinedHandle.jsonValue())).toEqual('undefined'): undefined is null
        Assert.That(await undefinedHandle.JsonValueAsync<object>(), Is.Null);
        var nullHandle = await aHandle.GetPropertyAsync("null");
        Assert.That(await nullHandle.JsonValueAsync<object>(), Is.EqualTo(null));
        var emptyhandle = await aHandle.GetPropertyAsync("empty");
        // .NET: expect(String(await emptyhandle.jsonValue())).toEqual('undefined'): undefined is null
        Assert.That(await emptyhandle.JsonValueAsync<object>(), Is.Null);
    }

    [UpstreamTest("JH-012 should work with unserializable values")]
    public async Task ShouldWorkWithUnserializableValues(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync(@"() => ({
    infinity: Infinity,
    nInfinity: -Infinity,
    nan: NaN,
    nzero: -0
  })");
        var infinityHandle = await aHandle.GetPropertyAsync("infinity");
        Assert.That(await infinityHandle.JsonValueAsync<double>(), Is.EqualTo(double.PositiveInfinity));
        var nInfinityHandle = await aHandle.GetPropertyAsync("nInfinity");
        Assert.That(await nInfinityHandle.JsonValueAsync<double>(), Is.EqualTo(double.NegativeInfinity));
        var nanHandle = await aHandle.GetPropertyAsync("nan");
        // .NET: expect(String(await nanHandle.jsonValue())).toEqual('NaN')
        Assert.That(await nanHandle.JsonValueAsync<double>(), Is.NaN);
        var nzeroHandle = await aHandle.GetPropertyAsync("nzero");
        ExpectToEqual(await nzeroHandle.JsonValueAsync<double>(), -0d);
    }

    [UpstreamTest("JH-013 getProperties should work")]
    public async Task GetPropertiesShouldWork(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync(@"() => ({
    foo: 'bar'
  })");
        var properties = await aHandle.GetPropertiesAsync();
        var foo = properties.GetValueOrDefault("foo");
        Assert.That(foo, Is.Not.Null);
        Assert.That(await foo.JsonValueAsync<string>(), Is.EqualTo("bar"));
    }

    [UpstreamTest("JH-014 getProperties should return empty map for non-objects")]
    public async Task GetPropertiesShouldReturnEmptyMapForNonObjects(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync("() => 123");
        var properties = await aHandle.GetPropertiesAsync();
        Assert.That(properties.Count, Is.EqualTo(0));
    }

    [UpstreamTest("JH-015 getProperties should return properties inhertied from super class")]
    public async Task GetPropertiesShouldReturnPropertiesInhertiedFromSuperClass(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync(@"() => {
    class A {
      constructor() {
        this.a = '1';
      }
    }
    class B extends A {
      constructor() {
        super();
        this.b = '2';
      }
    }
    return new B();
  }");
        var properties = await aHandle.GetPropertiesAsync();
        Assert.That(await properties["a"].JsonValueAsync<string>(), Is.EqualTo("1"));
        Assert.That(await properties["b"].JsonValueAsync<string>(), Is.EqualTo("2"));
    }

    [UpstreamTest("JH-016 getProperties should return own enumerable properties only")]
    public async Task GetPropertiesShouldReturnOwnEnumerablePropertiesOnly(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync(@"() => {
    class A {
      constructor() {
        this.own = 1;
        Object.defineProperty(this, 'ownHidden', { value: 2, enumerable: false });
      }
    }
    A.prototype.inherited = 3;
    return new A();
  }");
        Assert.That((await aHandle.GetPropertiesAsync()).Keys, Is.EqualTo(new[] { "own" }));

        await page.SetContentAsync("<div id=d><span>a</span><span>b</span></div>");
        var collection = await page.EvaluateHandleAsync("() => document.querySelector('#d').children");
        Assert.That((await collection.GetPropertiesAsync()).Keys, Is.EqualTo(new[] { "0", "1" }));
    }

    [UpstreamTest("JH-017 getProperties should work with elements")]
    public async Task GetPropertiesShouldWorkWithElements(IPage page)
    {
        await page.SetContentAsync("<div>Hello</div>");
        var handle = await page.EvaluateHandleAsync("() => ({ body: document.body })");
        var properties = await handle.GetPropertiesAsync();
        var body = (IElementHandle)properties.GetValueOrDefault("body");
        Assert.That(body, Is.Not.Null);
        Assert.That(await body.TextContentAsync(), Is.EqualTo("Hello"));
    }
}
