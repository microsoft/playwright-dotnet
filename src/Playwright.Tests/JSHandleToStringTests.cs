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

using System.Threading.Tasks;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.Tests
{
    [TestClass]
    public class JSHandleToStringTests : PageTestEx
    {
        [PlaywrightTest("jshandle-to-string.spec.ts", "should work for primitives")]
        public async Task ShouldWorkForPrimitives()
        {
            var numberHandle = await Page.EvaluateHandleAsync("() => 2");
            Assert.AreEqual("JSHandle@2", numberHandle.ToString());
            var stringHandle = await Page.EvaluateHandleAsync("() => 'a'");
            Assert.AreEqual("JSHandle@a", stringHandle.ToString());
        }

        [PlaywrightTest("jshandle-to-string.spec.ts", "should work for complicated objects")]
        public async Task ShouldWorkForComplicatedObjects()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => window");
            Assert.AreEqual("JSHandle@object", aHandle.ToString());
        }

        [PlaywrightTest("jshandle-to-string.spec.ts", "should work for promises")]
        public async Task ShouldWorkForPromises()
        {
            // wrap the promise in an object, otherwise we will await.
            var wrapperHandle = await Page.EvaluateHandleAsync("() => ({ b: Promise.resolve(123)})");
            var bHandle = await wrapperHandle.GetPropertyAsync("b");
            Assert.AreEqual("JSHandle@promise", bHandle.ToString());
        }

        [PlaywrightTest("jshandle-to-string.spec.ts", "should work with different subtypes")]
        public async Task ShouldWorkWithDifferentSubtypes()
        {
            Assert.AreEqual("JSHandle@function", (await Page.EvaluateHandleAsync("() => function(){}")).ToString());
            Assert.AreEqual("JSHandle@12", (await Page.EvaluateHandleAsync("12")).ToString());
            Assert.AreEqual("JSHandle@true", (await Page.EvaluateHandleAsync("true")).ToString());
            Assert.AreEqual("JSHandle@undefined", (await Page.EvaluateHandleAsync("undefined")).ToString());
            Assert.AreEqual("JSHandle@foo", (await Page.EvaluateHandleAsync("\"foo\"")).ToString());
            Assert.AreEqual("JSHandle@symbol", (await Page.EvaluateHandleAsync("Symbol()")).ToString());
            Assert.AreEqual("JSHandle@map", (await Page.EvaluateHandleAsync("new Map()")).ToString());
            Assert.AreEqual("JSHandle@set", (await Page.EvaluateHandleAsync("new Set()")).ToString());
            Assert.AreEqual("JSHandle@array", (await Page.EvaluateHandleAsync("[]")).ToString());
            Assert.AreEqual("JSHandle@null", (await Page.EvaluateHandleAsync("null")).ToString());
            Assert.AreEqual("JSHandle@regexp", (await Page.EvaluateHandleAsync("/foo/")).ToString());
            Assert.AreEqual("JSHandle@node", (await Page.EvaluateHandleAsync("document.body")).ToString());
            Assert.AreEqual("JSHandle@date", (await Page.EvaluateHandleAsync("new Date()")).ToString());
            Assert.AreEqual("JSHandle@weakmap", (await Page.EvaluateHandleAsync("new WeakMap()")).ToString());
            Assert.AreEqual("JSHandle@weakset", (await Page.EvaluateHandleAsync("new WeakSet()")).ToString());
            Assert.AreEqual("JSHandle@error", (await Page.EvaluateHandleAsync("new Error()")).ToString());
            Assert.AreEqual(TestConstants.IsWebKit ? "JSHandle@array" : "JSHandle@typedarray", (await Page.EvaluateHandleAsync("new Int32Array()")).ToString());
            Assert.AreEqual("JSHandle@proxy", (await Page.EvaluateHandleAsync("new Proxy({}, {})")).ToString());
        }
    }
}
