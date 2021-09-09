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
            var browser = await BrowserType.ConnectAsync(_browserServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(Server.EmptyPage, page.Url);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to connect two browsers at the same time")]
        [Ignore("SKIP WIRE")]
        public void ShouldBeAbleToConnectTwoBrowsersAtTheSameTime()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "disconnected event should be emitted when browser is closed or server is closed")]
        [Ignore("SKIP WIRE")]
        public void DisconnectedEventShouldBeEmittedWhenBrowserIsClosedOrServerIsClosed()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should handle exceptions during connect")]
        [Ignore("SKIP WIRE")]
        public void ShouldHandleExceptionsDuringConnect()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should set the browser connected state")]
        [Ignore("SKIP WIRE")]
        public void ShouldSetTheBrowserConnectedState()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should throw when used after isConnected returns false")]
        [Ignore("SKIP WIRE")]
        public void ShouldThrowWhenUsedAfterIsConnectedReturnsFalse()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should reject navigation when browser closes")]
        [Ignore("SKIP WIRE")]
        public void ShouldRejectNavigationWhenBrowserCloses()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should reject waitForSelector when browser closes")]
        [Ignore("SKIP WIRE")]
        public void ShouldRejectWaitForSelectorWhenBrowserCloses()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should emit close events on pages and contexts")]
        [Ignore("SKIP WIRE")]
        public void ShouldEmitCloseEventsOnPagesAndContexts()
        {
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
                platformId = RuntimeInformation.OSArchitecture == Architecture.X64 ? "win32_x64" : "win";
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
