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

using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-evaluate.spec.ts.</summary>
public class PageEvaluateTests
{
    [UpstreamTest("EV-001 should work @smoke")]
    public async Task ShouldWork(IPage page)
    {
        var result = await page.EvaluateAsync<int>("() => 7 * 3");
        Assert.That(result, Is.EqualTo(21));
    }

    [UpstreamTest("EV-002 should transfer NaN")]
    public async Task ShouldTransferNaN(IPage page)
    {
        var result = await page.EvaluateAsync<double>("a => a", double.NaN);
        Assert.That(ObjectIs(result, double.NaN), Is.EqualTo(true));
    }

    [UpstreamTest("EV-003 should transfer -0")]
    public async Task ShouldTransfer0(IPage page)
    {
        var result = await page.EvaluateAsync<double>("a => a", -0d);
        Assert.That(ObjectIs(result, -0d), Is.EqualTo(true));
    }

    [UpstreamTest("EV-004 should transfer Infinity")]
    public async Task ShouldTransferInfinity_EV004(IPage page)
    {
        var result = await page.EvaluateAsync<double>("a => a", double.PositiveInfinity);
        Assert.That(ObjectIs(result, double.PositiveInfinity), Is.EqualTo(true));
    }

    [UpstreamTest("EV-005 should transfer -Infinity")]
    public async Task ShouldTransferInfinity_EV005(IPage page)
    {
        var result = await page.EvaluateAsync<double>("a => a", double.NegativeInfinity);
        Assert.That(ObjectIs(result, double.NegativeInfinity), Is.EqualTo(true));
    }

    [UpstreamTest("EV-006 should roundtrip unserializable values")]
    public async Task ShouldRoundtripUnserializableValues(IPage page)
    {
        var value = new
        {
            infinity = double.PositiveInfinity,
            nInfinity = double.NegativeInfinity,
            nZero = -0d,
            nan = double.NaN,
        };
        var result = await page.EvaluateAsync<object>("value => value", value);
        ExpectToEqual(result, value);
    }

    [UpstreamTest("EV-007 should roundtrip promise to value")]
    public async Task ShouldRoundtripPromiseToValue(IPage page)
    {
        {
            var result = await page.EvaluateAsync<object>("value => Promise.resolve(value)", null);
            Assert.That(result == null, Is.True);
        }
        {
            var result = await page.EvaluateAsync<double>("value => Promise.resolve(value)", double.PositiveInfinity);
            Assert.That(result == double.PositiveInfinity, Is.True);
        }
        {
            var result = await page.EvaluateAsync<double>("value => Promise.resolve(value)", -0d);
            Assert.That(result == -0d, Is.True);
        }
        {
            var result = await page.EvaluateAsync<object>("value => Promise.resolve(value)", null);
            Assert.That(result == null, Is.True);
        }
    }

    [UpstreamTest("EV-008 should roundtrip promise to unserializable values")]
    public async Task ShouldRoundtripPromiseToUnserializableValues(IPage page)
    {
        var value = new
        {
            infinity = double.PositiveInfinity,
            nInfinity = double.NegativeInfinity,
            nZero = -0d,
            nan = double.NaN,
        };
        var result = await page.EvaluateAsync<object>("value => Promise.resolve(value)", value);
        ExpectToEqual(result, value);
    }

