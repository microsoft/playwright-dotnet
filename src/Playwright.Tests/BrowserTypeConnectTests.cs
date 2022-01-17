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
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
    public class BrowserTypeConnectTests : PlaywrightTestEx
    {
        private RemoteServer _remoteServer;

        [SetUp]
        public void SetUpAsync()
        {
            try
            {
                DirectoryInfo assemblyDirectory = new(AppContext.BaseDirectory);
                RemoteServer remoteServer = new();
                var startInfo = new ProcessStartInfo(Driver.GetExecutablePath(), $"launch-server {BrowserType.Name}")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                };
                foreach (var pair in Driver.GetEnvironmentVariables())
                {
                    startInfo.EnvironmentVariables[pair.Key] = pair.Value;
                }
                remoteServer.Process = new()
                {
                    StartInfo = startInfo,
                };
                remoteServer.Process.Start();
                remoteServer.WSEndpoint = remoteServer.Process.StandardOutput.ReadLine();

                if (remoteServer.WSEndpoint != null && !remoteServer.WSEndpoint.StartsWith("ws://"))
                {
                    throw new PlaywrightException("Invalid web socket address: " + remoteServer.WSEndpoint);
                }
                _remoteServer = remoteServer;
            }
            catch (IOException ex)
            {
                throw new PlaywrightException("Failed to launch server", ex);
            }
        }

        [TearDown]
        public void TearDown()
        {
            _remoteServer.Process.Kill(true);
            _remoteServer = null;
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to reconnect to a browser")]
        public async Task ShouldBeAbleToReconnectToABrowser()
        {
            {
                var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
                var browserContext = await browser.NewContextAsync();
                Assert.AreEqual(browserContext.Pages.Count, 0);
                var page = await browserContext.NewPageAsync();
                Assert.AreEqual(await page.EvaluateAsync<int>("11 * 11"), 121);
                await page.GotoAsync(Server.EmptyPage);
                await browser.CloseAsync();
            }
            {
                var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
                var browserContext = await browser.NewContextAsync();
                var page = await browserContext.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                await browser.CloseAsync();
            }
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to connect two browsers at the same time")]
        public async Task ShouldBeAbleToConnectTwoBrowsersAtTheSameTime()
        {
            var browser1 = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            Assert.AreEqual(browser1.Contexts.Count, 0);
            await browser1.NewContextAsync();
            Assert.AreEqual(browser1.Contexts.Count, 1);

            var browser2 = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            Assert.AreEqual(browser2.Contexts.Count, 0);
            await browser2.NewContextAsync();
            Assert.AreEqual(browser2.Contexts.Count, 1);
            Assert.AreEqual(browser1.Contexts.Count, 1);

            await browser1.CloseAsync();
            Assert.AreEqual(browser2.Contexts.Count, 1);

            var page2 = await browser2.NewPageAsync();
            Assert.AreEqual(await page2.EvaluateAsync<int>("7 * 6"), 42); // original browser should still work

            await browser2.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should timeout in connect while connecting")]
        [Skip(SkipAttribute.Targets.Windows)]
        public async Task ShouldTimeoutInConnectWhileConnecting()
        {
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(async () => await BrowserType.ConnectAsync($"ws://localhost:{Server.Port}/ws", new BrowserTypeConnectOptions { Timeout = 100 }));
            StringAssert.Contains("BrowserType.ConnectAsync: Timeout 100ms exceeded", exception.Message);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should support slowmo option")]
        public async Task ShouldSupportSlowMo()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint, new BrowserTypeConnectOptions { SlowMo = 200 });
            var start = DateTime.Now;
            var context = await browser.NewContextAsync();
            await browser.CloseAsync();
            Assert.Greater((DateTime.Now - start).TotalMilliseconds, 199);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "disconnected event should be emitted when browser is closed or server is closed")]
        public async Task DisconnectedEventShouldBeEmittedWhenBrowserIsClosedOrServerIsClosed()
        {
            var browser1 = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            await browser1.NewPageAsync();

            var browser2 = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            await browser2.NewPageAsync();

            int disconnected1 = 0;
            int disconnected2 = 0;
            browser1.Disconnected += (_, e) => disconnected1++;
            browser2.Disconnected += (_, e) => disconnected2++;

            var tsc1 = new TaskCompletionSource<object>();
            browser1.Disconnected += (_, e) => tsc1.SetResult(null);
            await browser1.CloseAsync();
            await tsc1.Task;
            Assert.AreEqual(disconnected1, 1);
            Assert.AreEqual(disconnected2, 0);

            var tsc2 = new TaskCompletionSource<object>();
            browser2.Disconnected += (_, e) => tsc2.SetResult(null);
            await browser2.CloseAsync();
            await tsc2.Task;
            Assert.AreEqual(disconnected1, 1);
            Assert.AreEqual(disconnected2, 1);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "disconnected event should have browser as argument")]
        public async Task DisconnectedEventShouldHaveBrowserAsArguments()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            IBrowser disconneced = null;
            var tsc = new TaskCompletionSource<object>();
            browser.Disconnected += (_, browser) =>
            {
                disconneced = browser;
                tsc.SetResult(null);
            };
            await browser.CloseAsync();
            await tsc.Task;
            Assert.AreEqual(browser, disconneced);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should set the browser connected state")]
        public async Task ShouldSetTheBrowserConnectedState()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            Assert.AreEqual(browser.IsConnected, true);
            var tsc = new TaskCompletionSource<bool>();
            browser.Disconnected += (_, e) => tsc.SetResult(false);
            _remoteServer.Close();
            await tsc.Task;
            Assert.AreEqual(browser.IsConnected, false);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should throw when used after isConnected returns false")]
        public async Task ShouldThrowWhenUsedAfterIsConnectedReturnsFalse()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var page = await browser.NewPageAsync();
            var tsc = new TaskCompletionSource<bool>();
            browser.Disconnected += (_, e) => tsc.SetResult(false);
            _remoteServer.Close();
            await tsc.Task;
            Assert.AreEqual(browser.IsConnected, false);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await page.EvaluateAsync("1 + 1"));
            StringAssert.Contains("has been closed", exception.Message);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should throw when calling waitForNavigation after disconnect")]
        public async Task ShouldThrowWhenWhenCallingWaitForNavigationAfterDisconnect()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var page = await browser.NewPageAsync();
            var tsc = new TaskCompletionSource<bool>();
            browser.Disconnected += (_, e) => tsc.SetResult(false);
            _remoteServer.Close();
            await tsc.Task;

            Assert.AreEqual(browser.IsConnected, false);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await page.WaitForNavigationAsync());
            StringAssert.Contains("Navigation failed because page was closed", exception.Message);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should reject navigation when browser closes")]
        public async Task ShouldRejectNavigationWhenBrowserCloses()
        {
            Server.SetRoute("/one-style.css", context =>
            {
                context.Response.Redirect("/one-style.css");
                return Task.CompletedTask;
            });

            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var page = await browser.NewPageAsync();
            var PageGoto = page.GotoAsync(Server.Prefix + "/one-style.html", new PageGotoOptions { Timeout = 60000 });
            await Server.WaitForRequest("/one-style.css");
            await browser.CloseAsync();

            Assert.AreEqual(browser.IsConnected, false);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await PageGoto);
            StringAssert.Contains("has been closed", exception.Message);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should reject waitForSelector when browser closes")]
        public async Task ShouldRejectWaitForSelectorWhenBrowserCloses()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var page = await browser.NewPageAsync();
            var watchdog = page.WaitForSelectorAsync("div");
            await browser.CloseAsync();

            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await watchdog);
            Assert.That(exception.Message, Contains.Substring("has been closed"));
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should emit close events on pages and contexts")]
        public async Task ShouldEmitCloseEventsOnPagesAndContexts()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var tsc = new TaskCompletionSource<object>();
            context.Close += (_, e) => tsc.SetResult(null);
            var page = await context.NewPageAsync();
            bool pageClosed = false;
            page.Close += (_, e) => pageClosed = true;

            _remoteServer.Close();
            await tsc.Task;
            Assert.AreEqual(pageClosed, true);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should terminate network waiters")]
        public async Task ShouldTerminateNetworkWaiters()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var page = await browser.NewPageAsync();
            var requestWatchdog = page.WaitForRequestAsync(Server.EmptyPage);
            var responseWatchog = page.WaitForResponseAsync(Server.EmptyPage);
            _remoteServer.Close();
            async Task CheckTaskHasException(Task task)
            {
                var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await task);
                StringAssert.Contains("Page closed", exception.Message);
                StringAssert.DoesNotContain("Timeout", exception.Message);

            }
            await CheckTaskHasException(requestWatchdog);
            await CheckTaskHasException(responseWatchog);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should not throw on close after disconnect")]
        public async Task ShouldNotThrowOnCloseAfterDisconnect()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var page = await browser.NewPageAsync();

            var tcs = new TaskCompletionSource<bool>();
            browser.Disconnected += (_, e) => tcs.SetResult(true);
            _remoteServer.Close();
            await tcs.Task;
            await browser.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should not throw on context.close after disconnect")]
        public async Task ShouldNotThrowOnContextCloseAfterDisconnect()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            await context.NewPageAsync();

            var tcs = new TaskCompletionSource<bool>();
            browser.Disconnected += (_, e) => tcs.SetResult(true);
            _remoteServer.Close();
            await tcs.Task;
            await context.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should not throw on page.close after disconnect")]
        public async Task ShouldNotThrowOnPageCloseAfterDisconnect()
        {
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var tcs = new TaskCompletionSource<bool>();
            browser.Disconnected += (_, e) => tcs.SetResult(true);
            _remoteServer.Close();
            await tcs.Task;
            await page.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should saveAs videos from remote browser")]
        public async Task ShouldSaveAsVideosFromRemoteBrowser()
        {
            using var tempDirectory = new TempDirectory();
            var videoPath = tempDirectory.Path;
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var context = await browser.NewContextAsync(new()
            {
                RecordVideoDir = videoPath,
                RecordVideoSize = new() { Height = 320, Width = 240 }
            });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await Task.Delay(1000);
            await context.CloseAsync();

            var videoSavePath = tempDirectory.Path + "my-video.webm";
            await page.Video.SaveAsAsync(videoSavePath);
            Assert.That(videoSavePath, Does.Exist);

            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await page.Video.PathAsync());
            StringAssert.Contains("Path is not available when connecting remotely. Use SaveAsAsync() to save a local copy", exception.Message);
        }


        [PlaywrightTest("browsertype-connect.spec.ts", "should save download")]
        public async Task ShouldSaveDownload()
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                context.Response.Headers["Content-Disposition"] = "attachment";
                return context.Response.WriteAsync("Hello world");
            });

            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
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
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => download.PathAsync());
            Assert.AreEqual("Path is not available when connecting remotely. Use SaveAsAsync() to save a local copy.", exception.Message);
            await browser.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should error when saving download after deletion")]
        public async Task ShouldErrorWhenSavingDownloadAfterDeletion()
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                context.Response.Headers["Content-Disposition"] = "attachment";
                return context.Response.WriteAsync("Hello world");
            });

            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
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
            await browser.CloseAsync();
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should save har")]
        public async Task ShouldSaveHar()
        {
            using var tempDirectory = new TempDirectory();
            var harPath = tempDirectory.Path + "/test.har";
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var context = await browser.NewContextAsync(new()
            {
                RecordHarPath = harPath
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            await context.CloseAsync();
            await browser.CloseAsync();

            Assert.That(harPath, Does.Exist);
            var logString = System.IO.File.ReadAllText(harPath);
            StringAssert.Contains(Server.EmptyPage, logString);
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should record trace with sources")]
        public async Task ShouldRecordContextTraces()
        {
            using var tempDirectory = new TempDirectory();
            var tracePath = tempDirectory.Path + "/trace.zip";
            var browser = await BrowserType.ConnectAsync(_remoteServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await context.Tracing.StartAsync(new() { Sources = true });
            await page.GotoAsync(Server.EmptyPage);
            await page.SetContentAsync("<button>Click</button>");
            await page.ClickAsync("button");
            await context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });

            await browser.CloseAsync();

            Assert.That(tracePath, Does.Exist);
            ZipFile.ExtractToDirectory(tracePath, tempDirectory.Path);
            Assert.That(tempDirectory.Path + "/trace.trace", Does.Exist);
            Assert.That(tempDirectory.Path + "/trace.network", Does.Exist);
            Assert.AreEqual(1, Directory.GetFiles(Path.Join(tempDirectory.Path, "resources"), "*.txt").Length);
        }

        private class RemoteServer
        {
            public Process Process { get; set; }
            public string WSEndpoint { get; set; }

            internal void Close()
            {
                Process.Kill(true);
                Process.WaitForExit();
            }
        }
    }
}
