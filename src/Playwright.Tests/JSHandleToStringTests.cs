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

namespace Microsoft.Playwright.Tests;

public class JSHandleToStringTests : PageTestEx
{
    [PlaywrightTest("jshandle-to-string.spec.ts", "should work for primitives")]
    public async Task ShouldWorkForPrimitives()
    {
        var numberHandle = await Page.EvaluateHandleAsync("() => 2");
        Assert.AreEqual("2", numberHandle.ToString());
        var stringHandle = await Page.EvaluateHandleAsync("() => 'a'");
        Assert.AreEqual("a", stringHandle.ToString());
    }

    [PlaywrightTest("jshandle-to-string.spec.ts", "should work for complicated objects")]
    public async Task ShouldWorkForComplicatedObjects()
    {
        var aHandle = await Page.EvaluateHandleAsync("() => window");
        if (TestConstants.IsFirefox)
        {
            Assert.AreEqual("JSHandle@object", aHandle.ToString());
        }
        else
        {
            Assert.AreEqual("Window", aHandle.ToString());
        }
    }

    [PlaywrightTest("jshandle-to-string.spec.ts", "should work for promises")]
    public async Task ShouldWorkForPromises()
    {
        // wrap the promise in an object, otherwise we will await.
        var wrapperHandle = await Page.EvaluateHandleAsync("() => ({ b: Promise.resolve(123)})");
        var bHandle = await wrapperHandle.GetPropertyAsync("b");
        Assert.AreEqual("Promise", bHandle.ToString());
    }

    [PlaywrightTest("jshandle-to-string.spec.ts", "should work with different subtypes")]
    public async Task ShouldWorkWithDifferentSubtypes()
    {
        StringAssert.Contains("function", (await Page.EvaluateHandleAsync("() => function(){}")).ToString());
        Assert.AreEqual("12", (await Page.EvaluateHandleAsync("12")).ToString());
        Assert.AreEqual("true", (await Page.EvaluateHandleAsync("true")).ToString());
        Assert.AreEqual("undefined", (await Page.EvaluateHandleAsync("undefined")).ToString());
        Assert.AreEqual("foo", (await Page.EvaluateHandleAsync("\"foo\"")).ToString());
        Assert.AreEqual("Symbol()", (await Page.EvaluateHandleAsync("Symbol()")).ToString());
        StringAssert.Contains("Map", (await Page.EvaluateHandleAsync("new Map()")).ToString());
        StringAssert.Contains("Set", (await Page.EvaluateHandleAsync("new Set()")).ToString());
        StringAssert.Contains("Array", (await Page.EvaluateHandleAsync("[]")).ToString());
        Assert.AreEqual("null", (await Page.EvaluateHandleAsync("null")).ToString());
        Assert.AreEqual("JSHandle@node", (await Page.EvaluateHandleAsync("document.body")).ToString());
        Assert.AreEqual("WeakMap", (await Page.EvaluateHandleAsync("new WeakMap()")).ToString());
        Assert.AreEqual("WeakSet", (await Page.EvaluateHandleAsync("new WeakSet()")).ToString());
        StringAssert.Contains("Error", (await Page.EvaluateHandleAsync("new Error()")).ToString());
        Assert.AreEqual("Proxy", (await Page.EvaluateHandleAsync("new Proxy({}, {})")).ToString());
    }
}
