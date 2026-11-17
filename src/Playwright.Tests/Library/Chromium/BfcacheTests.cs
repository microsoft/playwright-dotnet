/**
 * Copyright Microsoft Corporation. All rights reserved.
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

namespace Microsoft.Playwright.Tests.Library.Chromium;

/// <summary>Port of tests/library/chromium/bfcache.spec.ts.</summary>
public class BfcacheTests
{
    // .NET: upstream runs the files of tests/library/chromium only in the Chromium project.
    [BeforeEach]
    public static void Skip(string browserName) => Info.Skip(browserName != "chromium", "chromium only");

    // test.use({
    //   launchOptions: async ({ launchOptions }, use) => {
    //     await use({ ...launchOptions, ignoreDefaultArgs: ['--disable-back-forward-cache'] });
    //   }
    // });
    [Fixture]
    public static BrowserTypeLaunchOptions LaunchOptions(BrowserTypeLaunchOptions launchOptions) => new(launchOptions) { IgnoreDefaultArgs = ["--disable-back-forward-cache"] };

    [UpstreamTest("CR-042 bindings should work after restoring from bfcache")]
    public async Task BindingsShouldWorkAfterRestoringFromBfcache(IPage page, Server server)
    {
        await page.ExposeFunctionAsync("add", (int a, int b) => a + b);

        await page.GotoAsync(server.Prefix + "/cached/bfcached.html");
        Assert.That(await page.EvaluateAsync<int>("window.add(1, 2)"), Is.EqualTo(3));

        await page.SetContentAsync("<a href='about:blank'}>click me</a>");
        await page.ClickAsync("a");

        await page.GoBackAsync(new() { WaitUntil = WaitUntilState.Commit });
        await page.EvaluateAsync("window.didShow");
        Assert.That(await page.EvaluateAsync<int>("window.add(2, 3)"), Is.EqualTo(5));
    }
}