    [UpstreamTest("EV-009 should transfer arrays")]
    public async Task ShouldTransferArrays(IPage page)
    {
        var result = await page.EvaluateAsync<int[]>("a => a", new[] { 1, 2, 3 });
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [UpstreamTest("EV-010 should transfer arrays as arrays, not objects")]
    public async Task ShouldTransferArraysAsArraysNotObjects(IPage page)
    {
        var result = await page.EvaluateAsync<bool>("a => Array.isArray(a)", new[] { 1, 2, 3 });
        Assert.That(result, Is.EqualTo(true));
    }

    [UpstreamTest("EV-011 should transfer typed arrays")]
    [Fixme("dotnet: a .NET array is not sent as a typed array: the elements of sbyte[], byte[], ushort[], uint[], float[] and ulong[] arrive as empty objects ([{}, {}, {}]), and short[], int[], long[] and double[] arrive as a plain Array")]
    public async Task ShouldTransferTypedArrays(IPage page)
    {
        var testCases = new object[]
        {
            new sbyte[] { 1, 2, 3 },
            new byte[] { 1, 2, 3 },
            new byte[] { 1, 2, 3 },
            new short[] { 1, 2, 3 },
            new ushort[] { 1, 2, 3 },
            new int[] { 1, 2, 3 },
            new uint[] { 1, 2, 3 },
            new float[] { 1.1f, 2.2f, 3.3f },
            new double[] { 1.1, 2.2, 3.3 },
            new long[] { 1L, 2L, 3L },
            new ulong[] { 1UL, 2UL, 3UL },
        };

        foreach (var typedArray in testCases)
        {
            var result = await page.EvaluateAsync<object>("a => a", typedArray);
            ExpectToEqual(result, typedArray);
        }
    }

    [UpstreamTest("EV-012 should transfer bigint")]
    public async Task ShouldTransferBigint(IPage page)
    {
        Assert.That(await page.EvaluateAsync<BigInteger>("() => 42n"), Is.EqualTo(new BigInteger(42)));
        Assert.That(await page.EvaluateAsync<BigInteger>("a => a", new BigInteger(17)), Is.EqualTo(new BigInteger(17)));
    }

    [UpstreamTest("EV-013 should transfer maps as empty objects")]
    [Fixme("dotnet: there is no Map: a Dictionary<int, int> is sent as an Array of { Key, Value } objects, 'Array [{\"Key\":1,\"Value\":2}]', not as an empty object")]
    public async Task ShouldTransferMapsAsEmptyObjects(IPage page)
    {
        var result = await page.EvaluateAsync<string>("a => a.x.constructor.name + ' ' + JSON.stringify(a.x)", new { x = new Dictionary<int, int> { [1] = 2 } });
        Assert.That(result, Is.EqualTo("Object {}"));
        ExpectToEqual(await page.EvaluateAsync<object>("() => new Map([[1, 2]])"), new { });
    }

    [UpstreamTest("EV-014 should transfer sets as empty objects")]
    [Fixme("dotnet: there is no Set: a HashSet<int> is sent as an Array, 'Array [1,2]', not as an empty object")]
    public async Task ShouldTransferSetsAsEmptyObjects(IPage page)
    {
        var result = await page.EvaluateAsync<string>("a => a.x.constructor.name + ' ' + JSON.stringify(a.x)", new { x = new HashSet<int> { 1, 2 } });
        Assert.That(result, Is.EqualTo("Object {}"));
        ExpectToEqual(await page.EvaluateAsync<object>("() => new Set([1, 2])"), new { });
    }

    [UpstreamTest("EV-015 should modify global environment")]
    public async Task ShouldModifyGlobalEnvironment(IPage page)
    {
        await page.EvaluateAsync("() => window['globalVar'] = 123");
        Assert.That(await page.EvaluateAsync<int>("globalVar"), Is.EqualTo(123));
    }

    [UpstreamTest("EV-016 should evaluate in the page context")]
    public async Task ShouldEvaluateInThePageContext(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/global-var.html");
        Assert.That(await page.EvaluateAsync<int>("globalVar"), Is.EqualTo(123));
    }

    [UpstreamTest("EV-017 should return undefined for objects with symbols")]
    public async Task ShouldReturnUndefinedForObjectsWithSymbols(IPage page)
    {
        ExpectToEqual(await page.EvaluateAsync<object>("() => [Symbol('foo4')]"), new object[] { null });
        ExpectToEqual(
            await page.EvaluateAsync<object>(@"() => {
    const a = {};
    a[Symbol('foo4')] = 42;
    return a;
  }"),
            new { });
        ExpectToEqual(
            await page.EvaluateAsync<object>(@"() => {
    return { foo: [{ a: Symbol('foo4') }] };
  }"),
            new { foo = new[] { new Dictionary<string, object> { ["a"] = null } } });
    }

    [UpstreamTest("EV-018 should work with function shorthands")]
    [Fixme("dotnet: evaluate does not send isFunction, so the driver evaluates the text as an expression, and a method shorthand is a SyntaxError there")]
    public async Task ShouldWorkWithFunctionShorthands(IPage page)
    {
        var a = new
        {
            sum = "sum([a, b]) { return a + b; }",
            mult = "async mult([a, b]) { return a * b; }",
        };
        Assert.That(await page.EvaluateAsync<int>(a.sum, new[] { 1, 2 }), Is.EqualTo(3));
        Assert.That(await page.EvaluateAsync<int>(a.mult, new[] { 2, 4 }), Is.EqualTo(8));
    }

    [UpstreamTest("EV-019 should work with unicode chars")]
    public async Task ShouldWorkWithUnicodeChars(IPage page)
    {
        var result = await page.EvaluateAsync<int>("a => a['中文字符']", new Dictionary<string, object> { ["中文字符"] = 42 });
        Assert.That(result, Is.EqualTo(42));
    }

    [UpstreamTest("EV-020 should work with large strings")]
    public async Task ShouldWorkWithLargeStrings(IPage page, bool isAndroid)
    {
        It.Skip(isAndroid, "string is too long :(");

        var expected = string.Concat(Enumerable.Repeat("x", 40000));
        Assert.That(await page.EvaluateAsync<string>("data => data", expected), Is.EqualTo(expected));
    }

    [UpstreamTest("EV-021 should work with large unicode strings")]
    public async Task ShouldWorkWithLargeUnicodeStrings(IPage page, string browserName, string platform, bool isAndroid)
    {
        It.Skip(isAndroid, "string is too long :(");

        var expected = string.Concat(Enumerable.Repeat("🎭", 10000));
        Assert.That(await page.EvaluateAsync<string>("data => data", expected), Is.EqualTo(expected));
    }

    [UpstreamTest("EV-022 should throw when evaluation triggers reload")]
    public async Task ShouldThrowWhenEvaluationTriggersReload(IPage page)
    {
        Exception error = null;
        error = await Catch(() => page.EvaluateAsync(@"() => {
    location.reload();
    return new Promise(() => { });
  }"));
        Assert.That(error.Message, Does.Contain("navigation"));
    }

    [UpstreamTest("EV-023 should await promise")]
    public async Task ShouldAwaitPromise(IPage page)
    {
        var result = await page.EvaluateAsync<int>("() => Promise.resolve(8 * 7)");
        Assert.That(result, Is.EqualTo(56));
    }

    [UpstreamTest("EV-024 should work right after framenavigated")]
    public async Task ShouldWorkRightAfterFramenavigated(IPage page, Server server)
    {
        Task<int> frameEvaluation = null;
        page.FrameNavigated += (_, frame) =>
        {
            frameEvaluation = frame.EvaluateAsync<int>("() => 6 * 7");
        };
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await frameEvaluation, Is.EqualTo(42));
    }

    [UpstreamTest("EV-025 should work right after a cross-origin navigation")]
    public async Task ShouldWorkRightAfterACrossOriginNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Task<int> frameEvaluation = null;
        page.FrameNavigated += (_, frame) =>
        {
            frameEvaluation = frame.EvaluateAsync<int>("() => 6 * 7");
        };
        await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        Assert.That(await frameEvaluation, Is.EqualTo(42));
    }

