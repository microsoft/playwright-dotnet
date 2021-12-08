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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
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

        [PlaywrightTest("browsertype-connect.spec.ts", "disconnected event should be emitted when browser is closed or server is closed")]
        public async Task DisconnectedEventShouldBeEmittedWhenBrowserIsClosedOrServerIsClosed()
        {
            var browserOne = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            int browserOneCloseCount = 0;
            browserOne.Disconnected += (_, e) => browserOneCloseCount++;
            await browserOne.CloseAsync();
            Assert.AreEqual(browserOneCloseCount, 1);
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

        [PlaywrightTest("browsertype-connect.spec.ts", "should handle exceptions during connect")]
        [Ignore("SKIP WIRE")]
        public void ShouldHandleExceptionsDuringConnect()
        {
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
            Assert.AreEqual("Connection closed (Browser closed)", exception.Message);
        }


        [PlaywrightTest("browsertype-connect.spec.ts", "should reject navigation when browser closes")]
        public async Task ShouldRejectNavigationWhenBrowserCloses()
        {
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var page = await browser.NewPageAsync();
            await browser.CloseAsync();

            Assert.AreEqual(browser.IsConnected, false);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await page.GotoAsync(Server.EmptyPage));
            Assert.AreEqual("Connection closed (Browser closed)", exception.Message);
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
        [Ignore("SKIP WIRE")]
        public void ShouldTerminateNetworkWaiters()
        {         
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should respect selectors")]
        [Ignore("SKIP WIRE")]
        public void ShouldRespectSelectors()
        {
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
