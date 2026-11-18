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

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/jshandle-evaluate.spec.ts.</summary>
public class JshandleEvaluateTests : UpstreamPageTest
{
    [UpstreamTest("JH-005 should work with function @smoke")]
    public async Task ShouldWorkWithFunction()
    {
        var windowHandle = await Page.EvaluateHandleAsync(@"() => {
    window['foo'] = [1, 2];
    return window;
  }");
        Assert.That(await windowHandle.EvaluateAsync<int[]>("w => w['foo']"), Is.EqualTo(new[] { 1, 2 }));
    }

    [UpstreamTest("JH-006 should work with expression")]
    public async Task ShouldWorkWithExpression()
    {
        var windowHandle = await Page.EvaluateHandleAsync(@"() => {
    window['foo'] = [1, 2];
    return window;
  }");
        Assert.That(await windowHandle.EvaluateAsync<int[]>("window.foo"), Is.EqualTo(new[] { 1, 2 }));
    }
}