    [UpstreamTest("EV-026 should work from-inside an exposed function")]
    public async Task ShouldWorkFromInsideAnExposedFunction(IPage page)
    {
        // Setup inpage callback, which calls Page.evaluate
        await page.ExposeFunctionAsync("callController", async (int a, int b) =>
        {
            return await page.EvaluateAsync<int>("({ a, b }) => a * b", new { a, b });
        });
        var result = await page.EvaluateAsync<int>(@"async function() {
    return await window['callController'](9, 3);
  }");
        Assert.That(result, Is.EqualTo(27));
    }

    [UpstreamTest("EV-027 should reject promise with exception")]
    public async Task ShouldRejectPromiseWithException(IPage page)
    {
        Exception error = null;
        // @ts-ignore
        error = await Catch(() => page.EvaluateAsync("() => not_existing_object.property"));
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("not_existing_object"));
    }

    [UpstreamTest("EV-028 should support thrown strings as error messages")]
    public async Task ShouldSupportThrownStringsAsErrorMessages(IPage page)
    {
        Exception error = null;
        error = await Catch(() => page.EvaluateAsync("() => { throw 'qwerty'; }"));
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("qwerty"));
    }

    [UpstreamTest("EV-029 should support thrown numbers as error messages")]
    public async Task ShouldSupportThrownNumbersAsErrorMessages(IPage page)
    {
        Exception error = null;
        error = await Catch(() => page.EvaluateAsync("() => { throw 100500; }"));
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("100500"));
    }

    [UpstreamTest("EV-030 should reject when a falsy value is thrown")]
    public async Task ShouldRejectWhenAFalsyValueIsThrown(IPage page)
    {
        foreach (var value in new[] { "null", "undefined", "0", "''", "false", "NaN", "-0", "0n" })
        {
            var error = await Catch(() => page.EvaluateAsync($"(() => {{ throw {value}; }})()"));
            Assert.That(error, Is.InstanceOf<Exception>(), $"throw {value}");
            var rejection = await Catch(() => page.EvaluateAsync($"Promise.reject({value})"));
            Assert.That(rejection, Is.InstanceOf<Exception>(), $"Promise.reject({value})");
        }
    }

    [UpstreamTest("EV-031 should include a non-error rejection value in the error message")]
    public async Task ShouldIncludeANonErrorRejectionValueInTheErrorMessage(IPage page)
    {
        var error = await Catch(() => page.EvaluateAsync("() => Promise.reject(7)"));
        Assert.That(error.Message, Does.Contain("7"));
        var nullError = await Catch(() => page.EvaluateAsync("() => { throw null; }"));
        Assert.That(nullError.Message, Does.Contain("null"));
    }

    [UpstreamTest("EV-032 should return complex objects")]
    [Fixme("dotnet: an argument that is 200 objects deep does not leave the client: the connection serializes the message with the default MaxDepth of System.Text.Json, 64, and throws a JsonException")]
    public async Task ShouldReturnComplexObjects(IPage page)
    {
        var @object = new { foo = "bar!" };
        var result = await page.EvaluateAsync<object>("a => a", @object);
        Assert.That(result, Is.Not.SameAs(@object));
        ExpectToEqual(result, @object);

        object argument = 0;
        // Chromium rejects protocol arguments at 60 levels, WebKit at 200.
        for (var i = 0; i < 200; i++)
        {
            argument = new { children = new[] { argument } };
        }
        // .NET: JsonSerializer stops at 64 levels unless MaxDepth says otherwise
        Assert.That(await page.EvaluateAsync<int>("value => JSON.stringify(value).length", argument), Is.EqualTo(JsonSerializer.Serialize(argument, new JsonSerializerOptions { MaxDepth = 1000 }).Length));

        // An argument that is too deep rejects and does not hang.
        for (var i = 0; i < 800; i++)
        {
            argument = new { children = new[] { argument } };
        }
        var error = await Catch(() => page.EvaluateAsync<int>("value => JSON.stringify(value).length", argument));
        Assert.That(error.Message, Does.Match("Maximum call stack size exceeded|too much recursion"));
    }

    [UpstreamTest("EV-033 should return NaN")]
    public async Task ShouldReturnNaN(IPage page)
    {
        var result = await page.EvaluateAsync<double>("() => NaN");
        Assert.That(ObjectIs(result, double.NaN), Is.EqualTo(true));
    }

    [UpstreamTest("EV-034 should return -0")]
    public async Task ShouldReturn0(IPage page)
    {
        var result = await page.EvaluateAsync<double>("() => -0");
        Assert.That(ObjectIs(result, -0d), Is.EqualTo(true));
    }

    [UpstreamTest("EV-035 should return Infinity")]
    public async Task ShouldReturnInfinity_EV035(IPage page)
    {
        var result = await page.EvaluateAsync<double>("() => Infinity");
        Assert.That(ObjectIs(result, double.PositiveInfinity), Is.EqualTo(true));
    }

    [UpstreamTest("EV-036 should return -Infinity")]
    public async Task ShouldReturnInfinity_EV036(IPage page)
    {
        var result = await page.EvaluateAsync<double>("() => -Infinity");
        Assert.That(ObjectIs(result, double.NegativeInfinity), Is.EqualTo(true));
    }

    [UpstreamTest("EV-037 should work with overwritten Promise")]
    public async Task ShouldWorkWithOverwrittenPromise(IPage page)
    {
        await page.EvaluateAsync(@"() => {
    const originalPromise = window.Promise;
    class Promise2 {
      _promise;
      static all(arg) {
        return wrap(originalPromise.all(arg));
      }
      static race(arg) {
        return wrap(originalPromise.race(arg));
      }
      static resolve(arg) {
        return wrap(originalPromise.resolve(arg));
      }
      constructor(f) {
        this._promise = new originalPromise(f);
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
    }
    const wrap = p => {
      const result = new Promise2(() => { });
      result._promise = p;
      return result;
    };
    // @ts-ignore;
    window.Promise = Promise2;
    window['__Promise2'] = Promise2;
  }");

        // Sanity check.
        Assert.That(
            await page.EvaluateAsync<bool>(@"() => {
    const p = Promise.all([Promise.race([]), new Promise(() => { }).then(() => { })]);
    return p instanceof window['__Promise2'];
  }"),
            Is.EqualTo(true));

        // Now, the new promise should be awaitable.
        Assert.That(await page.EvaluateAsync<int>("() => Promise.resolve(42)"), Is.EqualTo(42));
        // No page timers below: the frozen-clock project never fires them.
        var handle = await page.EvaluateHandleAsync("() => new Promise(f => queueMicrotask(() => f(42)))");
        Assert.That(await handle.JsonValueAsync<int>(), Is.EqualTo(42));
        var error = await Catch(() => page.WaitForFunctionAsync("() => false", null, new() { Timeout = 500 }));
        Assert.That(error.Message, Does.Contain("Timeout 500ms exceeded"));
        await page.EvaluateAsync("() => window.__done = true");
        await page.WaitForFunctionAsync("() => window.__done");
    }

    [UpstreamTest("EV-042 should return undefined properties")]
    public async Task ShouldReturnUndefinedProperties(IPage page)
    {
        var value = await page.EvaluateAsync<object>("() => ({ a: undefined })");
        // .NET: 'a' in value
        Assert.That(((IDictionary<string, object>)value).ContainsKey("a"), Is.EqualTo(true));
    }

    [UpstreamTest("EV-043 should properly serialize null arguments")]
    public async Task ShouldProperlySerializeNullArguments(IPage page)
    {
        Assert.That(await page.EvaluateAsync<object>("x => x", null), Is.Null);
    }

    [UpstreamTest("EV-044 should properly serialize null fields")]
    public async Task ShouldProperlySerializeNullFields(IPage page)
    {
        ExpectToEqual(await page.EvaluateAsync<object>("() => ({ a: null })"), new Dictionary<string, object> { ["a"] = null });
    }

    [UpstreamTest("EV-045 should properly serialize PerformanceMeasure object")]
    public async Task ShouldProperlySerializePerformanceMeasureObject(IPage page)
    {
        ExpectToEqual(
            await page.EvaluateAsync<object>(@"() => {
    window.builtins.performance.mark('start');
    window.builtins.performance.mark('end');
    window.builtins.performance.measure('my-measure', 'start', 'end');
    return window.builtins.performance.getEntriesByType('measure');
  }"),
            new[]
            {
                ObjectContaining(new
                {
                    duration = AnyNumber,
                    entryType = "measure",
                    name = "my-measure",
                    startTime = AnyNumber,
                }),
            });
    }

    [UpstreamTest("EV-046 should properly serialize window.performance object")]
    public async Task ShouldProperlySerializeWindowPerformanceObject(IPage page)
    {
        It.Skip(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PW_CLOCK")));

        ExpectToEqual(await page.EvaluateAsync<object>("() => performance"), ObjectContaining(new
        {
            navigation = new
            {
                redirectCount = 0,
                type = AnyNumber,
            },
            timeOrigin = AnyNumber,
            timing = new
            {
                connectEnd = AnyNumber,
                connectStart = AnyNumber,
                domComplete = AnyNumber,
                domContentLoadedEventEnd = AnyNumber,
                domContentLoadedEventStart = AnyNumber,
                domInteractive = AnyNumber,
                domLoading = AnyNumber,
                domainLookupEnd = AnyNumber,
                domainLookupStart = AnyNumber,
                fetchStart = AnyNumber,
                loadEventEnd = AnyNumber,
                loadEventStart = AnyNumber,
                navigationStart = AnyNumber,
                redirectEnd = AnyNumber,
                redirectStart = AnyNumber,
                requestStart = AnyNumber,
                responseEnd = AnyNumber,
                responseStart = AnyNumber,
                secureConnectionStart = AnyNumber,
                unloadEventEnd = AnyNumber,
                unloadEventStart = AnyNumber,
            },
        }));
    }

    [UpstreamTest("EV-047 should return undefined for non-serializable objects")]
    public async Task ShouldReturnUndefinedForNonSerializableObjects(IPage page)
    {
        Assert.That(await page.EvaluateAsync<object>("() => function() {}"), Is.Null);
    }

    [UpstreamTest("EV-048 should throw for too deep reference chain")]
    public async Task ShouldThrowForTooDeepReferenceChain(IPage page, string browserName, bool isBidi)
    {
        It.Fixme(browserName == "firefox" && !isBidi, "Firefox Juggler -> Playwright serialiser does not throw for deep references.\nThis causes large objects to get serialised back to the Playwright client.\nThere our validators throw 'Maximum call stack size exceeded'.");
        var error = await ThrowsAsync<PlaywrightException>(() => page.EvaluateAsync(
            @"depth => {
    const obj = {};
    let temp = obj;
    for (let i = 0; i < depth; i++) {
      temp[i] = {};
      temp = temp[i];
    }
    return obj;
  }",
            1000));
        Assert.That(error.Message, Does.Contain("Cannot serialize result: object reference chain is too long."));
    }

    [UpstreamTest("EV-049 should throw for too deep reference chain 2")]
    public async Task ShouldThrowForTooDeepReferenceChain2(IPage page, string browserName, bool isAndroid)
    {
        It.Skip(browserName != "chromium", "this is a chromium-only limitation");
        It.Skip(isAndroid, "something fails there");

        var error = await ThrowsAsync<PlaywrightException>(() => page.EvaluateAsync(
            @"depth => {
    let node = {};
    for (let i = 0; i < depth; i++)
      node = { child: node };
    return node;
  }",
            200));
        Assert.That(error.Message, Does.Contain("Cannot serialize result: object reference chain is too long."));
    }

    [UpstreamTest("EV-054 should alias Window, Document and Node")]
    public async Task ShouldAliasWindowDocumentAndNode(IPage page)
    {
        var @object = await page.EvaluateAsync<string[]>("[window, document, document.body]");
        Assert.That(@object, Is.EqualTo(new[] { "ref: <Window>", "ref: <Document>", "ref: <Node>" }));
    }

    [UpstreamTest("EV-055 should work for circular object")]
    public async Task ShouldWorkForCircularObject(IPage page)
    {
        var result = await page.EvaluateAsync<object>(@"() => {
    const a = {};
    a.b = a;
    return a;
  }");
        var a = new Dictionary<string, object>();
        a["b"] = a;
        ExpectToEqual(result, a);
    }

    [UpstreamTest("EV-056 should be able to throw a tricky error")]
    public async Task ShouldBeAbleToThrowATrickyError(IPage page)
    {
        var windowHandle = await page.EvaluateHandleAsync("() => window");
        // .NET: await windowHandle.jsonValue().catch(e => e.message)
        string errorText;
        try
        {
            errorText = await windowHandle.JsonValueAsync<string>();
        }
        catch (Exception e)
        {
            errorText = e.Message;
        }
        var error = await Catch(() => page.EvaluateAsync(
            @"errorText => {
    throw new Error(errorText);
  }",
            errorText));
        Assert.That(error.Message, Does.Contain(errorText));
    }

    [UpstreamTest("EV-057 should accept a string")]
    public async Task ShouldAcceptAString(IPage page)
    {
        var result = await page.EvaluateAsync<int>("1 + 2");
        Assert.That(result, Is.EqualTo(3));
    }

    [UpstreamTest("EV-058 should accept a string with semi colons")]
    public async Task ShouldAcceptAStringWithSemiColons(IPage page)
    {
        var result = await page.EvaluateAsync<int>("1 + 5;");
        Assert.That(result, Is.EqualTo(6));
    }

    [UpstreamTest("EV-059 should accept a string with comments")]
    public async Task ShouldAcceptAStringWithComments(IPage page)
    {
        var result = await page.EvaluateAsync<int>("2 + 5;\n// do some math!");
        Assert.That(result, Is.EqualTo(7));
    }

    [UpstreamTest("EV-060 should accept element handle as an argument")]
    public async Task ShouldAcceptElementHandleAsAnArgument(IPage page)
    {
        await page.SetContentAsync("<section>42</section>");
        var element = await page.QuerySelectorAsync("section");
        var text = await page.EvaluateAsync<string>("e => e.textContent", element);
        Assert.That(text, Is.EqualTo("42"));
    }

    [UpstreamTest("EV-061 should throw if underlying element was disposed")]
    public async Task ShouldThrowIfUnderlyingElementWasDisposed(IPage page)
    {
        await page.SetContentAsync("<section>39</section>");
        var element = await page.QuerySelectorAsync("section");
        Assert.That(element, Is.Not.Null);
        await element.DisposeAsync();
        Exception error = null;
        error = await Catch(() => page.EvaluateAsync("e => e.textContent", element));
        Assert.That(error.Message, Does.Contain("no object with guid"));
    }

    [UpstreamTest("EV-062 should simulate a user gesture")]
    public async Task ShouldSimulateAUserGesture(IPage page)
    {
        var result = await page.EvaluateAsync<bool>(@"() => {
    document.body.appendChild(document.createTextNode('test'));
    document.execCommand('selectAll');
    return document.execCommand('copy');
  }");
        Assert.That(result, Is.EqualTo(true));
    }

    [UpstreamTest("EV-063 should throw a nice error after a navigation")]
    public async Task ShouldThrowANiceErrorAfterANavigation(IPage page)
    {
        It.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
        var navigationPromise = page.WaitForNavigationAsync();
        var error = await Catch(() => page.EvaluateAsync(@"() => new Promise(f => setTimeout(() => {
    window.location.reload();
    setTimeout(() => f(42), 1000);
  }, 0))"));
        await navigationPromise;
        Assert.That(error.Message, Does.Contain("navigation"));
    }

    [UpstreamTest("EV-064 should not throw an error when evaluation does a navigation")]
    public async Task ShouldNotThrowAnErrorWhenEvaluationDoesANavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/one-style.html");
        var result = await page.EvaluateAsync<int[]>(@"() => {
    window.location.href = '/empty.html';
    return [42];
  }");
        Assert.That(result, Is.EqualTo(new[] { 42 }));
    }

    [UpstreamTest("EV-065 should not throw an error when evaluation does a synchronous navigation and returns an object")]
    public async Task ShouldNotThrowAnErrorWhenEvaluationDoesASynchronousNavigationAndReturnsAnObject(IPage page, Server server, string browserName)
    {
        // It is important to be on about:blank for sync reload.
        var result = await page.EvaluateAsync<object>(@"() => {
    window.location.reload();
    return { a: 42 };
  }");
        ExpectToEqual(result, new { a = 42 });
    }

    [UpstreamTest("EV-066 should not throw an error when evaluation does a synchronous navigation and returns undefined")]
    public async Task ShouldNotThrowAnErrorWhenEvaluationDoesASynchronousNavigationAndReturnsUndefined(IPage page)
    {
        // It is important to be on about:blank for sync reload.
        var result = await page.EvaluateAsync<object>(@"() => {
    window.location.reload();
    return undefined;
  }");
        Assert.That(result, Is.Null);
    }

    [UpstreamTest("EV-067 should transfer 100Mb of data from page to node.js")]
    public async Task ShouldTransfer100MbOfDataFromPageToNodeJs(string mode, IPage page)
    {
        It.Skip(mode != "default");

        // This is too slow with wire.
        var a = await page.EvaluateAsync<string>("() => Array(100 * 1024 * 1024 + 1).join('a')");
        Assert.That(a.Length, Is.EqualTo(100 * 1024 * 1024));
    }

    [UpstreamTest("EV-068 should throw error with detailed information on exception inside promise ")]
    public async Task ShouldThrowErrorWithDetailedInformationOnExceptionInsidePromise(IPage page)
    {
        Exception error = null;
        error = await Catch(() => page.EvaluateAsync(@"() => new Promise(() => {
    throw new Error('Error in promise');
  })"));
        Assert.That(error.Message, Does.Contain("Error in promise"));
    }

    [UpstreamTest("EV-069 should work even when JSON is set to null")]
    public async Task ShouldWorkEvenWhenJSONIsSetToNull(IPage page)
    {
        await page.EvaluateAsync("() => { window.JSON.stringify = null; window.JSON = null; }");
        var result = await page.EvaluateAsync<object>("() => ({ abc: 123 })");
        ExpectToEqual(result, new { abc = 123 });
    }

    [UpstreamTest("EV-070 should await promise from popup")]
    public async Task ShouldAwaitPromiseFromPopup(IPage page, Server server)
    {
        // Something is wrong about the way Firefox waits for the chained promise
        await page.GotoAsync(server.EmptyPage);
        var result = await page.EvaluateAsync<int>(@"() => {
    const win = window.open('about:blank');
    return new win['Promise'](f => f(42));
  }");
        Assert.That(result, Is.EqualTo(42));
    }

    [UpstreamTest("EV-071 should work with new Function() and CSP")]
    public async Task ShouldWorkWithNewFunctionAndCSP(IPage page, Server server)
    {
        server.SetCSP("/empty.html", "script-src " + server.Prefix);
        await page.GotoAsync(server.Prefix + "/empty.html");
        Assert.That(await page.EvaluateAsync<bool>("() => new Function('return true')()"), Is.EqualTo(true));
    }

    [UpstreamTest("EV-072 should work with non-strict expressions")]
    public async Task ShouldWorkWithNonStrictExpressions(IPage page, Server server)
    {
        Assert.That(
            await page.EvaluateAsync<double>(@"() => {
    // @ts-ignore
    y = 3.14;
    // @ts-ignore
    return y;
  }"),
            Is.EqualTo(3.14));
    }

    [UpstreamTest("EV-073 should respect use strict expression")]
    public async Task ShouldRespectUseStrictExpression(IPage page)
    {
        var error = await Catch(() => page.EvaluateAsync(@"() => {
    'use strict';
    // @ts-ignore
    variableY = 3.14;
    // @ts-ignore
    return variableY;
  }"));
        Assert.That(error.Message, Does.Contain("variableY"));
    }

    [UpstreamTest("EV-074 should not leak utility script")]
    public async Task ShouldNotLeakUtilityScript(IPage page)
    {
        Assert.That(await page.EvaluateAsync<bool>("this === window"), Is.EqualTo(true));
    }

    [UpstreamTest("EV-075 should not leak handles")]
    [Fixme("dotnet: the message is 'handles is not defined', without the 'page.evaluate: ' prefix of the call", Browsers = "firefox")]
    public async Task ShouldNotLeakHandles(IPage page)
    {
        var error = await Catch(() => page.EvaluateAsync("handles.length"));
        Assert.That(error.Message, Does.Contain(" handles"));
    }

    [UpstreamTest("EV-076 should work with CSP")]
    public async Task ShouldWorkWithCSP(IPage page, Server server)
    {
        server.SetCSP("/empty.html", "script-src 'self'");
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.EvaluateAsync<int>("() => 2 + 2"), Is.EqualTo(4));
    }

    [UpstreamTest("EV-077 should evaluate exception with a function on the stack")]
    [Fixme("dotnet: an Error in the result is an Exception whose Message is the stack of the error, and whose StackTrace is null")]
    public async Task ShouldEvaluateExceptionWithAFunctionOnTheStack(IPage page)
    {
        var error = await page.EvaluateAsync<Exception>(@"() => {
    return (function functionOnStack() {
      return new Error('error message');
    })();
  }");
        Assert.That(error.Message, Is.EqualTo("error message"));
        Assert.That(error.StackTrace, Does.Contain("functionOnStack"));
    }

    [UpstreamTest("EV-078 should evaluate exception")]
    [Fixme("dotnet: an Error in the result is an Exception whose Message is the stack of the error, and whose StackTrace is null; the name is lost")]
    public async Task ShouldEvaluateException(IPage page)
    {
        var error = await page.EvaluateAsync<object>(@"() => {
    function innerFunction() {
      const e = new Error('error message');
      e.name = 'foobar';
      return e;
    }
    return innerFunction();
  }");
        Assert.That(error, Is.InstanceOf<Exception>());
        Assert.That((error as Exception).Message, Is.EqualTo("error message"));
        // .NET: the name of an exception is the name of its class
        Assert.That((error as Exception).GetType().Name, Is.EqualTo("foobar"));
        Assert.That((error as Exception).StackTrace, Does.Contain("innerFunction"));
    }

    [UpstreamTest("EV-080 should evaluate date")]
    public async Task ShouldEvaluateDate(IPage page)
    {
        var result = await page.EvaluateAsync<object>("() => ({ date: new Date('2020-05-27T01:31:38.506Z') })");
        ExpectToEqual(result, new { date = new DateTime(2020, 5, 27, 1, 31, 38, 506, DateTimeKind.Utc) });
    }

    [UpstreamTest("EV-081 should roundtrip date")]
    public async Task ShouldRoundtripDate(IPage page)
    {
        var date = new DateTime(2020, 5, 27, 1, 31, 38, 506, DateTimeKind.Utc);
        var result = await page.EvaluateAsync<DateTime>("date => date", date);
        // .NET: toUTCString()
        Assert.That(result.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture), Is.EqualTo(date.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture)));
    }

    [UpstreamTest("EV-082 should roundtrip regex")]
    public async Task ShouldRoundtripRegex(IPage page)
    {
        var regex = new Regex("hello", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var result = await page.EvaluateAsync<Regex>("regex => regex", regex);
        // .NET: toString() of a RegExp has the flags, ToString() of a Regex does not
        Assert.That((result.ToString(), result.Options), Is.EqualTo((regex.ToString(), regex.Options)));
    }

    [UpstreamTest("EV-083 should jsonValue() date")]
    public async Task ShouldJsonValueDate(IPage page)
    {
        var resultHandle = await page.EvaluateHandleAsync("() => ({ date: new Date('2020-05-27T01:31:38.506Z') })");
        ExpectToEqual(await resultHandle.JsonValueAsync<object>(), new { date = new DateTime(2020, 5, 27, 1, 31, 38, 506, DateTimeKind.Utc) });
    }

    [UpstreamTest("EV-084 should evaluate url")]
    public async Task ShouldEvaluateUrl(IPage page)
    {
        var result = await page.EvaluateAsync<object>("() => ({ url: new URL('https://example.com') })");
        ExpectToEqual(result, new { url = new Uri("https://example.com") });
    }

    [UpstreamTest("EV-085 should roundtrip url")]
    public async Task ShouldRoundtripUrl(IPage page)
    {
        var url = new Uri("https://example.com");
        var result = await page.EvaluateAsync<Uri>("url => url", url);
        Assert.That(result.ToString(), Is.EqualTo(url.ToString()));
    }

    [UpstreamTest("EV-086 should jsonValue() url")]
    public async Task ShouldJsonValueUrl(IPage page)
    {
        var resultHandle = await page.EvaluateHandleAsync("() => ({ url: new URL('https://example.com') })");
        ExpectToEqual(await resultHandle.JsonValueAsync<object>(), new { url = new Uri("https://example.com") });
    }

    [UpstreamTest("EV-087 should not use toJSON when evaluating")]
    public async Task ShouldNotUseToJSONWhenEvaluating(IPage page)
    {
        var result = await page.EvaluateAsync<object>("() => ({ toJSON: () => 'string', data: 'data' })");
        ExpectToEqual(result, new { data = "data", toJSON = new { } });
    }

    [UpstreamTest("EV-088 should not use Array.prototype.toJSON when evaluating")]
    public async Task ShouldNotUseArrayPrototypeToJSONWhenEvaluating(IPage page)
    {
        var result = await page.EvaluateAsync<int[]>(@"() => {
    Array.prototype.toJSON = () => 'busted';
    return [1, 2, 3];
  }");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [UpstreamTest("EV-089 should not add a toJSON property to newly created Arrays after evaluation")]
    public async Task ShouldNotAddAToJSONPropertyToNewlyCreatedArraysAfterEvaluation(IPage page, string browserName)
    {
        await page.EvaluateAsync("() => []");
        var hasToJSONProperty = await page.EvaluateAsync<bool>("() => 'toJSON' in []");
        Assert.That(hasToJSONProperty, Is.EqualTo(false));
    }

    [UpstreamTest("EV-090 should not use toJSON in jsonValue")]
    public async Task ShouldNotUseToJSONInJsonValue(IPage page)
    {
        var resultHandle = await page.EvaluateHandleAsync("() => ({ toJSON: () => 'string', data: 'data' })");
        ExpectToEqual(await resultHandle.JsonValueAsync<object>(), new { data = "data", toJSON = new { } });
    }

    [UpstreamTest("EV-091 should ignore buggy toJSON")]
    public async Task ShouldIgnoreBuggyToJSON(IPage page)
    {
        var result = await page.EvaluateAsync<object>(@"() => {
    class Foo {
      toJSON() {
        throw new Error('Bad');
      }
    }
    class Bar {
      get toJSON() {
        throw new Error('Also bad');
      }
    }
    return { foo: new Foo(), bar: new Bar() };
  }");
        ExpectToEqual(result, new { foo = new { }, bar = new { } });
    }

    [UpstreamTest("EV-092 should not expose the injected script export")]
    public async Task ShouldNotExposeTheInjectedScriptExport(IPage page)
    {
        Assert.That(await page.EvaluateAsync<bool>("typeof pwExport === \"undefined\""), Is.EqualTo(true));
    }

    [UpstreamTest("EV-093 should throw when frame is detached")]
    [Fixme("dotnet: the message is 'Frame was detached', without the 'frame.evaluate: ' prefix of the call")]
    public async Task ShouldThrowWhenFrameIsDetached(IPage page, Server server)
    {
        await AttachFrame(page, "frame1", server.EmptyPage);
        var frame = page.Frames[1];
        var promise = Catch(() => frame.EvaluateAsync("() => new Promise(() => {})"));
        await DetachFrame(page, "frame1");
        var error = await promise;
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Match("frame.evaluate: (Frame was detached|Execution context was destroyed)"));
    }

    [UpstreamTest("EV-094 should work with overridden Object.defineProperty")]
    public async Task ShouldWorkWithOverriddenObjectDefineProperty(IPage page, Server server)
    {
        server.SetRoute("/test", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["content-type"] = "text/html",
            });
            res.End(@"<script>
    Object.create = null;
    Object.defineProperty = null;
    Object.getOwnPropertyDescriptor = null;
    Object.getOwnPropertyNames = null;
    Object.getPrototypeOf = null;
    Object.prototype.hasOwnProperty = null;
    </script>");
        });
        await page.GotoAsync(server.Prefix + "/test");
        Assert.That(await page.EvaluateAsync<int>("1+2"), Is.EqualTo(3));
    }

    [UpstreamTest("EV-095 should work with busted Array.prototype.map/push")]
    public async Task ShouldWorkWithBustedArrayPrototypeMapPush(IPage page, Server server)
    {
        server.SetRoute("/test", (req, res) =>
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
        await page.GotoAsync(server.Prefix + "/test");
        Assert.That(await page.EvaluateAsync<int>("1+2"), Is.EqualTo(3));
        Assert.That(await (await page.EvaluateHandleAsync("1+2")).JsonValueAsync<int>(), Is.EqualTo(3));
    }

    [UpstreamTest("EV-096 should work with overridden globalThis.Window/Document/Node")]
    public async Task ShouldWorkWithOverriddenGlobalThisWindowDocumentNode(IPage page, Server server)
    {
        var testCases = new[]
        {
            // @ts-ignore
            "() => globalThis.Window = {}",
            // @ts-ignore
            "() => globalThis.Document = {}",
            // @ts-ignore
            "() => globalThis.Node = {}",
            "() => globalThis.Window = null",
            "() => globalThis.Document = null",
            "() => globalThis.Node = null",
        };
        foreach (var testCase in testCases)
        {
            await It.Step(testCase, async () =>
            {
                await page.GotoAsync(server.EmptyPage);
                await page.EvaluateAsync(testCase);
                Assert.That(await page.EvaluateAsync<int>("1+2"), Is.EqualTo(3));
                Assert.That(await page.EvaluateAsync<string[]>("() => ['foo']"), Is.EqualTo(new[] { "foo" }));
            });
        }
    }

    [UpstreamTest("EV-097 should work with overridden URL/Date/RegExp")]
    public async Task ShouldWorkWithOverriddenURLDateRegExp(IPage page, Server server)
    {
        var testCases = new[]
        {
            // @ts-ignore
            "() => globalThis.URL = 'foo'",
            // @ts-ignore
            "() => globalThis.RegExp = 'foo'",
            // @ts-ignore
            "() => globalThis.Date = 'foo'",
        };
        foreach (var testCase in testCases)
        {
            await It.Step(testCase, async () =>
            {
                await page.GotoAsync(server.EmptyPage);
                await page.EvaluateAsync(testCase);
                Assert.That(await page.EvaluateAsync<int>("1+2"), Is.EqualTo(3));
                ExpectToEqual(await page.EvaluateAsync<object>("() => ({ 'a': 2023 })"), new { a = 2023 });
            });
        }
    }

    [UpstreamTest("EV-098 should work with Array.from/map")]
    public async Task ShouldWorkWithArrayFromMap(IPage page)
    {
        Assert.That(
            await page.EvaluateAsync<string>(@"() => {
    const r = (str, amount) => Array.from(Array(amount)).map(() => str).join('');
    return r('([a-f0-9]{2})', 3);
  }"),
            Is.EqualTo("([a-f0-9]{2})([a-f0-9]{2})([a-f0-9]{2})"));
    }

    [UpstreamTest("EV-099 should work with a using declaration")]
    public async Task ShouldWorkWithAUsingDeclaration(IPage page, (int Major, int Minor, int Patch) nodeVersion, string browserName, int browserMajorVersion)
    {
        It.Skip(nodeVersion.Major < 24, "using is lowered to a module-scope helper that does not survive evaluate serialization on Node < 24");
        It.Skip(browserName == "webkit", "WebKit does not support using declarations");
        It.Skip(browserName == "chromium" && browserMajorVersion < 134, "Chromium supports using declarations since 134");
        var disposed = await page.EvaluateAsync<bool>(@"() => {
    let disposed = false;
    {
      using r = { [Symbol.dispose]: () => { disposed = true; } };
      void r;
    }
    return disposed;
  }");
        Assert.That(disposed, Is.EqualTo(true));
    }

    [UpstreamTest("EV-100 should ignore dangerous object keys")]
    public async Task ShouldIgnoreDangerousObjectKeys(IPage page)
    {
        var input = new
        {
            __proto__ = new { polluted = true },
            safeKey = "safeValue",
        };
        var result = await page.EvaluateAsync<object>("arg => arg", input);
        ExpectToEqual(result, new { safeKey = "safeValue" });
    }
}
