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

/// <summary>Port of tests/library/browsertype-basic.spec.ts.</summary>
public class BrowsertypeBasicTests : UpstreamBrowserTest
{
    [UpstreamTest("BT-001 browserType.executablePath should work")]
    public async Task BrowserTypeExecutablePathShouldWork()
    {
        Info.Skip(!string.IsNullOrEmpty(Channel), "We skip browser download when testing a channel");
        // .NET: the launchOptions fixture is (browserType as any)._playwright._defaultLaunchOptions.
        Info.Skip(!string.IsNullOrEmpty(LaunchOptions().ExecutablePath), "Skip with custom executable path");

        var executablePath = BrowserType.ExecutablePath;
        Assert.That(File.Exists(executablePath), Is.EqualTo(true));
    }

    [UpstreamTest("BT-002 browserType.name should work")]
    public async Task BrowserTypeNameShouldWork()
    {
        Assert.That(BrowserType.Name, Is.EqualTo(BrowserName));
    }

    [UpstreamTest("BT-003 should throw when trying to connect with not-chromium")]
    public async Task ShouldThrowWhenTryingToConnectWithNotChromium()
    {
        Info.Skip(BrowserName == "chromium" || BrowserName == "webkit");

        var error = await Catch(() => BrowserType.ConnectOverCDPAsync("ws://foo"));
        Assert.That(error.Message, Is.EqualTo("Connecting over CDP is only supported in Chromium and WebKit."));
    }
}
