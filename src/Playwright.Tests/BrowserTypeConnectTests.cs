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
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class BrowserTypeConnectTests : PlaywrightTestEx
    {
        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to reconnect to a browser")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToReconnectToABrowserAsync()
        {
            var _browserServer = launchServer(BrowserType);
            var browser = await BrowserType.ConnectAsync(_browserServer.wsEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync("https://google.com");
            Assert.AreEqual("https://www.google.com/", page.Url);
            _browserServer.process.Kill();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to connect two browsers at the same time")]
        [Test, Ignore("SKIP WIRE")]
        public void ShouldBeAbleToConnectTwoBrowsersAtTheSameTime()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "disconnected event should be emitted when browser is closed or server is closed")]
        [Test, Ignore("SKIP WIRE")]
        public void DisconnectedEventShouldBeEmittedWhenBrowserIsClosedOrServerIsClosed()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should handle exceptions during connect")]
        [Test, Ignore("SKIP WIRE")]
        public void ShouldHandleExceptionsDuringConnect()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should set the browser connected state")]
        [Test, Ignore("SKIP WIRE")]
        public void ShouldSetTheBrowserConnectedState()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should throw when used after isConnected returns false")]
        [Test, Ignore("SKIP WIRE")]
        public void ShouldThrowWhenUsedAfterIsConnectedReturnsFalse()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should reject navigation when browser closes")]
        [Test, Ignore("SKIP WIRE")]
        public void ShouldRejectNavigationWhenBrowserCloses()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should reject waitForSelector when browser closes")]
        public void ShouldRejectWaitForSelectorWhenBrowserCloses()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should emit close events on pages and contexts")]
        [Test, Ignore("SKIP WIRE")]
        public void ShouldEmitCloseEventsOnPagesAndContexts()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should terminate network waiters")]
        [Test, Ignore("SKIP WIRE")]
        public void ShouldTerminateNetworkWaiters()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should respect selectors")]
        public void ShouldRespectSelectors()
        {
        }


        private class BrowserServer
        {
            public Process process { get; set; }
            public string wsEndpoint { get; set; }

        }

        private BrowserServer launchServer(IBrowserType browserType)
        {
            try
            {
                var browserServer = new BrowserServer();
                browserServer.process = GetProcess(browserType.Name); ;
                browserServer.process.Start();
                browserServer.process.Exited += (_, _) => browserServer.process.Kill();
                browserServer.wsEndpoint = browserServer.process.StandardOutput.ReadLine();
                
                if (!browserServer.wsEndpoint.StartsWith("ws://"))
                {
                    throw new PlaywrightException("Invalid web socket address: " + browserServer.wsEndpoint);
                }
                return browserServer;
            }
            catch (IOException ex)
            {
                throw new PlaywrightException("Failed to launch server", ex);
            }
        }

        private static Process GetProcess(string browserType)
            => new()
            {
                StartInfo =
                {
                    FileName = Paths.GetExecutablePath(),
                    Arguments = $"launch-server {browserType}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };
    }
}
