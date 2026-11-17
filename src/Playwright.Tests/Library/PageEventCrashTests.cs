/**
 * Copyright 2017 Google Inc. All rights reserved.
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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/page-event-crash.spec.ts.</summary>
public class PageEventCrashTests
{
    [Fixture]
    public static async Task Crash(IPage page, string browserName, Func<Action, Task> use)
    {
        await use(() =>
        {
            if (browserName == "chromium")
            {
                IgnoreErrors(page.GotoAsync("chrome://crash"));
            }
            else if (browserName == "webkit")
            {
                // .NET: toImpl(page).delegate._session.send('Page.crash', {}).catch(e => {}) needs toImpl, which only the Node.js tests have.
                throw new NotSupportedException("toImpl(page).delegate._session.send('Page.crash', {}) is not available");
            }
            else if (browserName == "firefox")
            {
                // .NET: toImpl(page).delegate._session.send('Page.crash', {}).catch(e => {}) needs toImpl, which only the Node.js tests have.
                throw new NotSupportedException("toImpl(page).delegate._session.send('Page.crash', {}) is not available");
            }
        });
    }

    // Force a separate worker to avoid messing up with other tests.
    [Fixture(Scope = FixtureScope.Worker)]
    public static string Dummy() => string.Empty;

    [BeforeEach]
    public static void BeforeEach(string platform, string browserName, string channel)
    {
        Info.Slow(platform == "linux" && (browserName == "webkit"), "WebKit/Linux tests are consistently slower on some Linux environments. Most likely WebContent process is not getting terminated properly and is causing the slowdown.");
        Info.Skip(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) && platform == "linux" && browserName == "webkit", "crash event takes about a minute to arrive and often exceeds the test timeout");
        Info.Skip(channel == "webkit-wsl", "WebKit on WSL is even slower than above ^^ - skipping for now");
        Info.Skip(browserName == "chromium" && HostPlatform.StartsWith("ubuntu24.04", StringComparison.Ordinal), "never dispatches the crash event");
    }

    [UpstreamTest("ER-014 should emit crash event when page crashes")]
    [LocalFixme("harness: the crash fixture crashes a page of Firefox and WebKit with toImpl(page).delegate._session.send('Page.crash', {}), and toImpl is not available (only in firefox and webkit)")]
    public async Task ShouldEmitCrashEventWhenPageCrashes(IPage page, Action crash)
    {
        await page.SetContentAsync("<div>This page should crash</div>");
        crash();
        var crashedPage = await Once<IPage>(h => page.Crash += h, h => page.Crash -= h);
        Assert.That(crashedPage, Is.SameAs(page));
    }

    [UpstreamTest("ER-015 should throw on any action after page crashes")]
    [LocalFixme("harness: the crash fixture crashes a page of Firefox and WebKit with toImpl(page).delegate._session.send('Page.crash', {}), and toImpl is not available (only in firefox and webkit)")]
    public async Task ShouldThrowOnAnyActionAfterPageCrashes(IPage page, Action crash, Server server, string browserName)
    {
        await page.SetContentAsync("<div>This page should crash</div>");
        crash();
        await Once<IPage>(h => page.Crash += h, h => page.Crash -= h);
        void expectCrashError(Exception error)
        {
            Assert.That(error, Is.Not.Null, "action should reject after crash");
            // In Firefox, crashed page is sometimes "closed".
            if (browserName == "firefox")
            {
                Assert.That(error.Message.Contains("has been closed") || error.Message.Contains("crashed"), Is.EqualTo(true), error.Message);
            }
            else
            {
                Assert.That(error.Message, Does.Contain("crashed"));
            }
        }
        expectCrashError(await Catch(() => page.EvaluateAsync("() => {}")));
        expectCrashError(await Catch(() => page.GotoAsync(server.EmptyPage)));
        expectCrashError(await Catch(() => page.ReloadAsync()));
    }

    [UpstreamTest("ER-016 expect should not hang when page crashed")]
    [LocalFixme("harness: the crash fixture crashes a page of Firefox and WebKit with toImpl(page).delegate._session.send('Page.crash', {}), and toImpl is not available (only in firefox and webkit)")]
    public async Task ExpectShouldNotHangWhenPageCrashed(IPage page, Action crash)
    {
        var expectPromise = Expect(page.GetByText("child")).ToBeVisibleAsync();
        crash();
        await ThrowsAsync<PlaywrightException>(() => expectPromise);
    }

    [UpstreamTest("ER-017 should cancel waitForEvent when page crashes")]
    [LocalFixme("harness: the crash fixture crashes a page of Firefox and WebKit with toImpl(page).delegate._session.send('Page.crash', {}), and toImpl is not available (only in firefox and webkit)")]
    public async Task ShouldCancelWaitForEventWhenPageCrashes(IPage page, Action crash)
    {
        await page.SetContentAsync("<div>This page should crash</div>");
        var promise = Catch(() => page.WaitForResponseAsync(_ => true));
        crash();
        var error = await promise;
        Assert.That(error.Message, Does.Contain("Page crashed"));
    }

    [UpstreamTest("ER-018 should cancel navigation when page crashes")]
    [LocalFixme("dotnet: the message is 'Page crashed\\nCall log: ...', without the 'page.goto: ' prefix of the call (only in chromium); harness: the crash fixture crashes a page of Firefox and WebKit with toImpl(page).delegate._session.send('Page.crash', {}), and toImpl is not available (only in firefox and webkit)")]
    public async Task ShouldCancelNavigationWhenPageCrashes(Server server, IPage page, Action crash)
    {
        await page.SetContentAsync("<div>This page should crash</div>");
        server.SetRoute("/one-style.css", (req, res) => { });
        var promise = Catch(() => page.GotoAsync(server.Prefix + "/one-style.html"));
        await page.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        crash();
        var error = await promise;
        Assert.That(error.Message, Does.Contain("page.goto: Page crashed"));
    }

    [UpstreamTest("ER-019 should be able to close context when page crashes")]
    [LocalFixme("harness: the crash fixture crashes a page of Firefox and WebKit with toImpl(page).delegate._session.send('Page.crash', {}), and toImpl is not available (only in firefox and webkit)")]
    public async Task ShouldBeAbleToCloseContextWhenPageCrashes(bool isAndroid, IPage page, Action crash)
    {
        Info.Skip(isAndroid);

        await page.SetContentAsync("<div>This page should crash</div>");
        crash();
        await Once<IPage>(h => page.Crash += h, h => page.Crash -= h);
        await page.Context.CloseAsync();
    }

    [UpstreamTest("ER-020 should be able to close page after crash")]
    [LocalFixme("harness: the crash fixture crashes a page of Firefox and WebKit with toImpl(page).delegate._session.send('Page.crash', {}), and toImpl is not available (only in firefox and webkit)")]
    public async Task ShouldBeAbleToClosePageAfterCrash(IPage page, Action crash)
    {
        await page.SetContentAsync("<div>This page should crash</div>");
        crash();
        await Once<IPage>(h => page.Crash += h, h => page.Crash -= h);
        // Give the browser a chance to erroneously "close" the crashed page.
        await Task.Delay(3000);
        Assert.That(page.IsClosed, Is.EqualTo(false));
        Assert.That(page.Context.Pages, Is.EqualTo(new[] { page }));
        await page.CloseAsync();
        Assert.That(page.IsClosed, Is.EqualTo(true));
        Assert.That(page.Context.Pages, Is.EqualTo(Array.Empty<IPage>()));
    }

    [UpstreamTest("ER-021 should reject in-flight worker.evaluate when page crashes")]
    [LocalFixme("harness: the crash fixture crashes a page of Firefox and WebKit with toImpl(page).delegate._session.send('Page.crash', {}), and toImpl is not available (only in firefox and webkit)")]
    public async Task ShouldRejectInFlightWorkerEvaluateWhenPageCrashes(IPage page, Action crash, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (worker, _) = await PromiseAll(
            page.WaitForWorkerAsync(),
            page.EvaluateAsync(@"() => new Worker(URL.createObjectURL(
        new Blob(['self.onmessage = () => {}'], { type: 'application/javascript' })))"));
        var closePromise = Once<IWorker>(h => worker.Close += h, h => worker.Close -= h);
        var evalPromise = Catch(() => worker.EvaluateAsync<object>("() => new Promise(() => {})")); // never resolves in-worker
        crash();
        var error = await evalPromise;
        // Chromium may detach the worker before it reports the crash.
        Assert.That(error.Message, Does.Match("Page crashed|Target page, context or browser has been closed"));
        await closePromise;
        Assert.That(page.Workers, Is.EqualTo(Array.Empty<IWorker>()));
    }
}
