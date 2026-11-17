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
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/jshandle-json-value.spec.ts.</summary>
public class JshandleJsonValueTests : UpstreamPageTest
{
    [UpstreamTest("JH-007 should work @smoke")]
    public async Task ShouldWork()
    {
        var aHandle = await Page.EvaluateHandleAsync("() => ({ foo: 'bar' })");
        var json = await aHandle.JsonValueAsync<object>();
        ExpectToEqual(json, new { foo = "bar" });
    }

    [UpstreamTest("JH-008 should work with dates")]
    public async Task ShouldWorkWithDates()
    {
        var dateHandle = await Page.EvaluateHandleAsync("() => new Date('2017-09-26T00:00:00.000Z')");
        var date = await dateHandle.JsonValueAsync<DateTime>();
        // .NET: toJSON()
        Assert.That(date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture), Is.EqualTo("2017-09-26T00:00:00.000Z"));
    }

    [UpstreamTest("JH-009 should handle circular objects")]
    public async Task ShouldHandleCircularObjects()
    {
        var handle = await Page.EvaluateHandleAsync("const a = {}; a.b = a; a");
        var a = new Dictionary<string, object>();
        a["b"] = a;
        ExpectToEqual(await handle.JsonValueAsync<object>(), a);
    }
}
