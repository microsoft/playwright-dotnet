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

/// <summary>Port of tests/library/shared-worker.spec.ts.</summary>
public class SharedWorkerTests : UpstreamBrowserTest
{
    [UpstreamTest("WO-026 should survive shared worker restart")]
    public async Task ShouldSurviveSharedWorkerRestart()
    {
        var context = await ContextFixture();
        var page1 = await context.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/shared-worker/shared-worker.html");
        Assert.That(await page1.EvaluateAsync<string>("window.sharedWorkerResponsePromise"), Is.EqualTo("echo:hello"));
        await page1.CloseAsync();

        var page2 = await context.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/shared-worker/shared-worker.html");
        Assert.That(await page2.EvaluateAsync<string>("window.sharedWorkerResponsePromise"), Is.EqualTo("echo:hello"));
        await page2.CloseAsync();
    }
}
