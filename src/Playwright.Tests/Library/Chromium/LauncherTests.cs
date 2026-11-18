/**
 * Copyright 2019 Google Inc. All rights reserved.
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

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;

namespace Microsoft.Playwright.Tests.Library.Chromium;

/// <summary>Port of tests/library/chromium/launcher.spec.ts.</summary>
public class LauncherTests : UpstreamBrowserTest
{
    [UpstreamTest("CR-035 should not create pages automatically")]
    public async Task ShouldNotCreatePagesAutomatically()
    {
        var browser = await BrowserType.LaunchAsync(LaunchOptions());
        var browserSession = await browser.NewBrowserCDPSessionAsync();
        var targets = new List<JsonElement>();
        browserSession.Event("Target.targetCreated").OnEvent += (_, @event) =>
        {
            var targetInfo = @event.Value.GetProperty("targetInfo");
            if (targetInfo.GetProperty("type").GetString() != "browser")
            {
                targets.Add(targetInfo);
            }
        };
        await browserSession.SendAsync("Target.setDiscoverTargets", new() { ["discover"] = true });
        await browser.NewContextAsync(ContextOptions());
        await browser.CloseAsync();
        Assert.That(targets.Count, Is.EqualTo(0));
    }
}
