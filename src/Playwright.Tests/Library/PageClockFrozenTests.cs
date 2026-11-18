/**
 * Copyright (c) Microsoft Corporation.
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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/page-clock.frozen.spec.ts.</summary>
public class PageClockFrozenTests : UpstreamPageTest
{
    [UpstreamTest("CK-055 clock should be frozen")]
    public async Task ClockShouldBeFrozen()
    {
        Info.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") != "frozen");
        Assert.That(await Page.EvaluateAsync<long>("Date.now()"), Is.EqualTo(1000));
    }

    [UpstreamTest("CK-056 clock should be realtime")]
    public async Task ClockShouldBeRealtime()
    {
        Info.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") != "realtime");
        Assert.That(await Page.EvaluateAsync<long>("Date.now()"), Is.LessThan(10000));
    }
}
