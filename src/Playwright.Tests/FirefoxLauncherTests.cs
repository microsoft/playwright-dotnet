/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests.Firefox;

///<playwright-file>firefox/launcher.spec.ts</playwright-file>
///<playwright-describe>launcher</playwright-describe>
public class FirefoxLauncherTests : PlaywrightTestEx
{
    [PlaywrightTest("firefox/launcher.spec.ts", "should pass firefox user preferences")]
    [Skip(SkipAttribute.Targets.Chromium, SkipAttribute.Targets.Webkit)]
    public async Task ShouldPassFirefoxUserPreferences()
    {
        var firefoxUserPrefs = new Dictionary<string, object>
        {
            ["network.proxy.type"] = 1,
            ["network.proxy.http"] = "127.0.0.1",
            ["network.proxy.http_port"] = 333,
        };

        await using var browser = await BrowserType.LaunchAsync(new() { FirefoxUserPrefs = firefoxUserPrefs });
        var page = await browser.NewPageAsync();
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync("http://example.com"));

        StringAssert.Contains("NS_ERROR_PROXY_CONNECTION_REFUSED", exception.Message);
    }
}
