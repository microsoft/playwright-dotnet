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

using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
    [TestClass]
    public class BrowserTypeConnectTests : PlaywrightTestEx
    {
        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to reconnect to a browser")]
        [Ignore("SKIP WIRE")]
        public void ShouldBeAbleToReconnectToABrowser()
        {
            /*
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            await using var browser = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            await browser.CloseAsync();

            await using var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint);

            context = await remote.NewContextAsync();
            page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            await remote.CloseAsync();
            await browserServer.CloseAsync();
            */
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
    }
}
