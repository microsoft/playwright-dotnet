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

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library.Chromium;

/// <summary>Port of tests/library/chromium/connect-over-cdp.spec.ts.</summary>
public class ConnectOverCdpTests
{
    // .NET: upstream runs the files of tests/library/chromium only in the Chromium project.
    [BeforeEach]
    public static void Skip(string browserName) => It.Skip(browserName != "chromium", "chromium only");

    [UpstreamTest("CD-001 should connect to an existing cdp session")]
    public async Task ShouldConnectToAnExistingCdpSession(IBrowserType browserType, CreateUserDataDir createUserDataDir, ChildProcess childProcess, TestInfo testInfo, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        var configHome = await createUserDataDir();
        var userDataDir = Path.Combine(configHome, "google-chrome");
        var browserServer = await browserType.LaunchPersistentContextAsync(userDataDir, new(launchPersistentContextOptions)
        {
            Args = ["--remote-debugging-port=0"],
        });
        try
        {
            var port = (await File.ReadAllTextAsync(Path.Combine(userDataDir, "DevToolsActivePort"))).Split('\n')[0];
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var contexts = cdpBrowser.Contexts;
            Assert.That(contexts.Count, Is.EqualTo(1));
            await cdpBrowser.CloseAsync();

            // .NET: process.platform.
            if (Env.Platform == "linux")
            {
                foreach (var env in new Dictionary<string, string>[]
                {
                    new() { ["CHROME_USER_DATA_DIR"] = "", ["CHROME_CONFIG_HOME"] = "", ["XDG_CONFIG_HOME"] = configHome },
                    new() { ["CHROME_USER_DATA_DIR"] = "", ["CHROME_CONFIG_HOME"] = configHome, ["XDG_CONFIG_HOME"] = testInfo.OutputPath("unused-config") },
                    new() { ["CHROME_USER_DATA_DIR"] = userDataDir, ["CHROME_CONFIG_HOME"] = testInfo.OutputPath("unused-config"), ["XDG_CONFIG_HOME"] = testInfo.OutputPath("unused-xdg-config") },
                })
                {
                    var child = childProcess(new()
                    {
                        // .NET: require.resolve('playwright-core') is the package of the driver.
                        Command = [NodeProcess.ExecPath, "-e", $@"
            const {{ chromium }} = require({JsonStringify(Path.GetDirectoryName(NodeProcess.CliPath))});
            (async () => {{
              const browser = await chromium.connectOverCDP('chrome');
              console.log('contexts: ' + browser.contexts().length);
              await browser.close();
            }})().catch(error => {{
              console.error(error);
              process.exitCode = 1;
            }});
          "],
                        Env = env,
                    });
                    await child.CleanExit();
                    Assert.That(child.Stdout, Does.Contain("contexts: 1"));
                }
            }
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-002 should connect when an existing page has no renderer")]
    public async Task ShouldConnectWhenAnExistingPageHasNoRenderer(IBrowserType browserType, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            // Prepare a page without a renderer, e.g. one discarded by the Memory Saver.
            var cdpBrowser1 = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var context1 = cdpBrowser1.Contexts[0];
            var healthyPage1 = await context1.NewPageAsync();
            await healthyPage1.GotoAsync(server.Prefix + "/title.html");
            var crashedPage1 = await context1.NewPageAsync();
            await crashedPage1.GotoAsync(server.EmptyPage);
            IgnoreErrors(crashedPage1.GotoAsync("chrome://crash"));
            await Once<IPage>(h => crashedPage1.Crash += h, h => crashedPage1.Crash -= h);
            await cdpBrowser1.CloseAsync();

            // Connecting again should not hang on the page without a renderer,
            // and should not report it at all.
            var cdpBrowser2 = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var pages = cdpBrowser2.Contexts[0].Pages;
            Assert.That(pages.Select(page => page.Url), Is.EqualTo(new[] { server.Prefix + "/title.html" }));
            Assert.That(await pages[0].TitleAsync(), Is.EqualTo("Woof-Woof"));
            await cdpBrowser2.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-003 should connect when the opener of an existing page has no renderer")]
    public async Task ShouldConnectWhenTheOpenerOfAnExistingPageHasNoRenderer(IBrowserType browserType, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            // The popup should be in a different process to survive the crash of the opener.
            Args = ["--remote-debugging-port=" + port, "--site-per-process"],
        });
        try
        {
            var cdpBrowser1 = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var crashedPage1 = await cdpBrowser1.Contexts[0].NewPageAsync();
            await crashedPage1.GotoAsync(server.EmptyPage);
            var (popup1, _) = await PromiseAll(
                crashedPage1.WaitForPopupAsync(),
                crashedPage1.EvaluateAsync("url => { window.open(url); }", server.CrossProcessPrefix + "/title.html"));
            await popup1.WaitForLoadStateAsync();
            IgnoreErrors(crashedPage1.GotoAsync("chrome://crash"));
            await Once<IPage>(h => crashedPage1.Crash += h, h => crashedPage1.Crash -= h);
            await cdpBrowser1.CloseAsync();

            // Connecting again should not report the opener, and should not wait for it.
            var cdpBrowser2 = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var pages = cdpBrowser2.Contexts[0].Pages;
            Assert.That(pages.Select(page => page.Url), Is.EqualTo(new[] { server.CrossProcessPrefix + "/title.html" }));
            Assert.That(await pages[0].TitleAsync(), Is.EqualTo("Woof-Woof"));
            Assert.That(await pages[0].OpenerAsync(), Is.Null);
            await cdpBrowser2.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-004 should connect when an existing page has been discarded")]
    public async Task ShouldConnectWhenAnExistingPageHasBeenDiscarded(IBrowserType browserType, CreateUserDataDir createUserDataDir, Server server, TestInfo testInfo, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var userDataDir = await createUserDataDir();
        // chrome://discards is an internal debug page that is only available with this pref.
        File.WriteAllText(Path.Combine(userDataDir, "Local State"), JsonStringify(new { internal_only_uis_enabled = true }));
        var context = await browserType.LaunchPersistentContextAsync(userDataDir, new(launchPersistentContextOptions)
        {
            Headless = false,
            // Emulating the viewport of a discarded tab crashes the browser in
            // WebContentsImpl::SetDeviceEmulationSize, because it has no view.
            ViewportSize = ViewportSize.NoViewport,
            Args =
            [
                "--remote-debugging-port=" + port,
                // Chrome refuses to discard tabs with DevTools attached, and we attach to all of them.
                "--enable-features=AllowDevtoolsConnectedDiscard",
            ],
        });
        try
        {
            var victimUrl = server.Prefix + "/title.html";
            var victim = await context.NewPageAsync();
            await victim.GotoAsync(victimUrl);
            var discards = await context.NewPageAsync();
            await discards.GotoAsync("chrome://discards/");
            // Discarding replaces the tab's WebContents, but the url in the table survives.
            var row = discards.GetByRole(AriaRole.Row, new() { Name = victimUrl });
            await row.GetByText("Urgent Discard").ClickAsync();
            await Expect(row).ToContainTextAsync("discarded");
            // The renderer process is shut down asynchronously after the discard.
            await discards.WaitForTimeoutAsync(3000);
            await testInfo.Attach("discards rows", new { body = string.Join("\n", (await discards.GetByRole(AriaRole.Row).AllInnerTextsAsync()).Select(text => Regex.Replace(text, "\\s+", " "))) });
            // .NET: await (await fetch(url)).json() is HttpClient.GetStringAsync(url) and a JsonElement.
            using var http = new HttpClient();
            var targets = JsonSerializer.Deserialize<JsonElement>(await http.GetStringAsync($"http://127.0.0.1:{port}/json/list"));
            await testInfo.Attach("targets", new { body = JsonSerializer.Serialize(targets, new JsonSerializerOptions { WriteIndented = true }), contentType = "application/json" });

            // Connecting should not hang on the discarded page, and the page should not be reported.
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var pages = cdpBrowser.Contexts[0].Pages;
            Assert.That(pages.Select(page => page.Url).Order(StringComparer.Ordinal), Is.EqualTo(new[] { "about:blank", discards.Url }.Order(StringComparer.Ordinal)));
            await cdpBrowser.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [UpstreamTest("CD-008 should connectOverCDP and manage downloads in default context")]
    public async Task ShouldConnectOverCDPAndManageDownloadsInDefaultContext(IBrowserType browserType, string mode, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        server.SetRoute("/downloadWithFilename", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/octet-stream");
            res.SetHeader("Content-Disposition", "attachment; filename=file.txt");
            res.End("Hello world");
        });

        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });

        try
        {
            var browser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var page = await browser.Contexts[0].NewPageAsync();
            await page.SetContentAsync($"<a href=\"{server.Prefix}/downloadWithFilename\">download</a>");

            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            Assert.That(download.Page, Is.SameAs(page));
            Assert.That(download.Url, Is.EqualTo($"{server.Prefix}/downloadWithFilename"));
            Assert.That(download.SuggestedFilename, Is.EqualTo("file.txt"));

            var userPath = testInfo.OutputPath("download.txt");
            await download.SaveAsAsync(userPath);
            Assert.That(File.Exists(userPath), Is.True);
            Assert.That(File.ReadAllText(userPath), Is.EqualTo("Hello world"));
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-009 should connect to an existing cdp session twice")]
    public async Task ShouldConnectToAnExistingCdpSessionTwice(IBrowserType browserType, string mode, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var cdpBrowser1 = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var cdpBrowser2 = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var contexts1 = cdpBrowser1.Contexts;
            Assert.That(contexts1.Count, Is.EqualTo(1));
            var context1 = contexts1[0];
            var page1 = await context1.NewPageAsync();
            await page1.GotoAsync(server.EmptyPage);

            var contexts2 = cdpBrowser2.Contexts;
            Assert.That(contexts2.Count, Is.EqualTo(1));
            var context2 = contexts2[0];
            var page2 = await context2.NewPageAsync();
            await page2.GotoAsync(server.EmptyPage);

            Assert.That(context1.Pages.Count, Is.EqualTo(2));
            Assert.That(context2.Pages.Count, Is.EqualTo(2));

            await page1.EvaluateAsync("() => alert('dialog race')");
            Assert.That(await context2.Pages[0].TitleAsync(), Is.EqualTo(""));

            await cdpBrowser1.CloseAsync();
            await cdpBrowser2.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-010 should connect to existing page with iframe and navigate")]
    public async Task ShouldConnectToExistingPageWithIframeAndNavigate(IBrowserType browserType, string mode, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            {
                var context1 = await browserServer.NewContextAsync(contextOptions);
                var page = await context1.NewPageAsync();
                await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
            }
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var contexts = cdpBrowser.Contexts;
            Assert.That(contexts.Count, Is.EqualTo(1));
            await contexts[0].Pages[0].GotoAsync(server.EmptyPage);
            await cdpBrowser.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-012 should connect over a ws endpoint")]
    public async Task ShouldConnectOverAWsEndpoint(IBrowserType browserType, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            // .NET: http.get(url) that collects the data of the response is HttpClient.GetStringAsync(url).
            using var http = new HttpClient();
            var json = await http.GetStringAsync($"http://127.0.0.1:{port}/json/version/");
            var cdpBrowser = await browserType.ConnectOverCDPAsync(JsonSerializer.Deserialize<JsonElement>(json).GetProperty("webSocketDebuggerUrl").GetString());
            var contexts = cdpBrowser.Contexts;
            Assert.That(contexts.Count, Is.EqualTo(1));
            await cdpBrowser.CloseAsync();

            // also connect with the deprecated wsEndpoint option
            // .NET: the wsEndpoint option is not in the .NET API, the endpoint is always the first argument:
            // const cdpBrowser2 = await browserType.connectOverCDP({ wsEndpoint: JSON.parse(json).webSocketDebuggerUrl });
            // const contexts2 = cdpBrowser2.contexts();
            // expect(contexts2.length).toBe(1);
            // await cdpBrowser2.close();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-013 should send extra headers with connect request")]
    public async Task ShouldSendExtraHeadersWithConnectRequest(IBrowserType browserType, Server server, TestInfo testInfo)
    {
        {
            // .NET: the wsEndpoint option is the endpointURL argument.
            var (request, _) = await PromiseAll(
                server.WaitForWebSocketConnectionRequest(),
                Catch(() => browserType.ConnectOverCDPAsync($"ws://localhost:{server.Port}/ws", new()
                {
                    Headers = new Dictionary<string, string>
                    {
                        ["User-Agent"] = "Playwright",
                        ["foo"] = "bar",
                    },
                    Timeout = 100,
                })));
            Assert.That(request.Headers["user-agent"], Is.EqualTo("Playwright"));
            Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
        }
        {
            var (request, _) = await PromiseAll(
                server.WaitForRequest("/json/version/"),
                Catch(() => browserType.ConnectOverCDPAsync($"http://localhost:{server.Port}", new()
                {
                    Headers = new Dictionary<string, string>
                    {
                        ["User-Agent"] = "Playwright",
                        ["foo"] = "bar",
                    },
                    Timeout = 100,
                })));
            Assert.That(request.Headers["user-agent"], Is.EqualTo("Playwright"));
            Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
        }
    }

    [UpstreamTest("CD-014 should keep URL parameters when adding json/version")]
    public async Task ShouldKeepURLParametersWhenAddingJsonVersion(IBrowserType browserType, Server server)
    {
        await PromiseAll(
            server.WaitForRequest("/browser/json/version/?foo=bar"),
            Catch(() => browserType.ConnectOverCDPAsync($"http://localhost:{server.Port}/browser/?foo=bar")));
    }

    [UpstreamTest("CD-015 should append /json/version with a slash if there isnt one")]
    public async Task ShouldAppendJsonVersionWithASlashIfThereIsntOne(IBrowserType browserType, Server server)
    {
        await PromiseAll(
            server.WaitForRequest("/browser/json/version/?foo=bar"),
            Catch(() => browserType.ConnectOverCDPAsync($"http://localhost:{server.Port}/browser?foo=bar")));
    }

    [UpstreamTest("CD-017 should report all pages in an existing browser")]
    public async Task ShouldReportAllPagesInAnExistingBrowser(IBrowserType browserType, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var contexts = cdpBrowser.Contexts;
            Assert.That(contexts.Count, Is.EqualTo(1));
            for (var i = 0; i < 3; i++)
            {
                await contexts[0].NewPageAsync();
            }
            await cdpBrowser.CloseAsync();

            var cdpBrowser2 = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            Assert.That(cdpBrowser2.Contexts[0].Pages.Count, Is.EqualTo(3));

            await cdpBrowser2.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-019 should return valid browser from context.browser()")]
    public async Task ShouldReturnValidBrowserFromContextBrowser(IBrowserType browserType, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var contexts = cdpBrowser.Contexts;
            Assert.That(contexts.Count, Is.EqualTo(1));
            Assert.That(contexts[0].Browser, Is.SameAs(cdpBrowser));

            var context2 = await cdpBrowser.NewContextAsync(contextOptions);
            Assert.That(context2.Browser, Is.SameAs(cdpBrowser));

            await cdpBrowser.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-020 should report an expected error when the endpointURL returns a non-expected status code")]
    [Fixme("dotnet: the message is 'Unexpected status 404 when connecting to <server.PREFIX>/json/version/.', without the 'browserType.connectOverCDP: ' prefix of the call")]
    public async Task ShouldReportAnExpectedErrorWhenTheEndpointURLReturnsANonExpectedStatusCode(IBrowserType browserType, Server server)
    {
        server.SetRoute("/json/version/", (req, resp) =>
        {
            resp.StatusCode = 404;
            resp.End(JsonStringify(new
            {
                webSocketDebuggerUrl = "dont-use-me",
            }));
        });
        var error = await ThrowsAsync<PlaywrightException>(() => browserType.ConnectOverCDPAsync(server.Prefix));
        Assert.That(error.Message, Does.Contain($"browserType.connectOverCDP: Unexpected status 404 when connecting to {server.Prefix}/json/version/"));
    }

    [UpstreamTest("CD-021 should report an expected error when the endpoint URL JSON webSocketDebuggerUrl is undefined")]
    [Fixme("dotnet: the message is 'Invalid URL: undefined', without the 'browserType.connectOverCDP: ' prefix of the call")]
    public async Task ShouldReportAnExpectedErrorWhenTheEndpointURLJSONWebSocketDebuggerUrlIsUndefined(IBrowserType browserType, Server server)
    {
        server.SetRoute("/json/version/", (req, resp) =>
        {
            // .NET: JSON.stringify leaves out a property that is undefined.
            resp.End(JsonStringify(new
            {
            }));
        });
        var error = await ThrowsAsync<PlaywrightException>(() => browserType.ConnectOverCDPAsync(server.Prefix));
        Assert.That(error.Message, Does.Contain("browserType.connectOverCDP: Invalid URL"));
    }

    [UpstreamTest("CD-022 should connect to an existing cdp session when passed as a first argument")]
    public async Task ShouldConnectToAnExistingCdpSessionWhenPassedAsAFirstArgument(IBrowserType browserType, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var contexts = cdpBrowser.Contexts;
            Assert.That(contexts.Count, Is.EqualTo(1));
            await cdpBrowser.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-023 should use proxy with connectOverCDP")]
    public async Task ShouldUseProxyWithConnectOverCDP(IBrowserType browserType, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the proxy</title></html>");
        });
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var context = await cdpBrowser.NewContextAsync(new(contextOptions)
            {
                Proxy = new() { Server = $"localhost:{server.Port}" },
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync("http://non-existent.test/target.html");
            Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
            await cdpBrowser.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-026 should be able to connect via localhost")]
    public async Task ShouldBeAbleToConnectViaLocalhost(IBrowserType browserType, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://localhost:{port}");
            var contexts = cdpBrowser.Contexts;
            Assert.That(contexts.Count, Is.EqualTo(1));
            await cdpBrowser.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-027 emulate media should not be affected by second connectOverCDP with noDefaults")]
    public async Task EmulateMediaShouldNotBeAffectedBySecondConnectOverCDPWithNoDefaults(IBrowserType browserType, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            async Task<bool> isPrint(IPage page)
            {
                return await page.EvaluateAsync<bool>("() => matchMedia('print').matches");
            }

            var browser1 = await browserType.ConnectOverCDPAsync($"http://localhost:{port}");
            var context1 = await browser1.NewContextAsync(contextOptions);
            var page1 = await context1.NewPageAsync();
            await page1.EmulateMediaAsync(new() { Media = Media.Print });
            Assert.That(await isPrint(page1), Is.EqualTo(true));
            var browser2 = await browserType.ConnectOverCDPAsync($"http://localhost:{port}", new() { NoDefaults = true });
            Assert.That(await isPrint(page1), Is.EqualTo(true));
            await PromiseAll(
                browser1.CloseAsync(),
                browser2.CloseAsync());
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-028 should allow tracing over cdp session")]
    public async Task ShouldAllowTracingOverCdpSession(IBrowserType browserType, string trace, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        It.Skip(trace == "on");

        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var context = cdpBrowser.Contexts[0];
            await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => 2 + 2");
            var traceZip = testInfo.OutputPath("trace.zip");
            await context.Tracing.StopAsync(new() { Path = traceZip });
            await cdpBrowser.CloseAsync();
            Assert.That(File.Exists(traceZip), Is.EqualTo(true));
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-029 setInputFiles should preserve lastModified timestamp")]
    public async Task SetInputFilesShouldPreserveLastModifiedTimestamp(IBrowserType browserType, Func<string, string> asset, BrowserTypeLaunchOptions launchOptions)
    {
        var port = 9339 + It.Info().WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var context = cdpBrowser.Contexts[0];
            var page = await context.NewPageAsync();
            await page.SetContentAsync("<input type=file multiple=true/>");
            var input = page.Locator("input");
            var files = new[] { "file-to-upload.txt", "file-to-upload-2.txt" };
            await input.SetInputFilesAsync(files.Select(f => asset(f)));
            Assert.That(await input.EvaluateAsync<string[]>("e => [...e.files].map(f => f.name)"), Is.EqualTo(files));
            var timestamps = await input.EvaluateAsync<double[]>("e => [...e.files].map(f => f.lastModified)");
            // .NET: fs.statSync(file).mtimeMs is the last write time in milliseconds since the Unix epoch.
            var expectedTimestamps = files.Select(file => Math.Round((File.GetLastWriteTimeUtc(asset(file)) - DateTime.UnixEpoch).TotalMilliseconds, MidpointRounding.AwayFromZero)).ToArray();
            // On Linux browser sometimes reduces the timestamp by 1ms: 1696272058110.0715  -> 1696272058109 or even
            // rounds it to seconds in WebKit: 1696272058110 -> 1696272058000.
            for (var i = 0; i < timestamps.Length; i++)
            {
                Assert.That(Math.Abs(timestamps[i] - expectedTimestamps[i]), Is.LessThanOrEqualTo(1000), $"expected: {string.Join(",", expectedTimestamps)}; actual: {string.Join(",", timestamps)}");
            }
            await cdpBrowser.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-031 should print custom ws close error")]
    public async Task ShouldPrintCustomWsCloseError(IBrowserType browserType, Server server)
    {
        server.OnceWebSocketConnection((ws, request) =>
        {
            ws.OnMessage(message =>
            {
                ws.Close(4123, "Oh my!");
            });
        });
        var error = await Catch(() => browserType.ConnectOverCDPAsync($"ws://localhost:{server.Port}/ws"));
        Assert.That(error.Message, Does.Contain("Browser logs:\n\nOh my!\n"));
    }

    [UpstreamTest("CD-033 should get title and URL of existing page")]
    public async Task ShouldGetTitleAndURLOfExistingPage(IBrowserType browserType, string mode, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        var browsers = new List<IBrowser>();
        try
        {
            {
                var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
                browsers.Add(cdpBrowser);
                var context = cdpBrowser.Contexts[0];
                var page = await context.NewPageAsync();
                await page.GotoAsync(server.EmptyPage);
                await page.EvaluateAsync("() => document.title = 'my title'");
            }

            {
                var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
                browsers.Add(cdpBrowser);

                var context = cdpBrowser.Contexts[0];
                var page = context.Pages[0];
                Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
                Assert.That(await page.TitleAsync(), Is.EqualTo("my title"));
            }
        }
        finally
        {
            foreach (var browser in browsers)
            {
                await browser.CloseAsync();
            }
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-034 should skip default overrides with noDefaults")]
    public async Task ShouldSkipDefaultOverridesWithNoDefaults(IBrowserType browserType, string mode, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var browser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/", new()
            {
                NoDefaults = true,
            });
            var defaultContext = browser.Contexts[0];
            var page = await defaultContext.NewPageAsync();

            // Browser.setDownloadBehavior was not sent, so no Playwright download event fires.
            server.SetRoute("/download", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.SetHeader("Content-Disposition", "attachment; filename=file.txt");
                res.End("Hello world");
            });
            await page.SetContentAsync($"<a href=\"{server.Prefix}/download\">download</a>");
            var sawDownload = false;
            page.Download += (_, _) => { sawDownload = true; };
            await page.ClickAsync("a");
            await page.WaitForTimeoutAsync(500);
            Assert.That(sawDownload, Is.EqualTo(false));

            await browser.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-035 noDefaults should not affect new contexts")]
    public async Task NoDefaultsShouldNotAffectNewContexts(IBrowserType browserType, string mode, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browserServer = await browserType.LaunchAsync(new(launchOptions)
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var browser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/", new()
            {
                NoDefaults = true,
            });

            // New contexts should still get normal Playwright defaults.
            var newContext = await browser.NewContextAsync(contextOptions);
            var page = await newContext.NewPageAsync();
            var hasFocus = await page.EvaluateAsync<bool>("() => document.hasFocus()");
            Assert.That(hasFocus, Is.EqualTo(true));

            await newContext.CloseAsync();
            await browser.CloseAsync();
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("CD-053 should connect when another client inspects a terminated shared worker")]
    [ItFixme]
    public async Task ShouldConnectWhenAnotherClientInspectsATerminatedSharedWorker(IBrowserType browserType, Server server, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        var port = 9339 + testInfo.WorkerIndex;
        var browser = await browserType.LaunchAsync(new(launchOptions) { Args = ["--remote-debugging-port=" + port] });
        try
        {
            // Another client inspects the shared worker.
            var page = await browser.NewPageAsync(newPageOptions);
            var inspector = await browser.NewBrowserCDPSessionAsync();
            var workerCreated = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            inspector.Event("Target.targetCreated").OnEvent += (_, @event) =>
            {
                if (@event.Value.GetProperty("targetInfo").GetProperty("type").GetString() == "shared_worker")
                {
                    workerCreated.TrySetResult(@event.Value.GetProperty("targetInfo").GetProperty("targetId").GetString());
                }
            };
            await inspector.SendAsync("Target.setDiscoverTargets", new Dictionary<string, object> { ["discover"] = true });
            await page.GotoAsync(server.Prefix + "/shared-worker/shared-worker.html");
            Assert.That(await page.EvaluateAsync<string>("window.sharedWorkerResponsePromise"), Is.EqualTo("echo:hello"));
            var workerTargetId = await workerCreated.Task;
            await inspector.SendAsync("Target.attachToTarget", new Dictionary<string, object> { ["targetId"] = workerTargetId, ["flatten"] = true });

            // The last client of the shared worker goes away, so the worker is terminated.
            // The inspector stays attached, so the worker target lingers without a browser context.
            await page.CloseAsync();
            await Expect(async () =>
            {
                var targetInfos = (await inspector.SendAsync("Target.getTargets")).Value.GetProperty("targetInfos");
                Assert.That(targetInfos.EnumerateArray().Any(info => info.GetProperty("targetId").GetString() == workerTargetId), Is.EqualTo(false));
            }).ToPassAsync();

            // A new client auto-attaches to the lingering worker target.
            var cdpBrowser = await browserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var page2 = await cdpBrowser.Contexts[0].NewPageAsync();
            Assert.That(await page2.EvaluateAsync<int>("1 + 1"), Is.EqualTo(2));
            await cdpBrowser.CloseAsync();
        }
        finally
        {
            await browser.CloseAsync();
        }
    }
}
