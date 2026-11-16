/**
 * Copyright 2018 Google Inc. All rights reserved.
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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace Microsoft.Playwright.Tests.Library.Chromium;

/// <summary>Port of tests/library/chromium/disable-web-security.spec.ts.</summary>
public class DisableWebSecurityTests
{
    // .NET: upstream runs the files of tests/library/chromium only in the Chromium project.
    [BeforeEach]
    public static void Skip(string browserName) => It.Skip(browserName != "chromium", "chromium only");

    // it.use({
    //   launchOptions: async ({ launchOptions }, use) => {
    //     await use({ ...launchOptions, args: ['--disable-web-security'] });
    //   }
    // });
    [Fixture]
    public static BrowserTypeLaunchOptions LaunchOptions(BrowserTypeLaunchOptions launchOptions) => new(launchOptions) { Args = ["--disable-web-security"] };

    [UpstreamTest("CR-045 test utility world in popup w/ --disable-web-security")]
    public async Task TestUtilityWorldInPopupWDisableWebSecurity(IPage page, Server server)
    {
        server.SetRoute("/main.html", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/html",
            });
            res.End($"<a href=\"{server.Prefix}/target.html\" target=\"_blank\">Click me</a>");
        });
        server.SetRoute("/target.html", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/html",
            });
            res.End("<html></html>");
        });

        await page.GotoAsync(server.Prefix + "/main.html");
        var page1Promise = page.Context.WaitForPageAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Click me" }).ClickAsync();
        var page1 = await page1Promise;
        await Expect(page1).ToHaveURLAsync(new Regex("target"));
    }

    [UpstreamTest("CR-046 test init script w/ --disable-web-security")]
    public async Task TestInitScriptWDisableWebSecurity(IPage page, Server server)
    {
        server.SetRoute("/main.html", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/html",
            });
            res.End($"<a href=\"{server.Prefix}/target.html\" target=\"_blank\">Click me</a>");
        });
        server.SetRoute("/target.html", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/html",
            });
            res.End("<html></html>");
        });

        await page.Context.AddInitScriptAsync("window.injected = 123");
        await page.GotoAsync(server.Prefix + "/main.html");
        var page1Promise = page.Context.WaitForPageAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Click me" }).ClickAsync();
        var page1 = await page1Promise;
        var value = await page1.EvaluateAsync<int>("window.injected");
        Assert.That(value, Is.EqualTo(123));
    }
}
