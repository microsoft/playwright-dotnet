/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Dynamic;
using System.Text.Json;

namespace Microsoft.Playwright.Tests;

public class PageEvaluateHandleTests : PageTestEx
{
    [PlaywrightTest("page-evaluate-handle.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        var windowHandle = await Page.EvaluateHandleAsync("() => window");
        Assert.NotNull(windowHandle);
    }

    [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept object handle as an argument")]
    public async Task ShouldAcceptObjectHandleAsAnArgument()
    {
        var navigatorHandle = await Page.EvaluateHandleAsync("() => navigator");
        string text = await Page.EvaluateAsync<string>("e => e.userAgent", navigatorHandle);
        StringAssert.Contains("Mozilla", text);
    }

    [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept object handle to primitive types")]
    public async Task ShouldAcceptObjectHandleToPrimitiveTypes()
    {
        var aHandle = await Page.EvaluateHandleAsync("() => 5");
        bool isFive = await Page.EvaluateAsync<bool>("e => Object.is (e, 5)", aHandle);
        Assert.True(isFive);
    }

    [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept nested handle")]
    public async Task ShouldAcceptNestedHandle()
    {
        var foo = await Page.EvaluateHandleAsync("() => ({x: 1, y: 'foo'})");
        dynamic result = await Page.EvaluateAsync<ExpandoObject>("({ foo }) => foo", new { foo });

        Assert.AreEqual(1, result.x);
        Assert.AreEqual("foo", result.y);
    }

    [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept nested window handle")]
    public async Task ShouldAcceptNestedWindowHandle()
    {
        var foo = await Page.EvaluateHandleAsync("() => window");
        Assert.True(await Page.EvaluateAsync<bool>("({ foo }) => foo === window", new { foo }));
    }

    [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept multiple nested handles")]
    public async Task ShouldAcceptMultipleNestedHandles()
    {
        var foo = await Page.EvaluateHandleAsync("() => ({ x: 1, y: 'foo' })");
        var bar = await Page.EvaluateHandleAsync("() => 5");
        var baz = await Page.EvaluateHandleAsync("() => ['baz']");

        string result = await Page.EvaluateAsync<string>(
            "x => JSON.stringify(x)",
            new
            {
                a1 = new { foo },
                a2 = new
                {
                    bar,
                    arr = new[] { new { baz } },
                },
            });

        var json = JsonDocument.Parse(result).RootElement;

        Assert.AreEqual(1, json.GetProperty("a1").GetProperty("foo").GetProperty("x").GetInt32());
        Assert.AreEqual("foo", json.GetProperty("a1").GetProperty("foo").GetProperty("y").ToString());
        Assert.AreEqual(5, json.GetProperty("a2").GetProperty("bar").GetInt32());
        Assert.AreEqual("baz", json.GetProperty("a2").GetProperty("arr").EnumerateArray().First().GetProperty("baz").EnumerateArray().First().ToString());
    }

    [PlaywrightTest("page-evaluate-handle.spec.ts", "should be able to serialize circular data structures")]
    public async Task ShouldBeAbleToSerializeCircularDataStructures()
    {
        {
            // ExpandoObject
            dynamic a = new ExpandoObject();
            a.b = a;
            var wasCorrectlySerialized = await Page.EvaluateAsync<bool>("a => a.b === a", a);
            Assert.True(wasCorrectlySerialized);
        }
        {
            // objects
            var a = new Dictionary<string, object>();
            a["b"] = a;
            var wasCorrectlySerialized = await Page.EvaluateAsync<bool>("a => a.b === a", a);
            Assert.True(wasCorrectlySerialized);
        }

        {
            // lists
            var a = new List<object>();
            a.Add(a);
            var wasCorrectlySerialized = await Page.EvaluateAsync<bool>("a => window.a = a.length === 1 && a[0] && a[0] === a", a);
            Assert.True(wasCorrectlySerialized);
        }
    }

    [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept same nested object multiple times")]
    public async Task ShouldAcceptSameNestedObjectMultipleTimes()
    {
        dynamic foo = new { x = 1 };
        string result = await Page.EvaluateAsync<string>(
            "x => JSON.stringify(x)",
            new
            {
                foo,
                bar = new[] { foo },
                baz = new { foo },
            });

        var json = JsonDocument.Parse(result).RootElement;

        Assert.AreEqual(1, json.GetProperty("foo").GetProperty("x").GetInt32());
        Assert.AreEqual(1, json.GetProperty("bar").EnumerateArray().First().GetProperty("x").GetInt32());
        Assert.AreEqual(1, json.GetProperty("baz").GetProperty("foo").GetProperty("x").GetInt32());
    }

    [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept object handle to unserializable value")]
    public async Task ShouldAcceptObjectHandleToUnserializableValue()
    {
        var aHandle = await Page.EvaluateHandleAsync("() => Infinity");
        Assert.True(await Page.EvaluateAsync<bool>("e => Object.is(e, Infinity)", aHandle));
    }

    [PlaywrightTest("page-evaluate-handle.spec.ts", "should pass configurable args")]
    public async Task ShouldPassConfigurableArgs()
    {
        dynamic parsed = new ExpandoObject();
        parsed.b = parsed;
        JsonElement result = await Page.EvaluateAsync<JsonElement>(
           @"arg =>{
                  if (arg.foo !== 42)
                    throw new Error('Not a 42');
                  arg.foo = 17;
                  if (arg.foo !== 17)
                    throw new Error('Not 17');
                  delete arg.foo;
                  if (arg.foo === 17)
                    throw new Error('Still 17');
                  return arg;
                }",
            new { foo = 42 });

        Assert.AreEqual("{\"$id\":\"1\"}", result.ToString());
    }

    [PlaywrightTest("page-evaluate-handle.spec.ts", "should work with primitives")]
    public async Task ShouldWorkWithPrimitives()
    {
        var aHandle = await Page.EvaluateHandleAsync(@"() => {
                window.FOO = 123;
                return window;
            }");
        Assert.AreEqual(123, await Page.EvaluateAsync<int>("e => e.FOO", aHandle));
    }
}
