/**
 * Copyright (c) Microsoft Corporation.
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

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browser-server.spec.ts.</summary>
public class BrowserServerTests
{
    [BeforeEach]
    public static void Skip(string mode) => Info.Skip(mode != "default");

    [BeforeEach]
    public void BeforeEach(TestInfo testInfo, string serverRegistry)
    {
        // .NET: process.env.PWTEST_SERVER_REGISTRY = testInfo.outputPath('registry') is the serverRegistry fixture: the driver is another process, which got the directory at its start.
    }

    [UpstreamTest("BR-008 should start and stop pipe server")]
    public async Task ShouldStartAndStopPipeServer(IBrowserType browserType, IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        var serverInfo = await browser.BindAsync("default", new());
        ExpectToEqual(serverInfo, ObjectContaining(new
        {
            endpoint = StringMatching(new Regex("browser-")),
        }));

        var browser2 = await browserType.ConnectAsync(serverInfo.Endpoint);
        var page = await browser2.NewPageAsync(newPageOptions);
        await page.GotoAsync("data:text/html,<h1>Hello via pipe</h1>");
        Assert.That(await page.Locator("h1").TextContentAsync(), Is.EqualTo("Hello via pipe"));
        await page.CloseAsync();
        await browser2.CloseAsync();
        await browser.UnbindAsync();
    }

    [UpstreamTest("BR-009 should write descriptor on start and remove on stop")]
    public async Task ShouldWriteDescriptorOnStartAndRemoveOnStop(IBrowser browser, string serverRegistry)
    {
        // .NET: { wsPath: 'test' } as any is not an option of BindAsync.
        var serverInfo = await browser.BindAsync("my-title", new());

        // .NET: it.info().outputPath('registry') is the directory of the serverRegistry fixture.
        var registryDir = serverRegistry;
        var fileName = Path.GetFileName(Directory.GetFileSystemEntries(registryDir)[0]);
        var file = Path.Combine(registryDir, fileName);

        var descriptor = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(file));
        Assert.That(descriptor.GetProperty("title").GetString(), Is.EqualTo("my-title"));
        Assert.That(descriptor.GetProperty("playwrightVersion").GetString(), Is.Not.Empty);
        Assert.That(descriptor.GetProperty("playwrightLib").GetString(), Is.Not.Empty);
        Assert.That(descriptor.GetProperty("browser").GetProperty("browserName").GetString(), Is.Not.Empty);
        Assert.That(descriptor.GetProperty("endpoint").GetString(), Is.EqualTo(serverInfo.Endpoint));

        if (!OperatingSystem.IsWindows())
        {
            Assert.That(File.Exists(serverInfo.Endpoint), Is.EqualTo(true));
        }

        await browser.UnbindAsync();
        Assert.That(File.Exists(file), Is.EqualTo(false));
        if (!OperatingSystem.IsWindows())
        {
            Assert.That(File.Exists(serverInfo.Endpoint), Is.EqualTo(false));
        }
    }

    [UpstreamTest("BR-010 should start ws server with host/port and produce well-formed endpoint")]
    public async Task ShouldStartWsServerWithHostPortAndProduceWellFormedEndpoint(IBrowserType browserType, IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        var serverInfo = await browser.BindAsync("default", new() { Host = "localhost", Port = 0 });
        Assert.That(serverInfo.Endpoint, Does.Match(@"^ws:\/\/(127\.0\.0\.1|\[::1\]):\d+\/[a-f0-9]+$"));

        var browser2 = await browserType.ConnectAsync(serverInfo.Endpoint);
        var page = await browser2.NewPageAsync(newPageOptions);
        await page.GotoAsync("data:text/html,<h1>Hello via ws</h1>");
        Assert.That(await page.Locator("h1").TextContentAsync(), Is.EqualTo("Hello via ws"));
        await page.CloseAsync();
        await browser2.CloseAsync();
        await browser.UnbindAsync();
    }
}
