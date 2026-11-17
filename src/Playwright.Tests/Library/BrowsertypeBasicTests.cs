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
public class BrowsertypeBasicTests
{
    [UpstreamTest("BT-001 browserType.executablePath should work")]
    public async Task BrowserTypeExecutablePathShouldWork(IBrowserType browserType, string channel, string mode, BrowserTypeLaunchOptions launchOptions)
    {
        Info.Skip(!string.IsNullOrEmpty(channel), "We skip browser download when testing a channel");
        // .NET: the launchOptions fixture is (browserType as any)._playwright._defaultLaunchOptions.
        Info.Skip(!string.IsNullOrEmpty(launchOptions.ExecutablePath), "Skip with custom executable path");

        var executablePath = browserType.ExecutablePath;
        Assert.That(File.Exists(executablePath), Is.EqualTo(true));
    }

    [UpstreamTest("BT-002 browserType.name should work")]
    public async Task BrowserTypeNameShouldWork(IBrowserType browserType, string browserName)
    {
        Assert.That(browserType.Name, Is.EqualTo(browserName));
    }

    [UpstreamTest("BT-003 should throw when trying to connect with not-chromium")]
    [LocalFixme("dotnet: the message is 'Connecting over CDP is only supported in Chromium.', upstream expects 'Connecting over CDP is only supported in Chromium and WebKit.' (only in firefox)")]
    public async Task ShouldThrowWhenTryingToConnectWithNotChromium(IBrowserType browserType, string browserName)
    {
        Info.Skip(browserName == "chromium" || browserName == "webkit");

        var error = await Catch(() => browserType.ConnectOverCDPAsync("ws://foo"));
        Assert.That(error.Message, Is.EqualTo("Connecting over CDP is only supported in Chromium and WebKit."));
    }
}
