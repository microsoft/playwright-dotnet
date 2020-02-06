using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit.Abstractions;
using Xunit;
using System.Text.Json;

namespace PlaywrightSharp.Tests.Evaluation
{
    public class PageEvaluateTests : PlaywrightSharpPageBaseTest
    {
        internal PageEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {

            var result = await Page.EvaluateAsync<int>("() => 7 * 3");
            Assert.Equal(21, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer NaN</playwright-it>
        [Fact]
        public async Task ShouldTransferNaN()
        {
            var result = await Page.EvaluateAsync<double>("a => a", double.NaN);
            Assert.Equal(double.NaN, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer -0</playwright-it>
        [Fact]
        public async Task ShouldTransferNegative0()
        {

            var result = await Page.EvaluateAsync<int>("a => a", -0);
            Assert.Equal(-0, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer Infinity</playwright-it>
        [Fact]
        public async Task ShouldTransferInfinity()
        {
            var result = await Page.EvaluateAsync<double>("a => a", double.PositiveInfinity);
            Assert.Equal(double.PositiveInfinity, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer -Infinity</playwright-it>
        [Fact]
        public async Task ShouldTransferNegativeInfinity()
        {
            var result = await Page.EvaluateAsync<double>("a => a", double.NegativeInfinity);
            Assert.Equal(double.NegativeInfinity, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer arrays</playwright-it>
        [Fact]
        public async Task ShouldTransferArrays()
        {
            var result = await Page.EvaluateAsync<int[]>("a => a", new[] { 1, 2, 3 });
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should transfer arrays as arrays, not objects</playwright-it>
        [Fact]
        public async Task ShouldTransferArraysAsArraysNotObjects()
        {

            var result = await Page.EvaluateAsync<bool>("a => Array.isArray(a)", new[] { 1, 2, 3 });
            Assert.True(result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should modify global environment</playwright-it>
        [Fact]
        public async Task ShouldModifyGlobalEnvironment()
        {
            await Page.EvaluateAsync("() => window.globalVar = 123");
            Assert.Equal(123, await Page.EvaluateAsync<int>("globalVar"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should evaluate in the page context</playwright-it>
        [Fact]
        public async Task ShouldEvaluateInThePageContext()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/global-var.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("globalVar"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return undefined for objects with symbols</playwright-it>
        [Fact]
        public async Task ShouldReturnUndefinedForObjectsWithSymbols()
        {
            Assert.Null(await Page.EvaluateAsync<object>("() => [Symbol('foo4')]"));
            Assert.Equal(JsonDocument.Parse("{}"), await Page.EvaluateAsync<JsonDocument>(@"() => {
                var a = { };
                a[Symbol('foo4')] = 42;
                return a;
            }"));
            Assert.Null(await Page.EvaluateAsync<object>(@"() => {
                return { foo: [{ a: Symbol('foo4') }] };
            }"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work with function shorthands</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public Task ShouldWorkWithFunctionShorthands()
        {
            return Task.CompletedTask;
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work with unicode chars</playwright-it>
        [Fact]
        public async Task ShouldWorkWithUnicodeChars()
        {
            var result = await Page.EvaluateAsync<int>("a => a['Σ╕¡µצחσ¡קτ¼ª'], { 'Σ╕¡µצחσ¡קτ¼ª': 42}");
            Assert.Equal(42, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should throw when evaluation triggers reload</playwright-it>
        [Fact]
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
        [Fact]
        public async Task ShouldAwaitPromise()
        {
            var result = await Page.EvaluateAsync<int>("() => Promise.resolve(8 * 7)");
            Assert.Equal(56, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should work right after framenavigated</playwright-it>
        [Fact]
        public async Task ShouldWorkRightAfterFramenavigated()
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
        [Fact]
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
        [Fact]
        public async Task ShouldWorkFromInsideAnExposedFunction()
        {
            // Setup inpage callback, which calls Page.evaluate
            await Page.ExposeFunctionAsync("callController", @"async function(a, b) {
                return await Page.EvaluateAsync<object>((a, b) => a * b, a, b);
            }");
            var result = await Page.EvaluateAsync<int>(@"async function() {
                return await callController(9, 3);
            }");
            Assert.Equal(27, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should reject promise with exception</playwright-it>
        [Fact]
        public async Task ShouldRejectPromiseWithException()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => not_existing_object.property"));
            Assert.Contains("not_existing_object", exception.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should support thrown strings as error messages</playwright-it>
        [Fact]
        public async Task ShouldSupportThrownStringsAsErrorMessages()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => { throw 'qwerty'; }"));
            Assert.Contains("qwerty", exception.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should support thrown numbers as error messages</playwright-it>
        [Fact]
        public async Task ShouldSupportThrownNumbersAsErrorMessages()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => { throw 100500; }"));
            Assert.Contains("100500", exception.Message);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return complex objects</playwright-it>
        [Fact]
        public async Task ShouldReturnComplexObjects()
        {
            var obj = new { foo = "bar!" };
            var result = await Page.EvaluateAsync<object>("a => a", obj);
            Assert.NotSame(obj, result);
            Assert.Equal(obj, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return NaN</playwright-it>
        [Fact]
        public async Task ShouldReturnNaN()
        {
            var result = await Page.EvaluateAsync<double>("() => NaN");
            Assert.Equal(double.NaN, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return -0</playwright-it>
        [Fact]
        public async Task ShouldReturnNegative0()
        {
            var result = await Page.EvaluateAsync<int>("() => -0");
            Assert.Equal(-0, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return Infinity</playwright-it>
        [Fact]
        public async Task ShouldReturnInfinity()
        {
            var result = await Page.EvaluateAsync<double>("() => Infinity");
            Assert.Equal(double.PositiveInfinity, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return -Infinity</playwright-it>
        [Fact]
        public async Task ShouldReturnNegativeInfinity()
        {
            var result = await Page.EvaluateAsync<double>("() => Infinity");
            Assert.Equal(double.NegativeInfinity, result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should accept "undefined" as one of multiple parameters</playwright-it>
        [Fact]
        public async Task ShouldAcceptUndefinedAsOneOfMultipleParameters()
        {
            var result = await Page.EvaluateAsync<bool>("(a, b) => Object.is (a, undefined) && Object.is (b, 'foo')", null, "foo");
            Assert.True(result);
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should properly serialize undefined arguments</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public Task ShouldProperlySerializeUndefinedArguments()
        {
            return Task.CompletedTask;
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should properly serialize undefined fields</playwright-it>
        [Fact]
        public async Task ShouldProperlySerializeUndefinedFields()
        {
            Assert.Equal(JsonDocument.Parse("{}"), await Page.EvaluateAsync<JsonDocument>("() => ({ a: undefined})"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should properly serialize null arguments</playwright-it>
        [Fact]
        public async Task ShouldProperlySerializeNullArguments()
        {
            Assert.Null(await Page.EvaluateAsync<JsonDocument>("x => x", null));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should properly serialize null fields</playwright-it>
        [Fact]
        public async Task ShouldProperlySerializeNullFields()
        {
            Assert.Equal(new { a = (object)null }, await Page.EvaluateAsync<object>("() => ({ a: null}))"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should return undefined for non-serializable objects</playwright-it>
        [Fact]
        public async Task ShouldReturnUndefinedForNonSerializableObjects()
        {
            Assert.Null(await Page.EvaluateAsync<object>("() => window"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should fail for circular object</playwright-it>
        [Fact]
        public async Task ShouldFailForCircularObject()
        {
            var result = await Page.EvaluateAsync<object>(@"() => {
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
        [Fact]
        public async Task ShouldBeAbleToThrowATrickyError()
        {

            var windowHandle = (IElementHandle)await Page.EvaluateHandleAsync("() => window");
            var errorText = await windowHandle.jsonValue().catch (e => e.message);
            var error = await Page.EvaluateAsync<object>(errorText =>
            {
                throw new Error(errorText);
            }, errorText).catch (e => e);
            expect(error.message).toContain(errorText);
            }
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluate</playwright-describe>
        ///<playwright-it>should accept a string</playwright-it>
        [Fact]
        public async Task ShouldAcceptAString()
        {

            var result = await Page.EvaluateAsync<object>('1 + 2');
            expect(result).toBe(3);
        }
    }

    ///<playwright-file>evaluation.spec.js</playwright-file>
    ///<playwright-describe>Page.evaluate</playwright-describe>
    ///<playwright-it>should accept a string with semi colons</playwright-it>
    [Fact]
    public async Task ShouldAcceptAStringWithSemiColons()
    {

        var result = await Page.EvaluateAsync<object>('1 + 5;');
        expect(result).toBe(6);
    }
}

///<playwright-file>evaluation.spec.js</playwright-file>
///<playwright-describe>Page.evaluate</playwright-describe>
///<playwright-it>should accept a string with comments</playwright-it>
[Fact]
public async Task ShouldAcceptAStringWithComments()
{

    var result = await Page.EvaluateAsync<object>('2 + 5;\n// do some math!');
    expect(result).toBe(7);
}
}

///<playwright-file>evaluation.spec.js</playwright-file>
///<playwright-describe>Page.evaluate</playwright-describe>
///<playwright-it>should accept element handle as an argument</playwright-it>
[Fact]
public async Task ShouldAcceptElementHandleAsAnArgument()
{

    await page.setContent('<section>42</section>');
    var element = await page.$('section');
    var text = await Page.EvaluateAsync<object>(e => e.textContent, element);
    expect(text).toBe('42');
}
}

///<playwright-file>evaluation.spec.js</playwright-file>
///<playwright-describe>Page.evaluate</playwright-describe>
///<playwright-it>should throw if underlying element was disposed</playwright-it>
[Fact]
public async Task ShouldThrowIfUnderlyingElementWasDisposed()
{

    await page.setContent('<section>39</section>');
    var element = await page.$('section');
    expect(element).toBeTruthy();
    await element.dispose();
    let error = null;
    await Page.EvaluateAsync<object>(e => e.textContent, element).catch (e => error = e);
    expect(error.message).toContain('JSHandle is disposed');
    }
}

///<playwright-file>evaluation.spec.js</playwright-file>
///<playwright-describe>Page.evaluate</playwright-describe>
///<playwright-it>should simulate a user gesture</playwright-it>
[Fact]
public async Task ShouldSimulateAUserGesture()
{

    var result = await Page.EvaluateAsync<object>(() =>
    {
        document.body.appendChild(document.createTextNode('test'));
        document.execCommand('selectAll');
        return document.execCommand('copy');
    });
    expect(result).toBe(true);
}
}

///<playwright-file>evaluation.spec.js</playwright-file>
///<playwright-describe>Page.evaluate</playwright-describe>
///<playwright-it>should throw a nice error after a navigation</playwright-it>
[Fact]
public async Task ShouldThrowANiceErrorAfterANavigation()
{

    var errorPromise = page.evaluate(() => new Promise(f => window.__resolve = f)).catch (e => e);
    await Promise.all([
      page.waitForNavigation(),
      page.evaluate(() =>
      {
          window.location.reload();
          setTimeout(() => window.__resolve(42), 1000);
      })
    ]);
    var error = await errorPromise;
    expect(error.message).toContain('navigation');
    }
}

///<playwright-file>evaluation.spec.js</playwright-file>
///<playwright-describe>Page.evaluate</playwright-describe>
///<playwright-it>should not throw an error when evaluation does a navigation</playwright-it>
[Fact]
public async Task ShouldNotThrowAnErrorWhenEvaluationDoesANavigation()
{

    await Page.GoToAsync(server.PREFIX + '/one-style.html');
    var result = await Page.EvaluateAsync<object>(() =>
    {
        window.location = '/empty.html';
        return [42];
    });
    expect(result).toEqual([42]);
}
}

///<playwright-file>evaluation.spec.js</playwright-file>
///<playwright-describe>Page.evaluate</playwright-describe>
///<playwright-it>should transfer 100Mb of data from page to node.js</playwright-it>
[Fact(Skip = "Not implemented")]
public async Task ShouldTransfer100MbOfDataFromPageToNode.js()
{

    var a = await Page.EvaluateAsync<object>(() => Array(100 * 1024 * 1024 + 1).join('a'));
    expect(a.length).toBe(100 * 1024 * 1024);
}
}

///<playwright-file>evaluation.spec.js</playwright-file>
///<playwright-describe>Page.evaluate</playwright-describe>
///<playwright-it>should throw error with detailed information on exception inside promise </playwright-it>
[Fact]
public async Task ShouldThrowErrorWithDetailedInformationOnExceptionInsidePromise()
{

    let error = null;
    await Page.EvaluateAsync<object>(() => new Promise(() =>
    {
        throw new Error('Error in promise');
    })).catch (e => error = e);
    expect(error.message).toContain('Error in promise');
    }
}

///<playwright-file>evaluation.spec.js</playwright-file>
///<playwright-describe>Page.evaluate</playwright-describe>
///<playwright-it>should work even when JSON is set to null</playwright-it>
[Fact]
public async Task ShouldWorkEvenWhenJSONIsSetToNull()
{
    async({ page }) => {
        await Page.EvaluateAsync<object>(() => { window.JSON.stringify = null; window.JSON = null; });
        var result = await Page.EvaluateAsync<object>(() => ({ abc: 123}));
        expect(result).toEqual({ abc: 123});
    }
}
    }
}
