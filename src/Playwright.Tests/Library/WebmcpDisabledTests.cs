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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/webmcp-disabled.spec.ts.</summary>
public class WebmcpDisabledTests
{
    [BeforeEach]
    public static void Skip(string browserName) => Info.Skip(browserName == "webkit", "WebKit does not implement WebMCP");

    [BeforeEach]
    public static void Skip2(bool isBidi) => Info.Skip(isBidi, "WebMCP is not implemented over BiDi");

    [UpstreamTest("WM-015 should throw when the browser was launched without WebMCP")]
    public async Task ShouldThrowWhenTheBrowserWasLaunchedWithoutWebMCP(IPage page, string browserName)
    {
        var error = await Catch(() => page.Webmcp.ToolsAsync());
        Assert.That(error.Message, Does.Contain("WebMCP is not enabled."));
        Assert.That(error.Message, Does.Contain(browserName == "firefox" ? "dom.modelcontext.enabled" : "--enable-features=WebMCP"));
    }
}
