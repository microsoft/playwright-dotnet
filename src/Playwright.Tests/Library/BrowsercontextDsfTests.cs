/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-dsf.spec.ts.</summary>
public class BrowsercontextDsfTests
{
    [UpstreamTest("DV-009 should fetch lodpi assets @smoke")]
    public async Task ShouldFetchLodpiAssets(ContextFactory contextFactory, Server server)
    {
        var context = await contextFactory(new()
        {
            DeviceScaleFactor = 1
        });
        var page = await context.NewPageAsync();
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync("**/image*"),
            page.GotoAsync(server.Prefix + "/highdpi.html"));
        Assert.That(request.Url, Does.Contain("image1x"));
    }

    [UpstreamTest("DV-010 should fetch hidpi assets")]
    public async Task ShouldFetchHidpiAssets(ContextFactory contextFactory, Server server)
    {
        var context = await contextFactory(new()
        {
            DeviceScaleFactor = 2
        });
        var page = await context.NewPageAsync();
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync("**/image*"),
            page.GotoAsync(server.Prefix + "/highdpi.html"));
        Assert.That(request.Url, Does.Contain("image2x"));
    }
}
