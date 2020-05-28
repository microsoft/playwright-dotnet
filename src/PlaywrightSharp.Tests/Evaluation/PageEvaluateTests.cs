using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Evaluation
{
    ///<playwright-file>evaluation.spec.js</playwright-file>
    ///<playwright-describe>Page.evaluate</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEvaluateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            int result = await Page.EvaluateAsync<int>("() => 7 * 3");
            Assert.Equal(21, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer NaN</playwright-it>
        [Retry]
        public async Task ShouldTransferNaN()
        {
            double result = await Page.EvaluateAsync<double>("a => a", double.NaN);
            Assert.Equal(double.NaN, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer -0</playwright-it>
        [Retry]
        public async Task ShouldTransferNegative0()
        {
            int result = await Page.EvaluateAsync<int>("a => a", -0);
            Assert.Equal(-0, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer Infinity</playwright-it>
        [Retry]
        public async Task ShouldTransferInfinity()
        {
            double result = await Page.EvaluateAsync<double>("a => a", double.PositiveInfinity);
            Assert.Equal(double.PositiveInfinity, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer -Infinity</playwright-it>
        [Retry]
        public async Task ShouldTransferNegativeInfinity()
        {
            double result = await Page.EvaluateAsync<double>("a => a", double.NegativeInfinity);
            Assert.Equal(double.NegativeInfinity, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer arrays</playwright-it>
        [Retry]
        public async Task ShouldTransferArrays()
        {
            int[] result = await Page.EvaluateAsync<int[]>("a => a", new[] { 1, 2, 3 });
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer arrays as arrays, not objects</playwright-it>
        [Retry]
        public async Task ShouldTransferArraysAsArraysNotObjects()
        {
            bool result = await Page.EvaluateAsync<bool>("a => Array.isArray(a)", new[] { 1, 2, 3 });
            Assert.True(result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should modify global environment</playwright-it>
        [Retry]
        public async Task ShouldModifyGlobalEnvironment()
        {
            await Page.EvaluateAsync("() => window.globalVar = 123");
            Assert.Equal(123, await Page.EvaluateAsync<int>("globalVar"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should evaluate in the page context</playwright-it>
        [Retry]
        public async Task ShouldEvaluateInThePageContext()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/global-var.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("globalVar"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return undefined for objects with symbols</playwright-it>
        [Retry]
        public async Task ShouldReturnUndefinedForObjectsWithSymbols()
        {
            Assert.Null(await Page.EvaluateAsync<object>("() => [Symbol('foo4')]"));
            Assert.Equal("{}", (await Page.EvaluateAsync<JsonElement>(@"() => {
                var a = { };
                a[Symbol('foo4')] = 42;
                return a;
            }")).ToJson());
            Assert.Null(await Page.EvaluateAsync<object>(@"() => {
                return { foo: [{ a: Symbol('foo4') }] };
            }"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work with function shorthands</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldWorkWithFunctionShorthands()
        {
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work with unicode chars</playwright-it>
        [Retry]
        public async Task ShouldWorkWithUnicodeChars()
        {
            int result = await Page.EvaluateAsync<int>("a => a['中文字符']", new Dictionary<string, int> { ["中文字符"] = 42 });
            Assert.Equal(42, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should throw when evaluation triggers reload</playwright-it>
        [Retry]
        public async Task ShouldThrowWhenEvaluationTriggersReload()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync<object>(@"() => {
                location.reload();
                return new Promise(() => { });
            }"));
            Assert.Contains("navigation", exception.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should await promise</playwright-it>
        [Retry]
        public async Task ShouldAwaitPromise()
        {
            int result = await Page.EvaluateAsync<int>("() => Promise.resolve(8 * 7)");
            Assert.Equal(56, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work right after framenavigated</playwright-it>
        [Retry]
        public async Task ShouldWorkRightAfterFrameNavigated()
        {
            Task<int> frameEvaluation = null;
            Page.FrameNavigated += (sender, e) =>
            {
                frameEvaluation = e.Frame.EvaluateAsync<int>("() => 6 * 7");
            };
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(42, await frameEvaluation);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work right after a cross-origin navigation</playwright-it>
        [Retry]
        public async Task ShouldWorkRightAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Task<int> frameEvaluation = null;
            Page.FrameNavigated += (sender, e) =>
            {
                frameEvaluation = e.Frame.EvaluateAsync<int>("() => 6 * 7");
            };
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.Equal(42, await frameEvaluation);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work from-inside an exposed function</playwright-it>
        [Retry]
        public async Task ShouldWorkFromInsideAnExposedFunction()
        {
            // Setup inpage callback, which calls Page.evaluate
            await Page.ExposeFunctionAsync("callController", async (int a, int b) => await Page.EvaluateAsync<int>("(a, b) => a * b", a, b));
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await callController(9, 3);
            }");
            Assert.Equal(27, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should reject promise with exception</playwright-it>
        [Retry]
        public async Task ShouldRejectPromiseWithException()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => not_existing_object.property"));
            Assert.Contains("not_existing_object", exception.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should support thrown strings as error messages</playwright-it>
        [Retry]
        public async Task ShouldSupportThrownStringsAsErrorMessages()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => { throw 'qwerty'; }"));
            Assert.Contains("qwerty", exception.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should support thrown numbers as error messages</playwright-it>
        [Retry]
        public async Task ShouldSupportThrownNumbersAsErrorMessages()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => { throw 100500; }"));
            Assert.Contains("100500", exception.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return complex objects</playwright-it>
        [Retry]
        public async Task ShouldReturnComplexObjects()
        {
            var obj = new { foo = "bar!" };
            var result = await Page.EvaluateAsync<JsonElement>("a => a", obj);
            Assert.Equal("bar!", result.GetProperty("foo").GetString());
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return NaN</playwright-it>
        [Retry]
        public async Task ShouldReturnNaN()
        {
            double result = await Page.EvaluateAsync<double>("() => NaN");
            Assert.Equal(double.NaN, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return -0</playwright-it>
        [Retry]
        public async Task ShouldReturnNegative0()
        {
            int result = await Page.EvaluateAsync<int>("() => -0");
            Assert.Equal(-0, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return Infinity</playwright-it>
        [Retry]
        public async Task ShouldReturnInfinity()
        {
            double result = await Page.EvaluateAsync<double>("() => Infinity");
            Assert.Equal(double.PositiveInfinity, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return -Infinity</playwright-it>
        [Retry]
        public async Task ShouldReturnNegativeInfinity()
        {
            double result = await Page.EvaluateAsync<double>("() => -Infinity");
            Assert.Equal(double.NegativeInfinity, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should accept "undefined" as one of multiple parameters</playwright-it>
        [Retry]
        public async Task ShouldAcceptUndefinedAsOneOfMultipleParameters()
        {
            bool result = await Page.EvaluateAsync<bool>("(a, b) => Object.is (a, undefined) && Object.is (b, 'foo')", null, "foo");
            Assert.True(result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should properly serialize undefined arguments</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldProperlySerializeUndefinedArguments()
        {
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should properly serialize undefined fields</playwright-it>
        [Retry]
        public async Task ShouldProperlySerializeUndefinedFields()
            => Assert.Empty(await Page.EvaluateAsync<Dictionary<string, object>>("() => ({a: undefined})"));

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should properly serialize null arguments</playwright-it>
        [Retry]
        public async Task ShouldProperlySerializeNullArguments()
            => Assert.Null(await Page.EvaluateAsync<JsonDocument>("x => x", new object[] { null }));

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should properly serialize null fields</playwright-it>
        [Retry]
        public async Task ShouldProperlySerializeNullFields()
            => Assert.Equal(JsonValueKind.Null, (await Page.EvaluateAsync<JsonElement>("() => ({ a: null})")).GetProperty("a").ValueKind);

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return undefined for non-serializable objects</playwright-it>
        [Retry]
        public async Task ShouldReturnUndefinedForNonSerializableObjects()
            => Assert.Null(await Page.EvaluateAsync<object>("() => window"));

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should fail for circular object</playwright-it>
        [Retry]
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

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should be able to throw a tricky error</playwright-it>
        [Retry]
        public async Task ShouldBeAbleToThrowATrickyError()
        {
            var windowHandle = await Page.EvaluateHandleAsync("() => window");
            var exceptionText = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => windowHandle.GetJsonValueAsync<object>());
            var error = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.EvaluateAsync<JsonElement>(@"errorText => {
                throw new Error(errorText);
            }", exceptionText.Message));
            Assert.Contains(exceptionText.Message, error.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should accept a string</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldAcceptAString()
        {
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should accept a string with semi colons</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldAcceptAStringWithSemiColons()
        {
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should accept a string with comments</playwright-it>
        [Retry]
        public async Task ShouldAcceptAStringWithComments()
        {
            int result = await Page.EvaluateAsync<int>("2 + 5;\n// do some math!");
            Assert.Equal(7, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should accept element handle as an argument</playwright-it>
        [Retry]
        public async Task ShouldAcceptElementHandleAsAnArgument()
        {
            await Page.SetContentAsync("<section>42</section>");
            var element = await Page.QuerySelectorAsync("section");
            string text = await Page.EvaluateAsync<string>("e => e.textContent", element);
            Assert.Equal("42", text);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should throw if underlying element was disposed</playwright-it>
        [Retry]
        public async Task ShouldThrowIfUnderlyingElementWasDisposed()
        {
            await Page.SetContentAsync("<section>39</section>");
            var element = await Page.QuerySelectorAsync("section");
            Assert.NotNull(element);
            await element.DisposeAsync();

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("e => e.textContent", element));
            Assert.Contains("JSHandle is disposed", exception.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should simulate a user gesture</playwright-it>
        [Retry]
        public async Task ShouldSimulateAUserGesture()
        {
            bool result = await Page.EvaluateAsync<bool>(@"() => {
                document.body.appendChild(document.createTextNode('test'));
                document.execCommand('selectAll');
                return document.execCommand('copy');
            }");
            Assert.True(result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should throw a nice error after a navigation</playwright-it>
        [Retry]
        public async Task ShouldThrowANiceErrorAfterANavigation()
        {
            var exceptionTask = Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => new Promise(f => window.__resolve = f)"));
            await Task.WhenAll(
                Page.WaitForNavigationAsync(),
                Page.EvaluateAsync(@"() => {
                    window.location.reload();
                    setTimeout(() => window.__resolve(42), 1000);
                }")
            );
            var exception = await exceptionTask;
            Assert.Contains("navigation", exception.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should not throw an error when evaluation does a navigation</playwright-it>
        [Retry]
        public async Task ShouldNotThrowAnErrorWhenEvaluationDoesANavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            int[] result = await Page.EvaluateAsync<int[]>(@"() => {
                window.location = '/empty.html';
                return [42];
            }");
            Assert.Equal(new[] { 42 }, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer 100Mb of data from page to node.js</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldTransfer100MbOfDataFromPageToNodeJs()
        {
            string a = await Page.EvaluateAsync<string>("() => Array(100 * 1024 * 1024 + 1).join('a')");
            Assert.Equal(100 * 1024 * 1024, a.Length);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should throw error with detailed information on exception inside promise </playwright-it>
        [Retry]
        public async Task ShouldThrowErrorWithDetailedInformationOnExceptionInsidePromise()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync<object>(@"() => new Promise(() => {
                throw new Error('Error in promise');
            })"));
            Assert.Contains("Error in promise", exception.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work even when JSON is set to null</playwright-it>
        [Retry]
        public async Task ShouldWorkEvenWhenJSONIsSetToNull()
        {
            await Page.EvaluateAsync<object>("() => { window.JSON.stringify = null; window.JSON = null; }");
            var result = await Page.EvaluateAsync<JsonElement>("() => ({ abc: 123})");
            Assert.Equal(123, result.GetProperty("abc").GetInt32());
        }
    }
}
