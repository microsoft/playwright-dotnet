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

using System.Globalization;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/favicon.spec.ts.</summary>
public class FaviconTests
{
    [UpstreamTest("FA-001 should load svg favicon with prefer-color-scheme")]
    public async Task ShouldLoadSvgFaviconWithPreferColorScheme(IPage page, Server server, string browserName, bool headless, Func<string, string> asset, string channel)
    {
        Info.Skip(headless && browserName != "firefox", "headless browsers, except firefox, do not request favicons");
        Info.Skip(!headless && browserName == "webkit", "headed webkit does not have a favicon feature");

        // Browsers aggressively cache favicons, so force bust with the
        // `d` parameter to make iterating on this test more predictable and isolated.
        var favicon = $"/favicon.svg?d={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)}";
        server.SetRoute(favicon, (req, res) =>
        {
            server.ServeFile(req, res, asset("media-query-prefers-color-scheme.svg"));
        });

        server.SetRoute("/page.html", (_, res) =>
        {
            res.End($@"
      <!DOCTYPE html>
      <html>
        <head>
          <meta charset=""utf-8"">
          <link rel=""icon"" type=""image/svg+xml"" href=""{favicon}"">
          <title>SVG Favicon Test</title>
        </head>
        <body>
          favicons
        </body>
      </html>
");
        });

        await PromiseAll(
            server.WaitForRequest(favicon),
            page.GotoAsync(server.Prefix + "/page.html"));

        // Add artificial delay since, just because we saw the request for the favicon,
        // it does not mean the browser has processed it. There's not a great way to
        // hook into something like "favicon is fully displayed" event, so hopefully
        // 500ms is enough, but not too much!
        await page.WaitForTimeoutAsync(500);
        // Text still being around ensures we haven't actually lost our browser to a crash.
        await page.WaitForSelectorAsync("text=favicons");
    }
}
