/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-event-load.spec.ts.</summary>
public class PageEventLoadTests
{
    [UpstreamTest("NA-087 should fire once")]
    public async Task ShouldFireOnce(IPage page, Server server, string browserName, bool isBidi)
    {
        Info.Fixme(browserName == "firefox" && !isBidi, "Firefox sometimes double fires.");

        var count = 0;
        page.Load += (_, _) => count++;
        await page.GotoAsync(server.Prefix + "/one-style.html");
        await page.WaitForTimeoutAsync(1000);
        Assert.That(count, Is.EqualTo(1));
    }

    [UpstreamTest("NA-088 should fire once with iframe navigation")]
    public async Task ShouldFireOnceWithIframeNavigation(IPage page, Server server, string browserName, bool isBidi)
    {
        Info.Fixme(browserName == "firefox" && !isBidi, "Firefox sometimes double fires.");

        var requestCount = 0;
        server.SetRoute("/tracker", (_, res) =>
        {
            res.Write($"request count: {++requestCount}");
            res.End();
        });
        server.SetRoute("/home", (_, res) =>
        {
            res.Write(@"
      <!DOCTYPE html>
      <html>
        <head>
        </head>
        <body>
          <script>
            window.eventLog = [];
            window.addEventListener('load', () => {
              window.eventLog.push('load');
            });
          </script>
          <form id=""trackerForm"" action=""/tracker"" method=""post"" target=""tracker"">
            <input type=""submit"">
          </form>
          <iframe name=""tracker"" src=""/tracker"">
        </body>
      </html>
    ");
            res.End();
        });
        var count = 0;
        page.Load += (_, _) => count++;
        await page.GotoAsync(server.Prefix + "/home");
        var trackingFrame = page.FrameLocator("[name=tracker]");
        await Expect(trackingFrame.Locator(":scope")).ToContainTextAsync("request count: 1");
        // .NET: page.waitForEvent('load').then(() => 'loadfired') and page.waitForTimeout(1000).then(() => 'timeout') are local functions that are called in their place.
        async Task<string> LoadFired()
        {
            await Once<IPage>(h => page.Load += h, h => page.Load -= h);
            return "loadfired";
        }
        async Task<string> Timeout()
        {
            await page.WaitForTimeoutAsync(1000);
            return "timeout";
        }
        var loadFired = Task.WhenAny(
            LoadFired(),
            Timeout());
        await page.Locator("input[type=submit]").ClickAsync();
        Assert.That(await await loadFired, Is.EqualTo("timeout"));
        Assert.That(count, Is.EqualTo(1));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['eventLog']"), Is.EqualTo(new[] { "load" }));
    }
}
