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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnit;
using Microsoft.Playwright.Tests.TestServer;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
    public class BrowserTypeConnectTests : PlaywrightTestEx
    {
        private BrowserServer _browserServer;

        [SetUp]
        public void SetUpAsync()
        {
            try
            {
                DirectoryInfo assemblyDirectory = new(AppContext.BaseDirectory);
                BrowserServer browserServer = new();
                browserServer.Process = new()
                {
                    StartInfo =
                    {
                        FileName = GetExecutablePath(),
                        Arguments = $"launch-server {BrowserType.Name}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        RedirectStandardError = false,
                        CreateNoWindow = true,
                    },
                };
                browserServer.Process.Start();
                browserServer.WSEndpoint = browserServer.Process.StandardOutput.ReadLine();

                if (browserServer.WSEndpoint != null && !browserServer.WSEndpoint.StartsWith("ws://"))
                {
                    throw new PlaywrightException("Invalid web socket address: " + browserServer.WSEndpoint);
                }
                _browserServer = browserServer;
            }
            catch (IOException ex)
            {
                throw new PlaywrightException("Failed to launch server", ex);
            }
        }

        [TearDown]
        public void TearDown()
        {
            _browserServer.Process.Kill(true);
            _browserServer = null;
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to reconnect to a browser")]
        public async Task ShouldBeAbleToConnectToBrowserAsync()
        {
            {
                var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
                var context = await browser.NewContextAsync();
                var page = await context.NewPageAsync();
                var result = await page.EvaluateAsync("11 * 11");
                Assert.AreEqual(result.ToString(), "121");
                await browser.CloseAsync();
            }
            {
                var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
                var context = await browser.NewContextAsync();
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                Assert.AreEqual(Server.EmptyPage, page.Url);
                await browser.CloseAsync();
            }
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to connect two browsers at the same time")]
        public async Task ShouldBeAbleToConnectTwoBrowsersAtTheSameTime()
        {
            var browser1 = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            Assert.AreEqual(browser1.Contexts.Count, 0);
            await browser1.NewContextAsync();
            Assert.AreEqual(browser1.Contexts.Count, 1);

            var browser2 = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            Assert.AreEqual(browser2.Contexts.Count, 0);
            var context2 = await browser2.NewContextAsync();
            Assert.AreEqual(browser1.Contexts.Count, 1);
            Assert.AreEqual(browser2.Contexts.Count, 1);

            await browser1.CloseAsync();
            Assert.AreEqual(browser2.Contexts.Count, 1);

            var page2 = await context2.NewPageAsync();
            var result = await page2.EvaluateAsync("11 * 11");
            Assert.AreEqual(result.ToString(), "121");
            await browser2.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should timeout in connect while connecting")]
        public async Task ShouldTimeoutInConnectWhileConnecting()
        {
            string WsEndpoint = "ws://localhost:" + Server.Port.ToString() + "/ws";
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await BrowserType.ConnectAsync(WsEndpoint, new BrowserTypeConnectOptions { Timeout = 100 }));
            StringAssert.Contains("Timeout 100ms exceeded", exception.Message);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should send extra headers with connect request")]
        public async Task ShouldSendExtraHeadersWithConnect()
        {
            // Issue : Semphore disposed during server connection
            int port = Server.Port + 100;
            var disposableServer = new SimpleServer(port, TestUtils.FindParentDirectory("Playwright.Tests.TestServer"), false);
            await disposableServer.StartAsync();

            string WsEndpoint = "ws://localhost:" + port.ToString() + "/ws";
            //string WsEndpoint = "ws://localhost:" + Server.Port.ToString() + "/ws";
            await BrowserType.ConnectAsync(WsEndpoint, new BrowserTypeConnectOptions {
                Headers = new Dictionary<string, string>
                {
                    ["User-Agent"] = "Playwright",
                    ["foo"] = "bar",
                }
            });
            Assert.NotNull(Server.LastRequest);
            Assert.Equals("Playwright", Server.LastRequest.Headers["User-Agent"]);
            Assert.Equals("bar", Server.LastRequest.Headers["foo"]);
            await disposableServer.StopAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should send default headers with connect request")]
        public async Task ShouldSendDefaultUserAgentHeaderWithConnect()
        {
            // Issue : Semphore disposed during server connection
            string WsEndpoint = "ws://localhost:" + Server.Port.ToString() + "/ws";
            await BrowserType.ConnectAsync(WsEndpoint, new BrowserTypeConnectOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                }
            });
            Assert.NotNull(Server.LastRequest);
            Assert.Equals("bar", Server.LastRequest.Headers["foo"]);
            StringAssert.Contains("Playwright", Server.LastRequest.Headers["user-agent"]);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should support slowmo option")]
        public async Task ShouldSupportSlowMo()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint, new BrowserTypeConnectOptions { SlowMo = 200 });
            var context = await browser.NewContextAsync();
            var time1 = DateTime.Now;
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            var time2 = DateTime.Now;
            Assert.AreEqual(Server.EmptyPage, page.Url);
            Assert.Greater((time2 - time1).TotalMilliseconds, 200);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "disconnected event should be emitted when browser is closed or server is closed")]
        public async Task DisconnectedEventShouldBeEmittedWhenBrowserIsClosedOrServerIsClosed()
        {
            var browserOne = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var pageOne = browserOne.NewPageAsync();

            var browserTwo = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var pageTwo = browserTwo.NewPageAsync();

            int browserOneCloseCount = 0;
            int browserTwoCloseCount = 0;
            browserOne.Disconnected += (_, e) => browserOneCloseCount++;
            browserTwo.Disconnected += (_, e) => browserTwoCloseCount++;

            await browserOne.CloseAsync();
            Assert.AreEqual(browserOneCloseCount, 1);
            Assert.AreEqual(browserTwoCloseCount, 0);

            await browserTwo.CloseAsync();
            Assert.AreEqual(browserOneCloseCount, 1);
            Assert.AreEqual(browserTwoCloseCount, 1);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "disconnected event should have browser as argument")]
        public async Task DisconnectedEventShouldHaveBrowserAsArguments()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            IBrowser disconneced = null;
            browser.Disconnected += (_, e) => disconneced = e;
            await browser.CloseAsync();
            Assert.AreEqual(browser, disconneced);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should set the browser connected state")]
        public async Task ShouldSetTheBrowserConnectedState()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            Assert.AreEqual(browser.IsConnected, true);
            await browser.CloseAsync();
            Assert.AreEqual(browser.IsConnected, false);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should throw when used after isConnected returns false")]
        public async Task ShouldThrowWhenUsedAfterIsConnectedReturnsFalse()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var page = await browser.NewPageAsync();
            await browser.CloseAsync();
            Assert.AreEqual(browser.IsConnected, false);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await page.EvaluateAsync("1 + 1"));
            StringAssert.Contains("Connection closed (Browser closed)", exception.Message);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should throw when calling waitForNavigation after disconnect")]
        public async Task ShouldThrowWhenWhenCallingWaitForNavigationAfterDisconnect()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var page = await browser.NewPageAsync();
            await browser.CloseAsync();

            Assert.AreEqual(browser.IsConnected, false);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await page.WaitForNavigationAsync());
            StringAssert.Contains("Page closed", exception.Message);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should reject navigation when browser closes")]
        public async Task ShouldRejectNavigationWhenBrowserCloses()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var page = await browser.NewPageAsync();
            await browser.CloseAsync();

            Assert.AreEqual(browser.IsConnected, false);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await page.GotoAsync(Server.EmptyPage));
            StringAssert.Contains("Connection closed (Browser closed)", exception.Message);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should reject waitForSelector when browser closes")]
        public async Task ShouldRejectWaitForSelectorWhenBrowserCloses()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var watchdog = page.WaitForSelectorAsync("div");
            await browser.CloseAsync();

            Assert.AreEqual(browser.IsConnected, false);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await watchdog);
            Assert.That(exception.Message, Contains.Substring("Target closed"));
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should emit close events on pages and contexts")]
        public async Task ShouldEmitCloseEventsOnPagesAndContexts()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            bool pageClosed = false, contextClosed = false;
            page.Close += (_, e) => pageClosed = true;
            context.Close += (_, e) => contextClosed = true;

            await browser.CloseAsync();
            Assert.AreEqual(pageClosed, true);
            Assert.AreEqual(contextClosed, true);
            Assert.AreEqual(browser.IsConnected, false);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should terminate network waiters")]
        public async Task ShouldTerminateNetworkWaiters()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var task = TaskUtils.WhenAll(page.WaitForRequestAsync(Server.EmptyPage), page.WaitForResponseAsync(Server.EmptyPage), browser.CloseAsync());
            try
            {
                await task;
            }
            catch(Exception)
            {
                foreach(var exception in task.Exception.InnerExceptions)
                {
                    Console.WriteLine(exception.Message);
                    StringAssert.Contains("Page closed", exception.Message);
                }
            }
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should respect selectors")]
        public async Task ShouldRespectSelectors()
        {
            // Issue: Doesn't register the selectors

            string mycss = @"
            ({
                query(root, selector) {
                    return root.querySelector(selector);
                },
                queryAll(root, selector) {
                    return Array.from(root.querySelectorAll(selector));
                }
            })";
            // Register one engine before connecting.
            //string mycss = "{\n" +
            //      "    query(root, selector) {\n" +
            //      "      return root.querySelector(selector);\n" +
            //      "    },\n" +
            //      "    queryAll(root, selector) {\n" +
            //      "      return Array.from(root.querySelectorAll(selector));\n" +
            //      "    }\n" +
            //      "  }";
            await Playwright.Selectors.RegisterAsync("mycss1", new() { Script = mycss });

            var browser1 = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            //var browser1 = await BrowserType.LaunchAsync();
            var context1 = await browser1.NewContextAsync();

            await Playwright.Selectors.RegisterAsync("mycss2", new() { Script = mycss });

            var page1 = await context1.NewPageAsync();
            await page1.SetContentAsync("<div>hello</div>");
            Assert.AreEqual("hello", await page1.InnerHTMLAsync("css=div"));
            Assert.AreEqual("hello", await page1.InnerHTMLAsync("mycss1=div"));
            Assert.AreEqual("hello", await page1.InnerHTMLAsync("mycss2=div"));

            var browser2 = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            await Playwright.Selectors.RegisterAsync("mycss3", new() { Script = mycss });
            var page2 = await browser2.NewPageAsync();
            await page2.SetContentAsync("<div>hello</div>");

            Assert.AreEqual("hello", await page2.InnerHTMLAsync("css=div"));
            Assert.AreEqual("hello", await page2.InnerHTMLAsync("mycss1=div"));
            Assert.AreEqual("hello", await page2.InnerHTMLAsync("mycss2=div"));
            Assert.AreEqual("hello", await page2.InnerHTMLAsync("mycss3=div"));

            await browser1.CloseAsync();
            await browser2.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should not throw on close after disconnect")]
        public async Task ShouldNotThrowOnCloseAfterDisconnect()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var page = await browser.NewPageAsync();

            _browserServer.Process.Kill();
            await browser.CloseAsync();
            while (browser.IsConnected)
            {
                try
                {
                    await page.WaitForTimeoutAsync(100);
                }
                catch (Exception)
                {

                }
            }

            await browser.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should not throw on context.close after disconnect")]
        public async Task ShouldNotThrowOnContextCloseAfterDisconnect()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            _browserServer.Process.Kill();
            await browser.CloseAsync();
            while (browser.IsConnected)
            {
                try
                {
                    await page.WaitForTimeoutAsync(100);
                }
                catch (Exception)
                {

                }
            }
            await context.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should not throw on page.close after disconnect")]
        public async Task ShouldNotThrowOnPageCloseAfterDisconnect()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            _browserServer.Process.Kill();
            await browser.CloseAsync();
            while (browser.IsConnected)
            {
                try
                {
                    await page.WaitForTimeoutAsync(100);
                }
                catch (Exception)
                {

                }
            }
            await page.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should saveAs videos from remote browser")]
        public async Task ShouldSaveAsVideosFromRemoteBrowser()
        {
            using var tempDirectory = new TempDirectory();
            var videoPath = tempDirectory.Path;
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var context = await browser.NewContextAsync(new()
            {
                RecordVideoDir = videoPath,
                RecordVideoSize = new() { Height = 100, Width = 100 }
            });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await Task.Delay(1000);
            await context.CloseAsync();

            var videoSavePath = tempDirectory.Path + "my-video.webm";
            await page.Video.SaveAsAsync(videoSavePath);
            Assert.That(videoSavePath, Does.Exist);

            var exception = await PlaywrightAssert.ThrowsAsync<Exception>(async () => await page.Video.PathAsync());
            Assert.NotNull(exception);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to connect 20 times to a single server without warnings")]
        public async Task ShouldConnectTwentyTimesToASingleServerWithoutWarnings()
        {
            List<IBrowser> browsers = new List<IBrowser>();
            for (int i = 0; i < 20; i++)
            {
                browsers.Add(await BrowserType.ConnectAsync(_browserServer.WSEndpoint));
            }

            Assert.DoesNotThrowAsync(async() => {
                foreach(var browser in browsers)
                {
                    await browser.CloseAsync();
                }
            });
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should save download")]
        public async Task ShouldSaveDownload()
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                context.Response.Headers["Content-Disposition"] = "attachment;";
                return context.Response.WriteAsync("Hello world");
            });

            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var page = await browser.NewPageAsync(new() { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var downloadTask = page.WaitForDownloadAsync();

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "these", "are", "directories", "download.txt");
            var download = downloadTask.Result;
            await download.SaveAsAsync(userPath);
            Assert.True(new FileInfo(userPath).Exists);
            Assert.AreEqual("Hello world", File.ReadAllText(userPath));
            await page.CloseAsync();
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => download.PathAsync());
            StringAssert.Contains("Target page, context or browser has been closed", exception.Message);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should error when saving download after deletion")]
        public async Task ShouldErrorWhenSavingDownloadAfterDeletion()
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                context.Response.Headers["Content-Disposition"] = "attachment; filename=file.txt";
                return context.Response.WriteAsync("Hello world");
            });

            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var page = await browser.NewPageAsync(new() { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var downloadTask = page.WaitForDownloadAsync();

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "download.txt");
            var download = downloadTask.Result;
            await download.DeleteAsync();
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => download.SaveAsAsync(userPath));
            StringAssert.Contains("Target page, context or browser has been closed", exception.Message);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to connect when the wsEndpont is passed as the first argument")]
        public async Task ShouldConnectWhenWsEndpointIsPassedAsFirstArgument()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var result = await page.EvaluateAsync("1 + 2");
            Assert.AreEqual(result.ToString(), "3");
            await browser.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should save har")]
        public async Task ShouldSaveHar()
        {
            // Issue: Doesn't save the har : https://github.com/microsoft/playwright-dotnet/pull/1863/files

            using var tempDirectory = new TempDirectory();
            var harPath = tempDirectory.Path;
            //var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var browser = await BrowserType.LaunchAsync();
            var context = await browser.NewContextAsync(new()
            {
                RecordHarPath = harPath
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            await context.CloseAsync();
            await browser.CloseAsync();


            //Assert.That(harPath, Does.Exist);
            var logString = System.IO.File.ReadAllText(harPath);
            dynamic logJson = Newtonsoft.Json.JsonConvert.DeserializeObject(logString);
            var log = logJson.log;

            Assert.AreEqual(log.entries, 1);
            var entry = log.entries[0];
            Assert.AreEqual(entry.pageref, log.pages[0].id);
            Assert.AreEqual(entry.request.url, Server.EmptyPage);
        }

        private class BrowserServer
        {
            public Process Process { get; set; }
            public string WSEndpoint { get; set; }
        }

        private static string GetExecutablePath()
        {
            DirectoryInfo assemblyDirectory = new(AppContext.BaseDirectory);
            if (!assemblyDirectory.Exists || !File.Exists(Path.Combine(assemblyDirectory.FullName, "Microsoft.Playwright.dll")))
            {
                var assemblyLocation = typeof(Playwright).Assembly.Location;
                assemblyDirectory = new FileInfo(assemblyLocation).Directory;
            }

            string executableFile = GetPath(assemblyDirectory.FullName);
            if (File.Exists(executableFile))
            {
                return executableFile;
            }

            // if the above fails, we can assume we're in the nuget registry
            executableFile = GetPath(assemblyDirectory.Parent.Parent.FullName);
            if (File.Exists(executableFile))
            {
                return executableFile;
            }

            throw new PlaywrightException($"Driver not found: {executableFile}");
        }

        private static string GetPath(string driversPath)
        {
            string platformId;
            string runnerName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platformId = "win32_x64";
                runnerName = "playwright.cmd";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                runnerName = "playwright.sh";
                platformId = "mac";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                runnerName = "playwright.sh";
                platformId = "linux";
            }
            else
            {
                throw new PlaywrightException("Unknown platform");
            }

            return Path.Combine(driversPath, ".playwright", "node", platformId, runnerName);
        }
    }
}
