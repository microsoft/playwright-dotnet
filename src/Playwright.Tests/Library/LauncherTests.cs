/**
 * Copyright 2017 Google Inc. All rights reserved.
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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/launcher.spec.ts.</summary>
public class LauncherTests
{
    [UpstreamTest("BT-019 should have a devices object")]
    public async Task ShouldHaveADevicesObject(IPlaywright playwright)
    {
        Assert.That(playwright.Devices["iPhone 6"], Is.Not.Null);
        // .NET: expect(playwright.devices['iPhone 6'].defaultBrowserType).toBe('webkit'): a device is a BrowserNewContextOptions, which has no defaultBrowserType.
    }

    [UpstreamTest("BT-021 should throw a friendly error if its headed and there is no xserver on linux running")]
    public async Task ShouldThrowAFriendlyErrorIfItsHeadedAndThereIsNoXserverOnLinuxRunning(string mode, IBrowserType browserType, string platform, string channel, BrowserTypeLaunchOptions launchOptions)
    {
        Info.Skip(platform != "linux");
        Info.Skip(channel == "chromium-headless-shell", "shell is never headed");

        var error = await Catch(() => browserType.LaunchAsync(new(launchOptions)
        {
            Headless = false,
            Env = TestChildProcess.InheritAndCleanEnv(new Dictionary<string, string> { ["DISPLAY"] = null }),
        }));
        Assert.That(error, Is.InstanceOf<Exception>());
        Assert.That(error.Message, Does.Match("Looks like you launched a headed browser without having a XServer running."));
        Assert.That(error.Message, Does.Match("xvfb-run"));
    }
}
