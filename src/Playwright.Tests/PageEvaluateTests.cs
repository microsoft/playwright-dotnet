using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Testing.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEvaluateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            int result = await Page.EvaluateAsync<int>("() => 7 * 3");
            Assert.Equal(21, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should transfer NaN")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTransferNaN()
        {
            double result = await Page.EvaluateAsync<double>("a => a", double.NaN);
            Assert.Equal(double.NaN, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should transfer -0")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTransferNegative0()
        {
            double result = await Page.EvaluateAsync<double>("a => a", -0d);
            Assert.True(result.IsNegativeZero());
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should transfer Infinity")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTransferInfinity()
        {
            double result = await Page.EvaluateAsync<double>("a => a", double.PositiveInfinity);
            Assert.Equal(double.PositiveInfinity, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should transfer -Infinity")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTransferNegativeInfinity()
        {
            double result = await Page.EvaluateAsync<double>("a => a", double.NegativeInfinity);
            Assert.Equal(double.NegativeInfinity, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip unserializable values")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Equal(value.infinity, result.infinity);
            Assert.Equal(value.nInfinity, result.nInfinity);
            Assert.Equal(value.nZero, result.nZero);
            Assert.Equal(value.nan, result.nan);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip promise to value")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRoundtripPromiseToValue()
        {
            object result = await Page.EvaluateAsync<object>("value => Promise.resolve(value)", null);
            Assert.Null(result);

            double infitinity = await Page.EvaluateAsync<double>("value => Promise.resolve(value)", double.PositiveInfinity);
            Assert.Equal(double.PositiveInfinity, infitinity);

            double ninfitinity = await Page.EvaluateAsync<double>("value => Promise.resolve(value)", double.NegativeInfinity);
            Assert.Equal(double.NegativeInfinity, ninfitinity);

            double nzero = await Page.EvaluateAsync<double>("value => Promise.resolve(value)", -0d);
            Assert.Equal(-0, nzero);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip promise to unserializable values")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Equal(value.infinity, result.infinity);
            Assert.Equal(value.nInfinity, result.nInfinity);
            Assert.Equal(value.nZero, result.nZero);
            Assert.Equal(value.nan, result.nan);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should transfer arrays")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTransferArrays()
        {
            int[] result = await Page.EvaluateAsync<int[]>("a => a", new[] { 1, 2, 3 });
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should transfer arrays as arrays, not objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTransferArraysAsArraysNotObjects()
        {
            bool result = await Page.EvaluateAsync<bool>("a => Array.isArray(a)", new[] { 1, 2, 3 });
            Assert.True(result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should transfer maps as empty objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTransferMapsAsEmptyObjects()
        {
            dynamic result = await Page.EvaluateAsync<ExpandoObject>("a => a.x.constructor.name + ' ' + JSON.stringify(a.x), {x: new Map([[1, 2]])}");
            Assert.Empty(TypeDescriptor.GetProperties(result));
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should modify global environment")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldModifyGlobalEnvironment()
        {
            await Page.EvaluateAsync("() => window.globalVar = 123");
            Assert.Equal(123, await Page.EvaluateAsync<int>("globalVar"));
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should evaluate in the page context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldEvaluateInThePageContext()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/global-var.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("globalVar"));
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should return undefined for objects with symbols")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnUndefinedForObjectsWithSymbols()
        {
            Assert.Equal(new object[] { null }, await Page.EvaluateAsync<object>("() => [Symbol('foo4')]"));
            Assert.Equal("{}", (await Page.EvaluateAsync<JsonElement>(@"() => {
                var a = { };
                a[Symbol('foo4')] = 42;
                return a;
            }")).ToJson());
            dynamic element = await Page.EvaluateAsync<ExpandoObject>(@"() => {
                return { foo: [{ a: Symbol('foo4') }] };
            }");

            Assert.Null(element.foo[0].a);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work with function shorthands")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldWorkWithFunctionShorthands()
        {
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work with unicode chars")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUnicodeChars()
        {
            int result = await Page.EvaluateAsync<int>("a => a['中文字符']", new Dictionary<string, int> { ["中文字符"] = 42 });
            Assert.Equal(42, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should throw when evaluation triggers reload")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenEvaluationTriggersReload()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync<object>(@"() => {
                location.reload();
                return new Promise(() => { });
            }"));
            Assert.Contains("navigation", exception.Message);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should await promise")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAwaitPromise()
        {
            int result = await Page.EvaluateAsync<int>("() => Promise.resolve(8 * 7)");
            Assert.Equal(56, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work right after framenavigated")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkRightAfterFrameNavigated()
        {
            Task<int> frameEvaluation = null;
            Page.FrameNavigated += (_, e) =>
            {
                frameEvaluation = e.EvaluateAsync<int>("() => 6 * 7");
            };
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(42, await frameEvaluation);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work right after a cross-origin navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkRightAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Task<int> frameEvaluation = null;
            Page.FrameNavigated += (_, e) =>
            {
                frameEvaluation = e.EvaluateAsync<int>("() => 6 * 7");
            };
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.Equal(42, await frameEvaluation);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work from-inside an exposed function")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkFromInsideAnExposedFunction()
        {
            // Setup inpage callback, which calls Page.evaluate
            await Page.ExposeFunctionAsync("callController", async (int a, int b) => await Page.EvaluateAsync<int>("({a, b}) => a * b", new { a, b }));
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await callController(9, 3);
            }");
            Assert.Equal(27, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should reject promise with exception")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectPromiseWithException()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => not_existing_object.property"));
            Assert.Contains("not_existing_object", exception.Message);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should support thrown strings as error messages")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportThrownStringsAsErrorMessages()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => { throw 'qwerty'; }"));
            Assert.Contains("qwerty", exception.Message);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should support thrown numbers as error messages")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportThrownNumbersAsErrorMessages()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => { throw 100500; }"));
            Assert.Contains("100500", exception.Message);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should return complex objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnComplexObjects()
        {
            var obj = new { foo = "bar!" };
            var result = await Page.EvaluateAsync<JsonElement>("a => a", obj);
            Assert.Equal("bar!", result.GetProperty("foo").GetString());
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should return NaN")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNaN()
        {
            double result = await Page.EvaluateAsync<double>("() => NaN");
            Assert.Equal(double.NaN, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should return -0")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNegative0()
            => Assert.True((await Page.EvaluateAsync<double>("() => -0")).IsNegativeZero());

        [PlaywrightTest("page-evaluate.spec.ts", "should return Infinity")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnInfinity()
        {
            double result = await Page.EvaluateAsync<double>("() => Infinity");
            Assert.Equal(double.PositiveInfinity, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should return -Infinity")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNegativeInfinity()
        {
            double result = await Page.EvaluateAsync<double>("() => -Infinity");
            Assert.Equal(double.NegativeInfinity, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work with overwritten Promise")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithOverwrittenPromise()
        {
            await Page.EvaluateAsync(@"const originalPromise = window.Promise;
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
              window.__Promise2 = Promise2;");

            Assert.True(await Page.EvaluateAsync<bool>(@"() => {
              const p = Promise.all([Promise.race([]), new Promise(() => {}).then(() => {})]);
              return p instanceof window.__Promise2;
            }"));
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => Promise.resolve(42)"));
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should throw when passed more than one parameter")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldThrowWhenPassedMoreThanOneParameter()
        {
        }

        [PlaywrightTest("page-evaluate.spec.ts", @"should accept ""undefined"" as one of multiple parameters")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-evaluate.spec.ts", "should properly serialize undefined arguments")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldProperlySerializeUndefinedArguments()
        {
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should properly serialize undefined fields")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldProperlySerializeUndefinedFields()
        {
            dynamic result = await Page.EvaluateAsync<ExpandoObject>("() => ({a: undefined})");
            Assert.Null(result.a);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should properly serialize null arguments")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldProperlySerializeNullArguments()
                => Assert.Null(await Page.EvaluateAsync<JsonDocument>("x => x", null));

        [PlaywrightTest("page-evaluate.spec.ts", "should properly serialize null fields")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldProperlySerializeNullFields()
        {
            dynamic result = await Page.EvaluateAsync<ExpandoObject>("() => ({ a: null})");
            Assert.Null(result.a);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should return undefined for non-serializable objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnUndefinedForNonSerializableObjects()
            => Assert.Null(await Page.EvaluateAsync<object>("() => window"));

        [PlaywrightTest("page-evaluate.spec.ts", "should fail for circular object")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailForCircularObject()
        {
            object result = await Page.EvaluateAsync<object>(@"() => {
                var a = { };
                var b = { a };
                a.b = b;
                return a;
            }");
            Assert.Null(result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should be able to throw a tricky error")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToThrowATrickyError()
        {
            var windowHandle = await Page.EvaluateHandleAsync("() => window");
            var exceptionText = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => windowHandle.JsonValueAsync<object>());
            var error = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.EvaluateAsync<JsonElement>(@"errorText => {
                throw new Error(errorText);
            }", exceptionText.Message));
            Assert.Contains(exceptionText.Message, error.Message);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should accept a string")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldAcceptAString()
        {
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should accept a string with semi colons")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldAcceptAStringWithSemiColons()
        {
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should accept a string with comments")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptAStringWithComments()
        {
            int result = await Page.EvaluateAsync<int>("2 + 5;\n// do some math!");
            Assert.Equal(7, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should accept element handle as an argument")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptElementHandleAsAnArgument()
        {
            await Page.SetContentAsync("<section>42</section>");
            var element = await Page.QuerySelectorAsync("section");
            string text = await Page.EvaluateAsync<string>("e => e.textContent", element);
            Assert.Equal("42", text);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should throw if underlying element was disposed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowIfUnderlyingElementWasDisposed()
        {
            await Page.SetContentAsync("<section>39</section>");
            var element = await Page.QuerySelectorAsync("section");
            Assert.NotNull(element);
            await element.DisposeAsync();

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("e => e.textContent", element));
            Assert.Contains("JSHandle is disposed", exception.Message);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should simulate a user gesture")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowANiceErrorAfterANavigation()
        {
            var exceptionTask = Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => new Promise(f => window.__resolve = f)"));
            await TaskUtils.WhenAll(
                Page.WaitForNavigationAsync(),
                Page.EvaluateAsync(@"() => {
                    window.location.reload();
                    setTimeout(() => window.__resolve(42), 1000);
                }")
            );
            var exception = await exceptionTask;
            Assert.Contains("navigation", exception.Message);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should not throw an error when evaluation does a navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotThrowAnErrorWhenEvaluationDoesANavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            int[] result = await Page.EvaluateAsync<int[]>(@"() => {
                window.location = '/empty.html';
                return [42];
            }");
            Assert.Equal(new[] { 42 }, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should not throw an error when evaluation does a synchronous navigation and returns an object")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldNotThrowAnErrorWhenEvaluationDoesASynchronousNavigationAndReturnsAnObject()
        {
            var result = await Page.EvaluateAsync<JsonElement>(@"() => {
                window.location.reload();
                return {a: 42};
            }");
            Assert.Equal(42, result.GetProperty("a").GetInt32());
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should not throw an error when evaluation does a synchronous navigation and returns an undefined")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldNotThrowAnErrorWhenEvaluationDoesASynchronousNavigationAndReturnsUndefined()
        {
            var result = await Page.EvaluateAsync<JsonElement?>(@"() => {
                window.location.reload();
                return undefined;
            }");
            Assert.Null(result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should transfer 100Mb of data from page to node.js")]
        [Fact(Skip = "SKIP WIRE")]
        public async Task ShouldTransfer100MbOfDataFromPageToNodeJs()
        {
            string a = await Page.EvaluateAsync<string>("() => Array(100 * 1024 * 1024 + 1).join('a')");
            Assert.Equal(100 * 1024 * 1024, a.Length);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should throw error with detailed information on exception inside promise ")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowErrorWithDetailedInformationOnExceptionInsidePromise()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync<object>(@"() => new Promise(() => {
                throw new Error('Error in promise');
            })"));
            Assert.Contains("Error in promise", exception.Message);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work even when JSON is set to null")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkEvenWhenJSONIsSetToNull()
        {
            await Page.EvaluateAsync<object>("() => { window.JSON.stringify = null; window.JSON = null; }");
            var result = await Page.EvaluateAsync<JsonElement>("() => ({ abc: 123})");
            Assert.Equal(123, result.GetProperty("abc").GetInt32());
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should await promise from popup")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldAwaitPromiseFromPopup()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            int result = await Page.EvaluateAsync<int>(@"() => {
                const win = window.open('about:blank');
                return new win.Promise(f => f(42));
            }");
            Assert.Equal(42, result);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work with new Function() and CSP")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNewFunctionAndCSP()
        {
            Server.SetCSP("/empty.html", "script-src" + TestConstants.ServerUrl);
            await Page.GoToAsync(TestConstants.EmptyPage);

            Assert.True(await Page.EvaluateAsync<bool>("() => new Function('return true')()"));
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work with non-strict expressions")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNonStrictExpressions()
        {
            Assert.Equal(3.14m, await Page.EvaluateAsync<decimal>(@"() => {
              y = 3.14;
              return y;
            }"));
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should respect use strict expression")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectUseStrictExpression()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync<object>(@"() => {
                ""use strict"";
                variableY = 3.14;
               return variableY;
            }"));
            Assert.Contains("variableY", exception.Message);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should not leak utility script")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotLeakUtilityScript()
        {
            Assert.True(await Page.EvaluateAsync<bool>(@"() => this === window"));
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should not leak handles")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotLeakHandles()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync<object>(@"() => handles.length"));
            Assert.Contains("handles", exception.Message);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should work with CSP")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCSP()
        {
            Server.SetCSP("/empty.html", "script-src 'self'");
            await Page.GoToAsync(TestConstants.EmptyPage);

            Assert.Equal(4, await Page.EvaluateAsync<int>("() => 2 + 2"));
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should evaluate exception")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldEvaluateException()
        {
            string exception = await Page.EvaluateAsync<string>(@"() => {
                return (function functionOnStack() {
                    return new Error('error message');
                })();
            }");
            Assert.Contains("Error: error message", exception);
            Assert.Contains("functionOnStack", exception);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should evaluate exception")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldEvaluateException2()
        {
            string exception = await Page.EvaluateAsync<string>(@"() => new Error('error message')");
            Assert.Contains("Error: error message", exception);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should evaluate date")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldEvaluateDate()
        {
            dynamic result = await Page.EvaluateAsync<ExpandoObject>(@"() => ({ date: new Date('2020-05-27T01:31:38.506Z') })");
            Assert.Equal(new DateTime(2020, 05, 27, 1, 31, 38, 506), result.date);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip date")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRoundtripDate()
        {
            var date = new DateTime(2020, 05, 27, 1, 31, 38, 506, DateTimeKind.Utc);
            var result = await Page.EvaluateAsync<DateTime>(@"date => date", date);
            Assert.Equal(date, result);
        }

        [Fact(Skip = "The driver doesn't support this yet")]
        public async Task ShouldTreatEcma2020AsFunctions()
             => Assert.Equal("dario", await Page.EvaluateAsync<string>(
                 @"() => {
                    const person = { name: 'dario' };
                    return person?.name;
                }"));

        [PlaywrightTest("page-evaluate.spec.ts", "should roundtrip regex")]
        [Fact(Skip = "Regex is not native as in javascript")]
        public void ShouldRoundtripRegex()
        {
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should jsonValue() date")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldJsonValueDate()
        {
            var resultHandle = await Page.EvaluateHandleAsync(@"() => ({ date: new Date('2020-05-27T01:31:38.506Z') })");
            dynamic result = await resultHandle.JsonValueAsync<ExpandoObject>();
            Assert.Equal(new DateTime(2020, 05, 27, 1, 31, 38, 506), result.date);
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should not use toJSON when evaluating")]
        [Fact(Skip = "Skip for now")]
        public void ShouldNotUseToJSONWhenEvaluating()
        {
        }

        [PlaywrightTest("page-evaluate.spec.ts", "should not use toJSON in jsonValue")]
        [Fact(Skip = "Skip for now")]
        public void ShouldNotUseToJSONInJsonValue()
        {
        }
    }
}
