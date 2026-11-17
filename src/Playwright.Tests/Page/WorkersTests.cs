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

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/workers.spec.ts.</summary>
public class WorkersTests
{
    [UpstreamTest("WO-001 Page.workers @smoke")]
    public async Task PageWorkers(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "chromium" && browserMajorVersion < 143, "needs workerScriptLoaded event");

        await PromiseAll(
            page.WaitForWorkerAsync(),
            page.GotoAsync(server.Prefix + "/worker/worker.html"));
        var worker = page.Workers[0];
        Assert.That(worker.Url, Does.Contain("worker.js"));

        Assert.That(await worker.EvaluateAsync<string>("() => self['workerFunction']()"), Is.EqualTo("worker function result"));

        await page.GotoAsync(server.EmptyPage);
        Assert.That(page.Workers.Count, Is.EqualTo(0));
    }

    [UpstreamTest("WO-002 should emit created and destroyed events")]
    [Fixme("dotnet: the message is \"Target page, context or browser has been closed\", upstream expects the name of the call before it, \"jsHandle.getProperty: \"")]
    public async Task ShouldEmitCreatedAndDestroyedEvents(IPage page)
    {
        var workerCreatedPromise = page.WaitForWorkerAsync();
        var workerObj = await page.EvaluateHandleAsync("() => new Worker(URL.createObjectURL(new Blob(['1'], { type: 'application/javascript' })))");
        var worker = await workerCreatedPromise;
        var workerThisObj = await worker.EvaluateHandleAsync("this");
        var workerDestroyedPromise = Once<IWorker>(h => worker.Close += h, h => worker.Close -= h);
        await page.EvaluateAsync("workerObj => workerObj.terminate()", workerObj);
        Assert.That(await workerDestroyedPromise, Is.SameAs(worker));
        var error = await Catch(() => workerThisObj.GetPropertyAsync("self"));
        Assert.That(error.Message, Does.Contain("jsHandle.getProperty"));
        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
    }

    [UpstreamTest("WO-003 should reject in-flight evaluate and emit close when page closes")]
    [Fixme("dotnet: the message is \"Target page, context or browser has been closed\", upstream expects the name of the call before it, \"worker.evaluate: \"", Browsers = "chromium,firefox")]
    [Fixme("dotnet: the message is \"Target page, context or browser has been closed\" or \"Execution context was destroyed, most likely because of a navigation.\", upstream expects the name of the call before it, \"worker.evaluate: \"", Browsers = "webkit")]
    public async Task ShouldRejectInFlightEvaluateAndEmitCloseWhenPageCloses(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (worker, _) = await PromiseAll(
            page.WaitForWorkerAsync(),
            page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['1'], { type: 'application/javascript' })))"));
        var closePromise = Once<IWorker>(h => worker.Close += h, h => worker.Close -= h);
        var evalPromise = Catch(() => worker.EvaluateAsync<object>("() => new Promise(() => {})"));
        await page.CloseAsync();
        var error = await evalPromise;
        Assert.That(error.Message, Does.Contain("worker.evaluate"));
        Assert.That(await closePromise, Is.SameAs(worker));
    }

    [UpstreamTest("WO-004 should report console logs")]
    public async Task ShouldReportConsoleLogs(IPage page)
    {
        var (message, _) = await PromiseAll(
            page.WaitForConsoleMessageAsync(),
            page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))"));
        Assert.That(message.Text, Is.EqualTo("1"));
        // Firefox's juggler had an issue that reported worker blob urls as frame urls.
        Assert.That(page.Url, Does.Not.Contain("blob"));
    }

    [UpstreamTest("WO-005 should have timestamp on worker console messages")]
    [Fixme("dotnet: ConsoleMessage.Timestamp is a float, which holds the milliseconds since the epoch in steps of 131072 only: the value is up to 65 seconds away from the time of the message, and the test passes only when the rounded value falls in its window by chance")]
    public async Task ShouldHaveTimestampOnWorkerConsoleMessages(IPage page, bool isAndroid, string channel)
    {
        It.Skip(isAndroid, "there is a time difference between android emulator and host machine");
        It.Skip(channel == "webkit-wsl", "there is a time difference between WSL VM and host machine");

        // .NET: a number of JavaScript is a double. NUnit compares a float with a long as two floats.
        double before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1;  // Account for the rounding of fractional timestamps.
        var (message, _) = await PromiseAll(
            page.WaitForConsoleMessageAsync(),
            page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(\"ts\")'], { type: 'application/javascript' })))"));
        double after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1;  // Account for the rounding of fractional timestamps.
        Assert.That(message.Text, Is.EqualTo("ts"));
        Assert.That(message.Timestamp, Is.GreaterThanOrEqualTo(before));
        Assert.That(message.Timestamp, Is.LessThanOrEqualTo(after));
    }

    [UpstreamTest("WO-006 should not report console logs from workers twice")]
    public async Task ShouldNotReportConsoleLogsFromWorkersTwice(IPage page)
    {
        var messages = new List<string>();
        page.Console += (_, msg) => messages.Add(msg.Text);
        await PromiseAll(
            page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1); console.log(2);'], { type: 'application/javascript' })))"),
            page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text == "1" }),
            page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text == "2" }));
        Assert.That(messages, Is.EqualTo(new[] { "1", "2" }));
        // Firefox's juggler had an issue that reported worker blob urls as frame urls.
        Assert.That(page.Url, Does.Not.Contain("blob"));
    }

    [UpstreamTest("WO-007 should have JSHandles for console logs")]
    public async Task ShouldHaveJSHandlesForConsoleLogs(IPage page, string browserName)
    {
        var logPromise = new TaskCompletionSource<IConsoleMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        page.Console += (_, x) => logPromise.TrySetResult(x);
        await page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1,2,3,this)'], { type: 'application/javascript' })))");
        var log = await logPromise.Task;
        if (browserName != "firefox")
        {
            Assert.That(log.Text, Is.EqualTo("1 2 3 DedicatedWorkerGlobalScope"));
        }
        else
        {
            Assert.That(log.Text, Is.EqualTo("1 2 3 JSHandle@object"));
        }
        Assert.That(log.Args.Count, Is.EqualTo(4));
        Assert.That(await (await log.Args[3].GetPropertyAsync("origin")).JsonValueAsync<string>(), Is.EqualTo("null"));
    }

    [UpstreamTest("WO-008 should evaluate")]
    public async Task ShouldEvaluate(IPage page, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "chromium" && browserMajorVersion < 143, "needs workerScriptLoaded event");

        var workerCreatedPromise = page.WaitForWorkerAsync();
        await page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
        var worker = await workerCreatedPromise;
        Assert.That(await worker.EvaluateAsync<int>("1+1"), Is.EqualTo(2));
    }

    [UpstreamTest("WO-009 should report console event on the worker")]
    public async Task ShouldReportConsoleEventOnTheWorker(IPage page, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "chromium" && browserMajorVersion < 143, "needs workerScriptLoaded event");

        var (worker, _) = await PromiseAll(
            page.WaitForWorkerAsync(),
            page.EvaluateAsync(@"() => {
      window.worker = new Worker(URL.createObjectURL(new Blob(['42'], { type: 'application/javascript' })));
    }"));
        var (message1, message2, message3, _) = await PromiseAll(
            Once<IConsoleMessage>(h => worker.Console += h, h => worker.Console -= h),
            page.WaitForConsoleMessageAsync(),
            page.Context.WaitForConsoleMessageAsync(),
            worker.EvaluateAsync<object>(@"() => {
      console.log('hello from worker');
    }"));
        Assert.That(message1.Text, Is.EqualTo("hello from worker"));
        Assert.That(message1, Is.SameAs(message2));
        Assert.That(message1, Is.SameAs(message3));
    }

    [UpstreamTest("WO-010 should report console event on the worker when not listening on page or context")]
    public async Task ShouldReportConsoleEventOnTheWorkerWhenNotListeningOnPageOrContext(IPage page, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "chromium" && browserMajorVersion < 143, "needs workerScriptLoaded event");

        var (worker, _) = await PromiseAll(
            page.WaitForWorkerAsync(),
            page.EvaluateAsync(@"() => {
      window.worker = new Worker(URL.createObjectURL(new Blob(['42'], { type: 'application/javascript' })));
    }"));
        var (message, _) = await PromiseAll(
            Once<IConsoleMessage>(h => worker.Console += h, h => worker.Console -= h),
            worker.EvaluateAsync<object>(@"() => {
      console.log('hello from worker');
    }"));
        Assert.That(message.Text, Is.EqualTo("hello from worker"));
    }

    [UpstreamTest("WO-011 should report errors")]
    public async Task ShouldReportErrors(IPage page)
    {
        var errorPromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        page.PageError += (_, x) => errorPromise.TrySetResult(x);
        await page.EvaluateAsync(@"() => new Worker(URL.createObjectURL(new Blob([`
    setTimeout(() => {
      // Do a console.log just to check that we do not confuse it with an error.
      console.log('hey');
      throw new Error('this is my error');
    })
  `], { type: 'application/javascript' })))");
        var errorLog = await errorPromise.Task;
        Assert.That(errorLog, Does.Contain("this is my error"));
    }

    [UpstreamTest("WO-012 should clear upon navigation")]
    public async Task ShouldClearUponNavigation(Server server, IPage page)
    {
        await page.GotoAsync(server.EmptyPage);
        var workerCreatedPromise = page.WaitForWorkerAsync();
        await page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
        var worker = await workerCreatedPromise;
        Assert.That(page.Workers.Count, Is.EqualTo(1));
        var destroyed = false;
        EventHandler<IWorker> once = null;
        once = (_, _) =>
        {
            worker.Close -= once;
            destroyed = true;
        };
        worker.Close += once;
        await page.GotoAsync(server.Prefix + "/one-style.html");
        Assert.That(destroyed, Is.EqualTo(true));
        Assert.That(page.Workers.Count, Is.EqualTo(0));
    }

    [UpstreamTest("WO-013 should clear upon cross-process navigation")]
    public async Task ShouldClearUponCrossProcessNavigation(Server server, IPage page)
    {
        await page.GotoAsync(server.EmptyPage);
        var workerCreatedPromise = page.WaitForWorkerAsync();
        await page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
        var worker = await workerCreatedPromise;
        Assert.That(page.Workers.Count, Is.EqualTo(1));
        var destroyed = false;
        EventHandler<IWorker> once = null;
        once = (_, _) =>
        {
            worker.Close -= once;
            destroyed = true;
        };
        worker.Close += once;
        await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        Assert.That(destroyed, Is.EqualTo(true));
        Assert.That(page.Workers.Count, Is.EqualTo(0));
    }

    [UpstreamTest("WO-014 should attribute network activity for worker inside iframe to the iframe")]
    public async Task ShouldAttributeNetworkActivityForWorkerInsideIframeToTheIframe(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "firefox" && browserMajorVersion < 114, "https://github.com/microsoft/playwright/issues/21760");
        It.Skip(browserName == "chromium" && browserMajorVersion < 149, "needs TargetInfo.parentFrameId for workers");

        await page.GotoAsync(server.Prefix + "/empty.html");
        var (worker, frame) = await PromiseAll(
            page.WaitForWorkerAsync(),
            AttachFrame(page, "frame1", server.Prefix + "/worker/worker.html"));
        var url = server.Prefix + "/one-style.css";
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync(url),
            worker.EvaluateAsync<object>("url => fetch(url).then(response => response.text()).then(console.log)", url));
        Assert.That(request.Url, Is.EqualTo(url));
        Assert.That(request.Frame, Is.SameAs(frame));
    }

    [UpstreamTest("WO-015 should report network activity")]
    public async Task ShouldReportNetworkActivity(IPage page, Server server, string browserName, int browserMajorVersion, Func<string, string> asset)
    {
        It.Skip(browserName == "firefox" && browserMajorVersion < 114, "https://github.com/microsoft/playwright/issues/21760");
        It.Skip(browserName == "chromium" && browserMajorVersion < 143, "needs workerScriptLoaded event");

        var (worker, _) = await PromiseAll(
            page.WaitForWorkerAsync(),
            page.GotoAsync(server.Prefix + "/worker/worker.html"));
        var url = server.Prefix + "/one-style.css";
        var requestPromise = page.WaitForRequestAsync(url);
        var responsePromise = page.WaitForResponseAsync(url);
        await worker.EvaluateAsync<object>("url => fetch(url).then(response => response.text()).then(console.log)", url);
        var request = await requestPromise;
        var response = await responsePromise;
        Assert.That(request.Url, Is.EqualTo(url));
        Assert.That(response.Request, Is.SameAs(request));
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(await response.TextAsync(), Is.EqualTo(File.ReadAllText(asset("one-style.css"))));
    }

    [UpstreamTest("WO-016 should report network activity on worker creation")]
    public async Task ShouldReportNetworkActivityOnWorkerCreation(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "firefox" && browserMajorVersion < 114, "https://github.com/microsoft/playwright/issues/21760");
        await page.GotoAsync(server.EmptyPage);
        var url = server.Prefix + "/one-style.css";
        var requestPromise = page.WaitForRequestAsync(url);
        var responsePromise = page.WaitForResponseAsync(url);
        await page.EvaluateAsync(@"url => new Worker(URL.createObjectURL(new Blob([`
    fetch(""${url}"").then(response => response.text()).then(console.log);
  `], { type: 'application/javascript' })))", url);
        var request = await requestPromise;
        var response = await responsePromise;
        Assert.That(request.Url, Is.EqualTo(url));
        Assert.That(response.Request, Is.SameAs(request));
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    [UpstreamTest("WO-017 should report worker script as network request")]
    public async Task ShouldReportWorkerScriptAsNetworkRequest(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (request1, request2, _) = await PromiseAll(
            page.WaitForRequestAsync(r => r.Url.Contains("worker.js")),
            page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url.Contains("worker.js") }),
            page.EvaluateAsync("() => window.w = new Worker('/worker/worker.js')"));
        Assert.That(request1.Url, Is.EqualTo(server.Prefix + "/worker/worker.js"));
        Assert.That(request1, Is.SameAs(request2));
        var response = await request1.ResponseAsync();
        var text = await response.TextAsync();
        Assert.That(text, Does.Contain("console.log('hello from the worker');"));
    }

    [UpstreamTest("WO-018 should report worker script as network request after redirect")]
    public async Task ShouldReportWorkerScriptAsNetworkRequestAfterRedirect(IPage page, Server server, string browserName)
    {
        It.Fixme(browserName == "chromium", "Chromium does not report the redirect because it is not plumbed to the worker target");

        await page.GotoAsync(server.EmptyPage);
        server.SetRedirect("/worker.js", "/worker2.js");
        server.SetRoute("/worker2.js", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/javascript");
            res.End("console.log('hello from the worker');");
        });
        var (request, _, _) = await PromiseAll(
            page.WaitForRequestAsync(r => r.Url.Contains("worker.js")),
            page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text.Contains("hello from the worker") }),
            page.EvaluateAsync("() => window.w = new Worker('/worker.js')"));
        Assert.That(request.Url, Is.EqualTo(server.Prefix + "/worker.js"));
        var redirect = request.RedirectedTo;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect.Url, Is.EqualTo(server.Prefix + "/worker2.js"));
        var response = await redirect.ResponseAsync();
        var text = await response.TextAsync();
        Assert.That(text, Does.Contain("console.log('hello from the worker');"));
    }

    [UpstreamTest("WO-019 should dispatch console messages when page has workers")]
    public async Task ShouldDispatchConsoleMessagesWhenPageHasWorkers(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await PromiseAll(
            page.WaitForWorkerAsync(),
            page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['const x = 1;'], { type: 'application/javascript' })))"));
        var (message, _) = await PromiseAll(
            page.WaitForConsoleMessageAsync(),
            page.EvaluateAsync("() => console.log('foo')"));
        Assert.That(message.Text, Is.EqualTo("foo"));
    }

    [UpstreamTest("WO-020 should report and intercept network from nested worker")]
    public async Task ShouldReportAndInterceptNetworkFromNestedWorker(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Fixme(browserName == "webkit", "https://github.com/microsoft/playwright/issues/27376");
        It.Skip(browserName == "chromium" && browserMajorVersion < 143, "needs workerScriptLoaded event");

        await page.RouteAsync("**/simple.json", async route =>
        {
            var json = new { foo = "not bar" };
            await route.FulfillAsync(new() { Json = json });
        });

        await page.GotoAsync(server.EmptyPage);
        var url = server.Prefix + "/simple.json";
        var workers = new List<IWorker>();
        var messages = new List<string>();

        page.Worker += (_, worker) => workers.Add(worker);
        page.Console += (_, msg) => messages.Add(msg.Text);

        await page.EvaluateAsync(@"url => new Worker(URL.createObjectURL(new Blob([`
    fetch(""${url}"").then(response => response.text()).then(t => console.log(t.trim()));
  `], { type: 'application/javascript' })))", url);
        await Expect(async () => Assert.That(workers.Count, Is.EqualTo(1))).ToPassAsync();

        await workers[0].EvaluateAsync<object>(@"url => new Worker(URL.createObjectURL(new Blob([`
    fetch(""${url}"").then(response => response.text()).then(t => console.log(t.trim()));
  `], { type: 'application/javascript' })))", url);

        await Expect(async () => Assert.That(workers.Count, Is.EqualTo(2))).ToPassAsync();

        await Expect(async () => Assert.That(messages, Is.EqualTo(new[] { "{\"foo\":\"not bar\"}", "{\"foo\":\"not bar\"}" }))).ToPassAsync();
    }

    [UpstreamTest("WO-021 should support extra http headers")]
    public async Task ShouldSupportExtraHttpHeaders(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "chromium" && browserMajorVersion < 143, "needs workerScriptLoaded event");

        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["foo"] = "bar" });
        var (worker, request1, _) = await PromiseAll(
            page.WaitForWorkerAsync(),
            server.WaitForRequest("/worker/worker.js"),
            page.GotoAsync(server.Prefix + "/worker/worker.html"));
        var (request2, _) = await PromiseAll(
            server.WaitForRequest("/one-style.css"),
            worker.EvaluateAsync<object>("url => fetch(url)", server.Prefix + "/one-style.css"));
        Assert.That(request1.Headers["foo"], Is.EqualTo("bar"));
        Assert.That(request2.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("WO-022 should support offline")]
    public async Task ShouldSupportOffline(IPage page, Server server, string browserName)
    {
        It.Fixme(browserName == "webkit", "flaky on all platforms");
        It.Fixme(browserName == "firefox", "does not plumb setOffline into WorkerNavigator::OnLine");

        var (worker, _, _) = await PromiseAll(
            page.WaitForWorkerAsync(),
            page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text.Contains("hello from the worker") }),
            page.GotoAsync(server.Prefix + "/worker/worker.html"));
        await page.Context.SetOfflineAsync(true);
        await Expect(async () => Assert.That(await worker.EvaluateAsync<bool>("() => navigator.onLine"), Is.EqualTo(false))).ToPassAsync();
        Assert.That(await worker.EvaluateAsync<string>("() => fetch('/one-style.css').catch(e => 'error')"), Is.EqualTo("error"));
        await page.Context.SetOfflineAsync(false);
        await Expect(async () => Assert.That(await worker.EvaluateAsync<bool>("() => navigator.onLine"), Is.EqualTo(true))).ToPassAsync();
    }

    [UpstreamTest("WO-023 should resolve worker script allHeaders in main frame")]
    public async Task ShouldResolveWorkerScriptAllHeadersInMainFrame(IPage page, Server server, string browserName)
    {
        var (request, _) = await PromiseAll(
            page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url == server.Prefix + "/worker/worker.js" }),
            page.GotoAsync(server.Prefix + "/worker/worker.html"));
        var response = await request.ResponseAsync();
        var requestHeaders = await request.AllHeadersAsync();
        Assert.That(requestHeaders["host"], Is.Not.Empty);
        var responseHeaders = await response.AllHeadersAsync();
        Assert.That(responseHeaders["content-type"], Is.Not.Empty);
    }

    [UpstreamTest("WO-024 should resolve worker script allHeaders in iframe")]
    public async Task ShouldResolveWorkerScriptAllHeadersInIframe(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "chromium" && browserMajorVersion < 151, "needs proper Network.requestWillBeSentExtraInfo");

        var (request, _) = await PromiseAll(
            page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url == server.Prefix + "/worker/worker.js" }),
            AttachFrame(page, "frame1", server.Prefix + "/worker/worker.html"));
        var response = await request.ResponseAsync();
        var requestHeaders = await request.AllHeadersAsync();
        Assert.That(requestHeaders["host"], Is.Not.Empty);
        var responseHeaders = await response.AllHeadersAsync();
        Assert.That(responseHeaders["content-type"], Is.Not.Empty);
    }

    [UpstreamTest("WO-025 should resolve worker script allHeaders in nested worker inside iframe")]
    public async Task ShouldResolveWorkerScriptAllHeadersInNestedWorkerInsideIframe(IPage page, Server server, string browserName)
    {
        It.Fixme(browserName == "webkit", "cannot evaluate in nested worker");
        It.Fixme(browserName == "firefox", "nested worker script request is not reported at all");

        var url = server.Prefix + "/worker/worker.js";
        var (worker, _, _) = await PromiseAll(
            page.WaitForWorkerAsync(),
            page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url == url }),
            AttachFrame(page, "frame1", server.Prefix + "/worker/worker.html"));

        var (request, _) = await PromiseAll(
            page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url == url }),
            worker.EvaluateAsync<object>(@"url => {
      self.w = new Worker(url);
    }", url));

        var response = await request.ResponseAsync();
        var headers = await response.AllHeadersAsync();
        Assert.That(headers["content-type"], Is.Not.Empty);
    }
}
