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

using System.ComponentModel;
using System.Dynamic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests;

public class PageEvaluateTests : PageTestEx
{
    [PlaywrightTest("page-evaluate.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        int result = await Page.EvaluateAsync<int>("() => 7 * 3");
        Assert.AreEqual(21, result);
    }

    public async Task ShouldSerializeArguments()
    {
        int result = await Page.EvaluateAsync<int>("a => a.m * a.n", new { m = 7, n = 3 });
        Assert.AreEqual(21, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should transfer NaN")]
    public async Task ShouldTransferNaN()
    {
        double result = await Page.EvaluateAsync<double>("a => a", double.NaN);
        Assert.AreEqual(double.NaN, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should transfer -0")]
    public async Task ShouldTransferNegative0()
    {
        double result = await Page.EvaluateAsync<double>("a => a", -0d);
        Assert.AreEqual(-0, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should transfer Infinity")]
    public async Task ShouldTransferInfinity()
    {
        double result = await Page.EvaluateAsync<double>("a => a", double.PositiveInfinity);
        Assert.AreEqual(double.PositiveInfinity, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should transfer -Infinity")]
    public async Task ShouldTransferNegativeInfinity()
    {
        double result = await Page.EvaluateAsync<double>("a => a", double.NegativeInfinity);
        Assert.AreEqual(double.NegativeInfinity, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip unserializable values")]
    public async Task ShouldRoundtripUnserializableValues()
    {
        dynamic value = new
        {
            infinity = double.PositiveInfinity,
            nInfinity = double.NegativeInfinity,
            nZero = -0d,
            nan = double.NaN,
        };

        dynamic result = await Page.EvaluateAsync<dynamic>("value => value", value);
        Assert.AreEqual(value.infinity, result.infinity);
        Assert.AreEqual(value.nInfinity, result.nInfinity);
        Assert.AreEqual(value.nZero, result.nZero);
        Assert.AreEqual(value.nan, result.nan);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip promise to value")]
    public async Task ShouldRoundtripPromiseToValue()
    {
        object result = await Page.EvaluateAsync<object>("value => Promise.resolve(value)", null);
        Assert.Null(result);

        double infitinity = await Page.EvaluateAsync<double>("value => Promise.resolve(value)", double.PositiveInfinity);
        Assert.AreEqual(double.PositiveInfinity, infitinity);

        double ninfitinity = await Page.EvaluateAsync<double>("value => Promise.resolve(value)", double.NegativeInfinity);
        Assert.AreEqual(double.NegativeInfinity, ninfitinity);

        double nzero = await Page.EvaluateAsync<double>("value => Promise.resolve(value)", -0d);
        Assert.AreEqual(-0, nzero);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip promise to unserializable values")]
    public async Task ShouldRoundtripPromiseToUnserializableValues()
    {
        dynamic value = new
        {
            infinity = double.PositiveInfinity,
            nInfinity = double.NegativeInfinity,
            nZero = -0d,
            nan = double.NaN,
        };

        dynamic result = await Page.EvaluateAsync<ExpandoObject>("value => Promise.resolve(value)", value);
        Assert.AreEqual(value.infinity, result.infinity);
        Assert.AreEqual(value.nInfinity, result.nInfinity);
        Assert.AreEqual(value.nZero, result.nZero);
        Assert.AreEqual(value.nan, result.nan);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should transfer arrays")]
    public async Task ShouldTransferArrays()
    {
        int[] result = await Page.EvaluateAsync<int[]>("a => a", new[] { 1, 2, 3 });
        Assert.AreEqual(new[] { 1, 2, 3 }, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should transfer arrays as arrays, not objects")]
    public async Task ShouldTransferArraysAsArraysNotObjects()
    {
        bool result = await Page.EvaluateAsync<bool>("a => Array.isArray(a)", new[] { 1, 2, 3 });
        Assert.True(result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should transfer maps as empty objects")]
    public async Task ShouldTransferMapsAsEmptyObjects()
    {
        dynamic result = await Page.EvaluateAsync<ExpandoObject>("a => a.x.constructor.name + ' ' + JSON.stringify(a.x), {x: new Map([[1, 2]])}");
        Assert.IsEmpty(TypeDescriptor.GetProperties(result));
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should modify global environment")]
    public async Task ShouldModifyGlobalEnvironment()
    {
        await Page.EvaluateAsync("() => window.globalVar = 123");
        Assert.AreEqual(123, await Page.EvaluateAsync<int>("globalVar"));
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should evaluate in the page context")]
    public async Task ShouldEvaluateInThePageContext()
    {
        await Page.GotoAsync(Server.Prefix + "/global-var.html");
        Assert.AreEqual(123, await Page.EvaluateAsync<int>("globalVar"));
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should return undefined for objects with symbols")]
    public async Task ShouldReturnUndefinedForObjectsWithSymbols()
    {
        Assert.AreEqual(new object[] { null }, await Page.EvaluateAsync<object>("() => [Symbol('foo4')]"));
        Assert.AreEqual("{\"$id\":\"1\"}", (await Page.EvaluateAsync<JsonElement>(@"() => {
                var a = { };
                a[Symbol('foo4')] = 42;
                return a;
            }")).GetRawText());
        dynamic element = await Page.EvaluateAsync<ExpandoObject>(@"() => {
                return { foo: [{ a: Symbol('foo4') }] };
            }");

        Assert.Null(element.foo[0].a);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should work with unicode chars")]
    public async Task ShouldWorkWithUnicodeChars()
    {
        int result = await Page.EvaluateAsync<int>("a => a['中文字符']", new Dictionary<string, int> { ["中文字符"] = 42 });
        Assert.AreEqual(42, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should throw when evaluation triggers reload")]
    public async Task ShouldThrowWhenEvaluationTriggersReload()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync<object>(@"() => {
                location.reload();
                return new Promise(() => { });
            }"));
        StringAssert.Contains("navigation", exception.Message);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should await promise")]
    public async Task ShouldAwaitPromise()
    {
        int result = await Page.EvaluateAsync<int>("() => Promise.resolve(8 * 7)");
        Assert.AreEqual(56, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should work right after framenavigated")]
    public async Task ShouldWorkRightAfterFrameNavigated()
    {
        Task<int> frameEvaluation = null;
        Page.FrameNavigated += (_, e) =>
        {
            frameEvaluation = e.EvaluateAsync<int>("() => 6 * 7");
        };
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(42, await frameEvaluation);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should work right after a cross-origin navigation")]
    public async Task ShouldWorkRightAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Task<int> frameEvaluation = null;
        Page.FrameNavigated += (_, e) =>
        {
            frameEvaluation = e.EvaluateAsync<int>("() => 6 * 7");
        };
        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        Assert.AreEqual(42, await frameEvaluation);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should work from-inside an exposed function")]
    public async Task ShouldWorkFromInsideAnExposedFunction()
    {
        // Setup inpage callback, which calls Page.evaluate
        await Page.ExposeFunctionAsync("callController", async (int a, int b) => await Page.EvaluateAsync<int>("({a, b}) => a * b", new { a, b }));
        int result = await Page.EvaluateAsync<int>(@"async function() {
                return await callController(9, 3);
            }");
        Assert.AreEqual(27, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should reject promise with exception")]
    public async Task ShouldRejectPromiseWithException()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync("() => not_existing_object.property"));
        StringAssert.Contains("not_existing_object", exception.Message);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should support thrown strings as error messages")]
    public async Task ShouldSupportThrownStringsAsErrorMessages()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync("() => { throw 'qwerty'; }"));
        StringAssert.Contains("qwerty", exception.Message);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should support thrown numbers as error messages")]
    public async Task ShouldSupportThrownNumbersAsErrorMessages()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync("() => { throw 100500; }"));
        StringAssert.Contains("100500", exception.Message);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should return complex objects")]
    public async Task ShouldReturnComplexObjects()
    {
        var obj = new { foo = "bar!" };
        var result = await Page.EvaluateAsync<JsonElement>("a => a", obj);
        Assert.AreEqual("bar!", result.GetProperty("foo").GetString());
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should return NaN")]
    public async Task ShouldReturnNaN()
    {
        double result = await Page.EvaluateAsync<double>("() => NaN");
        Assert.AreEqual(double.NaN, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should return -0")]
    public async Task ShouldReturnNegative0()
    {
        Assert.AreEqual(-0, (await Page.EvaluateAsync<double>("() => -0")));
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should return Infinity")]
    public async Task ShouldReturnInfinity()
    {
        double result = await Page.EvaluateAsync<double>("() => Infinity");
        Assert.AreEqual(double.PositiveInfinity, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should return -Infinity")]
    public async Task ShouldReturnNegativeInfinity()
    {
        double result = await Page.EvaluateAsync<double>("() => -Infinity");
        Assert.AreEqual(double.NegativeInfinity, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should work with overwritten Promise")]
    public async Task ShouldWorkWithOverwrittenPromise()
    {
        await Page.EvaluateAsync(@"() => {
              const originalPromise = window.Promise;
              class Promise2 {
                static all(...arg) {
                  return wrap(originalPromise.all(...arg));
                }
                static race(...arg) {
                  return wrap(originalPromise.race(...arg));
                }
                static resolve(...arg) {
                  return wrap(originalPromise.resolve(...arg));
                }
                constructor(f, r) {
                  this._promise = new originalPromise(f, r);
                }
                then(f, r) {
                  return wrap(this._promise.then(f, r));
                }
                catch(f) {
                  return wrap(this._promise.catch(f));
                }
                finally(f) {
                  return wrap(this._promise.finally(f));
                }
              };
              const wrap = p => {
                const result = new Promise2(() => {}, () => {});
                result._promise = p;
                return result;
              };
              window.Promise = Promise2;
              window.__Promise2 = Promise2;
            }");

        Assert.True(await Page.EvaluateAsync<bool>(@"() => {
              const p = Promise.all([Promise.race([]), new Promise(() => {}).then(() => {})]);
              return p instanceof window.__Promise2;
            }"));
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => Promise.resolve(42)"));
    }

    [PlaywrightTest("page-evaluate.spec.ts", @"should accept ""undefined"" as one of multiple parameters")]
    public async Task ShouldAcceptUndefinedAsOneOfMultipleParameters()
    {
        //C# will send nulls
        bool result = await Page.EvaluateAsync<bool>(@"({a, b}) => {
                console.log(a);
                console.log(b);
                return Object.is (a, null) && Object.is (b, 'foo')
            }", new { a = (object)null, b = "foo" });
        Assert.True(result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should properly serialize undefined fields")]
    public async Task ShouldProperlySerializeUndefinedFields()
    {
        dynamic result = await Page.EvaluateAsync<ExpandoObject>("() => ({a: undefined})");
        Assert.Null(result.a);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should properly serialize null arguments")]
    public async Task ShouldProperlySerializeNullArguments()
            => Assert.Null(await Page.EvaluateAsync<JsonDocument>("x => x", null));

    [PlaywrightTest("page-evaluate.spec.ts", "should properly serialize null fields")]
    public async Task ShouldProperlySerializeNullFields()
    {
        dynamic result = await Page.EvaluateAsync<ExpandoObject>("() => ({ a: null})");
        Assert.Null(result.a);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should return undefined for non-serializable objects")]
    public async Task ShouldReturnUndefinedForNonSerializableObjects()
        => Assert.NotNull(await Page.EvaluateAsync<object>("() => window"));

    [PlaywrightTest("page-evaluate.spec.ts", "should work for circular object")]
    public async Task ShouldWorkForCircularObject()
    {
        object result = await Page.EvaluateAsync<object>(@"() => {
                var a = { };
                a.b = a;
                return a;
            }");
        Assert.NotNull(result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should accept a string with comments")]
    public async Task ShouldAcceptAStringWithComments()
    {
        int result = await Page.EvaluateAsync<int>("2 + 5;\n// do some math!");
        Assert.AreEqual(7, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should accept element handle as an argument")]
    public async Task ShouldAcceptElementHandleAsAnArgument()
    {
        await Page.SetContentAsync("<section>42</section>");
        var element = await Page.QuerySelectorAsync("section");
        string text = await Page.EvaluateAsync<string>("e => e.textContent", element);
        Assert.AreEqual("42", text);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should throw if underlying element was disposed")]
    public async Task ShouldThrowIfUnderlyingElementWasDisposed()
    {
        await Page.SetContentAsync("<section>39</section>");
        var element = await Page.QuerySelectorAsync("section");
        Assert.NotNull(element);
        await element.DisposeAsync();

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync("e => e.textContent", element));
        StringAssert.Contains("JSHandle is disposed", exception.Message);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should simulate a user gesture")]
    public async Task ShouldSimulateAUserGesture()
    {
        bool result = await Page.EvaluateAsync<bool>(@"() => {
                document.body.appendChild(document.createTextNode('test'));
                document.execCommand('selectAll');
                return document.execCommand('copy');
            }");
        Assert.True(result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should throw a nice error after a navigation")]
    public async Task ShouldThrowANiceErrorAfterANavigation()
    {
        var evaluateTask = Page.EvaluateAsync("() => new Promise(f => window.__resolve = f)");
        await TaskUtils.WhenAll(
            Page.WaitForNavigationAsync(),
            Page.EvaluateAsync(@"() => {
                    window.location.reload();
                    setTimeout(() => window.__resolve(42), 1000);
                }")
        );
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => evaluateTask);
        StringAssert.Contains("navigation", exception.Message);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should not throw an error when evaluation does a navigation")]
    public async Task ShouldNotThrowAnErrorWhenEvaluationDoesANavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        int[] result = await Page.EvaluateAsync<int[]>(@"() => {
                window.location = '/empty.html';
                return [42];
            }");
        Assert.AreEqual(new[] { 42 }, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should not throw an error when evaluation does a synchronous navigation and returns an object")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldNotThrowAnErrorWhenEvaluationDoesASynchronousNavigationAndReturnsAnObject()
    {
        var result = await Page.EvaluateAsync<JsonElement>(@"() => {
                window.location.reload();
                return {a: 42};
            }");
        Assert.AreEqual(42, result.GetProperty("a").GetInt32());
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should not throw an error when evaluation does a synchronous navigation and returns an undefined")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldNotThrowAnErrorWhenEvaluationDoesASynchronousNavigationAndReturnsUndefined()
    {
        var result = await Page.EvaluateAsync<JsonElement?>(@"() => {
                window.location.reload();
                return undefined;
            }");
        Assert.Null(result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should transfer 100Mb of data from page to node.js")]
    public async Task ShouldTransfer100MbOfDataFromPageToNodeJs()
    {
        string a = await Page.EvaluateAsync<string>("() => Array(100 * 1024 * 1024 + 1).join('a')");
        Assert.AreEqual(100 * 1024 * 1024, a.Length);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should throw error with detailed information on exception inside promise ")]
    public async Task ShouldThrowErrorWithDetailedInformationOnExceptionInsidePromise()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync<object>(@"() => new Promise(() => {
                throw new Error('Error in promise');
            })"));
        StringAssert.Contains("Error in promise", exception.Message);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should work even when JSON is set to null")]
    public async Task ShouldWorkEvenWhenJSONIsSetToNull()
    {
        await Page.EvaluateAsync<object>("() => { window.JSON.stringify = null; window.JSON = null; }");
        var result = await Page.EvaluateAsync<JsonElement>("() => ({ abc: 123})");
        Assert.AreEqual(123, result.GetProperty("abc").GetInt32());
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should await promise from popup")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldAwaitPromiseFromPopup()
    {
        await Page.GotoAsync(Server.EmptyPage);

        int result = await Page.EvaluateAsync<int>(@"() => {
                const win = window.open('about:blank');
                return new win.Promise(f => f(42));
            }");
        Assert.AreEqual(42, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should work with non-strict expressions")]
    public async Task ShouldWorkWithNonStrictExpressions()
    {
        Assert.AreEqual(3.14m, await Page.EvaluateAsync<decimal>(@"() => {
              y = 3.14;
              return y;
            }"));
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should respect use strict expression")]
    public async Task ShouldRespectUseStrictExpression()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync<object>(@"() => {
                ""use strict"";
                variableY = 3.14;
               return variableY;
            }"));
        StringAssert.Contains("variableY", exception.Message);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should not leak utility script")]
    public async Task ShouldNotLeakUtilityScript()
    {
        Assert.True(await Page.EvaluateAsync<bool>(@"() => this === window"));
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should not leak handles")]
    public async Task ShouldNotLeakHandles()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync<object>(@"() => handles.length"));
        StringAssert.Contains("handles", exception.Message);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should evaluate exception")]
    public async Task ShouldEvaluateException()
    {
        string exception = await Page.EvaluateAsync<string>(@"() => {
                return (function functionOnStack() {
                    return new Error('error message');
                })();
            }");
        StringAssert.Contains("Error: error message", exception);
        StringAssert.Contains("functionOnStack", exception);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should evaluate exception")]
    public async Task ShouldEvaluateException2()
    {
        string exception = await Page.EvaluateAsync<string>(@"() => new Error('error message')");
        StringAssert.Contains("Error: error message", exception);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should evaluate date")]
    public async Task ShouldEvaluateDate()
    {
        dynamic result = await Page.EvaluateAsync<ExpandoObject>(@"() => ({ date: new Date('2020-05-27T01:31:38.506Z') })");
        Assert.AreEqual(new DateTime(2020, 05, 27, 1, 31, 38, 506), result.date);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip date")]
    public async Task ShouldRoundtripDate()
    {
        var date = new DateTime(2020, 05, 27, 1, 31, 38, 506, DateTimeKind.Utc);
        var result = await Page.EvaluateAsync<DateTime>(@"date => date", date);
        Assert.AreEqual(date, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should evaluate url")]
    public async Task ShouldEvaluateUrl()
    {
        dynamic result = await Page.EvaluateAsync<ExpandoObject>(@"() => ({ someKey: new URL('https://example.com') })");
        Assert.AreEqual(new Uri("https://example.com"), result.someKey);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip url")]
    public async Task ShouldRoundtripUrl()
    {
        var uri = new Uri("https://example.com");
        var result = await Page.EvaluateAsync<Uri>("url => url", uri);
        Assert.AreEqual(uri, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip complex url")]
    public async Task ShouldRoundtripComplexUrl()
    {
        var uri = new Uri("https://user:password@www.contoso.com:80/Home/Index.htm?q1=v1&q2=v2#FragmentName");
        var result = await Page.EvaluateAsync<Uri>("url => url", uri);
        Assert.AreEqual(uri, result);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should jsonValue() url")]
    public async Task ShouldJsonValueUrl()
    {
        var resultHandle = await Page.EvaluateHandleAsync("() => ({ url: new URL('https://example.com') })");
        dynamic result = await resultHandle.JsonValueAsync<ExpandoObject>();
        Assert.AreEqual(new Uri("https://example.com"), result.url);
    }

    [PlaywrightTest()]
    public async Task ShouldTreatEcma2020AsFunctions()
         => Assert.AreEqual("dario", await Page.EvaluateAsync<string>(
             @"() => {
                    const person = { name: 'dario' };
                    return person?.name;
                }"));

    [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip regex")]
    public async Task ShouldRoundtripRegex()
    {
        var regex = new Regex("hello", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var result = await Page.EvaluateAsync<Regex>("regex => regex", regex);
        Assert.AreEqual(regex.ToString(), result.ToString());
        Assert.AreEqual(regex.Options, result.Options);
    }

    [PlaywrightTest("page-evaluate.spec.ts", "should jsonValue() date")]
    public async Task ShouldJsonValueDate()
    {
        var resultHandle = await Page.EvaluateHandleAsync(@"() => ({ date: new Date('2020-05-27T01:31:38.506Z') })");
        dynamic result = await resultHandle.JsonValueAsync<ExpandoObject>();
        Assert.AreEqual(new DateTime(2020, 05, 27, 1, 31, 38, 506), result.date);
    }

    public async Task ShouldSerializeEnumProperty()
    {
        int result = await Page.EvaluateAsync<int>("a => a.TestEnum", new ClassWithEnumProperty());
        Assert.AreEqual(1, result);
    }

    private class ClassWithEnumProperty
    {
        public TestEnum TestEnum { get; set; } = TestEnum.Test;
    }

    private enum TestEnum
    {
        Test = 1
    }

    [PlaywrightTest(Description = "https://github.com/microsoft/playwright-dotnet/issues/1706")]
    public async Task ShouldNotReturnDisposedJsonElement()
    {
        var result = await Page.EvaluateAsync<JsonElement?>("()=> [{a:1,b:2},{a:1,b:2}]");
        Assert.AreEqual("[{\"$id\":\"1\",\"a\":1,\"b\":2},{\"$id\":\"2\",\"a\":1,\"b\":2}]", result.ToString());
    }

    [PlaywrightTest()]
    public async Task ShouldAllowJsonElementWhenDeserializing()
    {
        JsonElement? result = null;

        result = await Page.EvaluateAsync<JsonElement?>("() => [{a:1,b:2},{a:1,b:2}]"); // list
        Assert.AreEqual("[{\"$id\":\"1\",\"a\":1,\"b\":2},{\"$id\":\"2\",\"a\":1,\"b\":2}]", result.ToString());
        result = await Page.EvaluateAsync<JsonElement>("() => [{a:1,b:2},{a:1,b:2}]");
        Assert.AreEqual("[{\"$id\":\"1\",\"a\":1,\"b\":2},{\"$id\":\"2\",\"a\":1,\"b\":2}]", result.ToString());

        result = await Page.EvaluateAsync<JsonElement?>("() => ({a:1,b:2})"); // object
        Assert.AreEqual("{\"$id\":\"1\",\"a\":1,\"b\":2}", result.ToString());
        result = await Page.EvaluateAsync<JsonElement>("() => ({a:1,b:2})");
        Assert.AreEqual("{\"$id\":\"1\",\"a\":1,\"b\":2}", result.ToString());

        result = await Page.EvaluateAsync<JsonElement?>("() => 42"); // number
        Assert.AreEqual("42", result.ToString());
        result = await Page.EvaluateAsync<JsonElement>("() => 42");
        Assert.AreEqual("42", result.ToString());

        result = await Page.EvaluateAsync<JsonElement?>("() => 'kek'"); // string
        Assert.AreEqual("kek", result.ToString());
        result = await Page.EvaluateAsync<JsonElement>("() => 'kek'");
        Assert.AreEqual("kek", result.ToString());
    }

    private class Shape
    {
        public int Width { get; set; } = default!;
        public int Height { get; set; } = default!;
    }

    [PlaywrightTest()]
    public async Task ShouldParseTypeProperties()
    {
        var result = await Page.EvaluateAsync<Shape>("() => ({ width: 600, height: 400 })");
        Assert.AreEqual(600, result.Width);
        Assert.AreEqual(400, result.Height);
    }
}
