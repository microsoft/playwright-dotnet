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

using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/modernizr.spec.ts.</summary>
public class ModernizrTests
{
    private static async Task<(Dictionary<string, object> actual, JsonObject expected)> CheckFeatures(string name, IBrowserContext context, Server server)
    {
        try
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/modernizr/index.html");
            var actual = await page.EvaluateAsync<Dictionary<string, object>>("window.report");
            // .NET: JSON.parse() gives an object that the tests change, a JsonObject.
            var expected = JsonNode.Parse(File.ReadAllText(Asset($"modernizr/{name}.json"))).AsObject();
            return (actual, expected);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [UpstreamTest("CA-030 Safari Desktop")]
    public async Task SafariDesktop(IBrowser browser, string browserName, string platform, Server httpsServer, string channel, bool isFrozenWebkit, BrowserNewContextOptions contextOptions)
    {
        It.Skip(browserName != "webkit");
        It.Skip(browserName == "webkit" && platform == "darwin" && RuntimeInformation.ProcessArchitecture == Architecture.X64, "Modernizr uses WebGL which is not available on Intel macOS - https://bugs.webkit.org/show_bug.cgi?id=278277");
        It.Skip(isFrozenWebkit);
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            DeviceScaleFactor = 2,
            IgnoreHTTPSErrors = true,
        });
        var (actual, expected) = await CheckFeatures("safari-26", context, httpsServer);

        // Shipping Safari exposes `font-display` as a settable CSS property (element.style.fontDisplay === ''),
        // but open-source WebKit keeps it a @font-face descriptor only, so it is undefined here. No runtime
        // flag bridges the gap, hence the override.
        expected["fontdisplay"] = false;

        expected.Remove("webglextensions");
        actual.Remove("webglextensions");
        // .NET: !!value of a JsonNode, and of a value of the page.
        expected["audio"] = expected["audio"]?.GetValueKind() is not (null or JsonValueKind.False);
        actual["audio"] = actual["audio"] is not (null or false);
        expected["video"] = expected["video"]?.GetValueKind() is not (null or JsonValueKind.False);
        actual["video"] = actual["video"] is not (null or false);

        if (platform == "linux" || channel == "webkit-wsl")
        {
            expected["speechrecognition"] = false;
            expected["mediastream"] = false;
            expected["todataurlwebp"] = true;

            // GHA
            actual.Remove("variablefonts");
            expected.Remove("variablefonts");
        }

        if (platform == "win32" && channel != "webkit-wsl")
        {
            expected["getusermedia"] = false;
            expected["peerconnection"] = false;
            expected["speechrecognition"] = false;
            expected["speechsynthesis"] = false;
            expected["todataurlwebp"] = true;
            expected["webaudio"] = false;
            expected["gamepads"] = false;

            expected.Remove("datalistelem");

            expected["mediastream"] = false;
            expected["mediasource"] = false;
            expected["datachannel"] = false;

            expected["inputtypes"]["color"] = false;
            expected["inputtypes"]["date"] = false;
            expected["inputtypes"]["datetime-local"] = false;
            expected["inputtypes"]["time"] = false;
        }

        // .NET: ExpectToEqual takes the JsonObject as a JsonElement.
        ExpectToEqual(actual, expected.Deserialize<JsonElement>());
    }

    [UpstreamTest("CA-031 Mobile Safari")]
    public async Task MobileSafari(IPlaywright playwright, IBrowser browser, string browserName, string platform, Server httpsServer, string channel, bool isFrozenWebkit, BrowserNewContextOptions contextOptions)
    {
        It.Skip(browserName != "webkit");
        It.Skip(browserName == "webkit" && platform == "darwin" && RuntimeInformation.ProcessArchitecture == Architecture.X64, "Modernizr uses WebGL which is not available on Intel macOS - https://bugs.webkit.org/show_bug.cgi?id=278277");
        It.Skip(isFrozenWebkit);
        var iPhone = playwright.Devices["iPhone 12"];
        var context = await browser.NewContextAsync(MergeOptions(
            new BrowserNewContextOptions(iPhone)
            {
                IgnoreHTTPSErrors = true,
            },
            contextOptions));
        var (actual, expected) = await CheckFeatures("mobile-safari-26", context, httpsServer);

        {
            // All platforms.
            expected["capture"] = false;
            expected["cssscrollbar"] = true;
            expected["cssvhunit"] = true;
            expected["cssvmaxunit"] = true;
            expected["overflowscrolling"] = false;
            expected["mediasource"] = true;
            expected["scrolltooptions"] = false;

            // Shipping Safari exposes `font-display` as a settable CSS property (element.style.fontDisplay === ''),
            // but open-source WebKit keeps it a @font-face descriptor only, so it is undefined here. No runtime
            // flag bridges the gap, hence the override.
            expected["fontdisplay"] = false;

            expected.Remove("webglextensions");
            actual.Remove("webglextensions");
            // .NET: !!value of a JsonNode, and of a value of the page.
            expected["audio"] = expected["audio"]?.GetValueKind() is not (null or JsonValueKind.False);
            actual["audio"] = actual["audio"] is not (null or false);
            expected["video"] = expected["video"]?.GetValueKind() is not (null or JsonValueKind.False);
            actual["video"] = actual["video"] is not (null or false);
        }

        if (platform == "linux" || channel == "webkit-wsl")
        {
            expected["speechrecognition"] = false;
            expected["mediastream"] = false;
            expected["todataurlwebp"] = true;

            // GHA
            actual.Remove("variablefonts");
            expected.Remove("variablefonts");
        }

        if (platform == "win32" && channel != "webkit-wsl")
        {
            expected["getusermedia"] = false;
            expected["peerconnection"] = false;
            expected["speechrecognition"] = false;
            expected["speechsynthesis"] = false;
            expected["todataurlwebp"] = true;
            expected["webaudio"] = false;
            expected["gamepads"] = false;

            expected.Remove("datalistelem");

            expected["mediastream"] = false;
            expected["mediasource"] = false;
            expected["datachannel"] = false;

            expected["inputtypes"]["color"] = false;
            expected["inputtypes"]["date"] = false;
            expected["inputtypes"]["datetime-local"] = false;
            expected["inputtypes"]["time"] = false;
        }

        // .NET: ExpectToEqual takes the JsonObject as a JsonElement.
        ExpectToEqual(actual, expected.Deserialize<JsonElement>());
    }
}
