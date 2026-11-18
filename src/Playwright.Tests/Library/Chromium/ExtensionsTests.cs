/**
 * Copyright 2019 Google Inc. All rights reserved.
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

namespace Microsoft.Playwright.Tests.Library.Chromium;

public abstract class ExtensionsTestsBase : UpstreamBrowserTest
{
    protected async Task<IBrowserContext> LaunchPersistentContext(string extensionPath, BrowserTypeLaunchPersistentContextOptions options = null)
    {
        var extensionOptions = new BrowserTypeLaunchPersistentContextOptions(options)
        {
            Args =
            [
                $"--disable-extensions-except={extensionPath}",
                $"--load-extension={extensionPath}",
            ],
        };
        return await BrowserType.LaunchPersistentContextAsync(string.Empty, MergeOptions(extensionOptions, LaunchPersistentContextOptions()));
    }

    // .NET: upstream closes the contexts of a list that it never fills, so there is no teardown.

    [SetUp]
    public void Skip() => Info.Skip(IsHeadlessShell, "Headless Shell has no support for extensions");
}

/// <summary>Port of tests/library/chromium/extensions.spec.ts.</summary>
public class ExtensionsTests : ExtensionsTestsBase
{
    public class MV3 : ExtensionsTestsBase
    {
        [SetUp]
        public void Skip2() => Info.Skip(Channel?.StartsWith("chrome", StringComparison.Ordinal) == true, "--load-extension is not supported in Chrome anymore. https://groups.google.com/a/chromium.org/g/chromium-extensions/c/1-g8EFx2BBY/m/S0ET5wPjCAAJ");

        [UpstreamTest("CR-040 should report console messages from content script")]
        public async Task ShouldReportConsoleMessagesFromContentScript()
        {
            var extensionPath = Asset("extension-mv3-with-logging");
            var context = await LaunchPersistentContext(extensionPath);
            var page = await context.NewPageAsync();
            var consolePromise = page.WaitForConsoleMessageAsync(new() { Predicate = e => e.Text.Contains("Test console log from a third-party execution context") });
            await page.GotoAsync(Server.EmptyPage);
            var message = await consolePromise;
            Assert.That(message.Text, Does.Contain("Test console log from a third-party execution context"));
            await context.CloseAsync();
        }
    }
}
