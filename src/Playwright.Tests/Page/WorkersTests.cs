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
public class WorkersTests : UpstreamPageTest
{
    [UpstreamTest("WO-001 Page.workers @smoke")]
    public async Task PageWorkers()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 143, "needs workerScriptLoaded event");

        await PromiseAll(
            Page.WaitForWorkerAsync(),
            Page.GotoAsync(Server.Prefix + "/worker/worker.html"));
        var worker = Page.Workers[0];
        Assert.That(worker.Url, Does.Contain("worker.js"));

        Assert.That(await worker.EvaluateAsync<string>("() => self['workerFunction']()"), Is.EqualTo("worker function result"));

        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(Page.Workers.Count, Is.EqualTo(0));
    }

    [UpstreamTest("WO-002 should emit created and destroyed events")]
    [LocalFixme("dotnet: the message is \"Target page, context or browser has been closed\", upstream expects the name of the call before it, \"jsHandle.getProperty: \"")]
    public async Task ShouldEmitCreatedAndDestroyedEvents()
    {
        var workerCreatedPromise = Page.WaitForWorkerAsync();
        var workerObj = await Page.EvaluateHandleAsync("() => new Worker(URL.createObjectURL(new Blob(['1'], { type: 'application/javascript' })))");
        var worker = await workerCreatedPromise;
        var workerThisObj = await worker.EvaluateHandleAsync("this");
        var workerDestroyedPromise = Once<IWorker>(h => worker.Close += h, h => worker.Close -= h);
        await Page.EvaluateAsync("workerObj => workerObj.terminate()", workerObj);
        Assert.That(await workerDestroyedPromise, Is.SameAs(worker));
        var error = await Catch(() => workerThisObj.GetPropertyAsync("self"));
        Assert.That(error.Message, Does.Contain("jsHandle.getProperty"));
        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
    }

    [UpstreamTest("WO-003 should reject in-flight evaluate and emit close when page closes")]
    public async Task ShouldRejectInFlightEvaluateAndEmitCloseWhenPageCloses()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (worker, _) = await PromiseAll(
            Page.WaitForWorkerAsync(),
            Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['1'], { type: 'application/javascript' })))"));
        var closePromise = Once<IWorker>(h => worker.Close += h, h => worker.Close -= h);
        var evalPromise = Catch(() => worker.EvaluateAsync<object>("() => new Promise(() => {})"));
        await Page.CloseAsync();
        var error = await evalPromise;
        Assert.That(error.Message, Does.Match("(Target page, context or browser has been closed|Execution context was destroyed)"));
        Assert.That(await closePromise, Is.SameAs(worker));
    }

    [UpstreamTest("WO-004 should report console logs")]
    public async Task ShouldReportConsoleLogs()
    {
        var (message, _) = await PromiseAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))"));
        Assert.That(message.Text, Is.EqualTo("1"));
        // Firefox's juggler had an issue that reported worker blob urls as frame urls.
        Assert.That(Page.Url, Does.Not.Contain("blob"));
    }

    [UpstreamTest("WO-005 should have timestamp on worker console messages")]
    [LocalFixme("dotnet: ConsoleMessage.Timestamp is a float, which holds the milliseconds since the epoch in steps of 131072 only: the value is up to 65 seconds away from the time of the message, and the test passes only when the rounded value falls in its window by chance")]
    public async Task ShouldHaveTimestampOnWorkerConsoleMessages()
    {
        Info.Skip(IsAndroid, "there is a time difference between android emulator and host machine");
        Info.Skip(Channel == "webkit-wsl", "there is a time difference between WSL VM and host machine");

        // .NET: a number of JavaScript is a double. NUnit compares a float with a long as two floats.
        double before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1;  // Account for the rounding of fractional timestamps.
        var (message, _) = await PromiseAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(\"ts\")'], { type: 'application/javascript' })))"));
        double after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1;  // Account for the rounding of fractional timestamps.
        Assert.That(message.Text, Is.EqualTo("ts"));
        Assert.That(message.Timestamp, Is.GreaterThanOrEqualTo(before));
        Assert.That(message.Timestamp, Is.LessThanOrEqualTo(after));
    }

    [UpstreamTest("WO-006 should not report console logs from workers twice")]
    public async Task ShouldNotReportConsoleLogsFromWorkersTwice()
    {
        var messages = new List<string>();
        Page.Console += (_, msg) => messages.Add(msg.Text);
        await PromiseAll(
            Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1); console.log(2);'], { type: 'application/javascript' })))"),
            Page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text == "1" }),
            Page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text == "2" }));
        Assert.That(messages, Is.EqualTo(new[] { "1", "2" }));
        // Firefox's juggler had an issue that reported worker blob urls as frame urls.
        Assert.That(Page.Url, Does.Not.Contain("blob"));
    }

    [UpstreamTest("WO-007 should have JSHandles for console logs")]
    public async Task ShouldHaveJSHandlesForConsoleLogs()
    {
        var logPromise = new TaskCompletionSource<IConsoleMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        Page.Console += (_, x) => logPromise.TrySetResult(x);
        await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1,2,3,this)'], { type: 'application/javascript' })))");
        var log = await logPromise.Task;
        if (BrowserName != "firefox")
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
    public async Task ShouldEvaluate()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 143, "needs workerScriptLoaded event");

        var workerCreatedPromise = Page.WaitForWorkerAsync();
        await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
        var worker = await workerCreatedPromise;
        Assert.That(await worker.EvaluateAsync<int>("1+1"), Is.EqualTo(2));
    }

    [UpstreamTest("WO-009 should report console event on the worker")]
    public async Task ShouldReportConsoleEventOnTheWorker()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 143, "needs workerScriptLoaded event");

        var (worker, _) = await PromiseAll(
            Page.WaitForWorkerAsync(),
            Page.EvaluateAsync(@"() => {
      window.worker = new Worker(URL.createObjectURL(new Blob(['42'], { type: 'application/javascript' })));
    }"));
        var (message1, message2, message3, _) = await PromiseAll(
            Once<IConsoleMessage>(h => worker.Console += h, h => worker.Console -= h),
            Page.WaitForConsoleMessageAsync(),
            Page.Context.WaitForConsoleMessageAsync(),
            worker.EvaluateAsync<object>(@"() => {
      console.log('hello from worker');
    }"));
        Assert.That(message1.Text, Is.EqualTo("hello from worker"));
        Assert.That(message1, Is.SameAs(message2));
        Assert.That(message1, Is.SameAs(message3));
    }

    [UpstreamTest("WO-010 should report console event on the worker when not listening on page or context")]
    public async Task ShouldReportConsoleEventOnTheWorkerWhenNotListeningOnPageOrContext()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 143, "needs workerScriptLoaded event");

        var (worker, _) = await PromiseAll(
            Page.WaitForWorkerAsync(),
            Page.EvaluateAsync(@"() => {
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
    public async Task ShouldReportErrors()
    {
        var errorPromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Page.PageError += (_, x) => errorPromise.TrySetResult(x);
        await Page.EvaluateAsync(@"() => new Worker(URL.createObjectURL(new Blob([`
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
    public async Task ShouldClearUponNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var workerCreatedPromise = Page.WaitForWorkerAsync();
        await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
        var worker = await workerCreatedPromise;
        Assert.That(Page.Workers.Count, Is.EqualTo(1));
        var destroyed = false;
        EventHandler<IWorker> once = null;
        once = (_, _) =>
        {
            worker.Close -= once;
            destroyed = true;
        };
        worker.Close += once;
        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(destroyed, Is.EqualTo(true));
        Assert.That(Page.Workers.Count, Is.EqualTo(0));
    }

    [UpstreamTest("WO-013 should clear upon cross-process navigation")]
    public async Task ShouldClearUponCrossProcessNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var workerCreatedPromise = Page.WaitForWorkerAsync();
        await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
        var worker = await workerCreatedPromise;
        Assert.That(Page.Workers.Count, Is.EqualTo(1));
        var destroyed = false;
        EventHandler<IWorker> once = null;
        once = (_, _) =>
        {
            worker.Close -= once;
            destroyed = true;
        };
        worker.Close += once;
        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        Assert.That(destroyed, Is.EqualTo(true));
        Assert.That(Page.Workers.Count, Is.EqualTo(0));
    }

    [UpstreamTest("WO-014 should attribute network activity for worker inside iframe to the iframe")]
    public async Task ShouldAttributeNetworkActivityForWorkerInsideIframeToTheIframe()
    {
        Info.Skip(BrowserName == "firefox" && BrowserMajorVersion < 114, "https://github.com/microsoft/playwright/issues/21760");
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 149, "needs TargetInfo.parentFrameId for workers");

        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var (worker, frame) = await PromiseAll(
            Page.WaitForWorkerAsync(),
            AttachFrame(Page, "frame1", Server.Prefix + "/worker/worker.html"));
        var url = Server.Prefix + "/one-style.css";
        var (request, _) = await PromiseAll(
            Page.WaitForRequestAsync(url),
            worker.EvaluateAsync<object>("url => fetch(url).then(response => response.text()).then(console.log)", url));
        Assert.That(request.Url, Is.EqualTo(url));
        Assert.That(request.Frame, Is.SameAs(frame));
    }

    [UpstreamTest("WO-015 should report network activity")]
    public async Task ShouldReportNetworkActivity()
    {
        Info.Skip(BrowserName == "firefox" && BrowserMajorVersion < 114, "https://github.com/microsoft/playwright/issues/21760");
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 143, "needs workerScriptLoaded event");

        var (worker, _) = await PromiseAll(
            Page.WaitForWorkerAsync(),
            Page.GotoAsync(Server.Prefix + "/worker/worker.html"));
        var url = Server.Prefix + "/one-style.css";
        var requestPromise = Page.WaitForRequestAsync(url);
        var responsePromise = Page.WaitForResponseAsync(url);
        await worker.EvaluateAsync<object>("url => fetch(url).then(response => response.text()).then(console.log)", url);
        var request = await requestPromise;
        var response = await responsePromise;
        Assert.That(request.Url, Is.EqualTo(url));
        Assert.That(response.Request, Is.SameAs(request));
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(await response.TextAsync(), Is.EqualTo(File.ReadAllText(Asset("one-style.css"))));
    }

    [UpstreamTest("WO-016 should report network activity on worker creation")]
    public async Task ShouldReportNetworkActivityOnWorkerCreation()
    {
        Info.Skip(BrowserName == "firefox" && BrowserMajorVersion < 114, "https://github.com/microsoft/playwright/issues/21760");
        await Page.GotoAsync(Server.EmptyPage);
        var url = Server.Prefix + "/one-style.css";
        var requestPromise = Page.WaitForRequestAsync(url);
        var responsePromise = Page.WaitForResponseAsync(url);
        await Page.EvaluateAsync(@"url => new Worker(URL.createObjectURL(new Blob([`
    fetch(""${url}"").then(response => response.text()).then(console.log);
  `], { type: 'application/javascript' })))", url);
        var request = await requestPromise;
        var response = await responsePromise;
        Assert.That(request.Url, Is.EqualTo(url));
        Assert.That(response.Request, Is.SameAs(request));
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    [UpstreamTest("WO-017 should report worker script as network request")]
    public async Task ShouldReportWorkerScriptAsNetworkRequest()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (request1, request2, _) = await PromiseAll(
            Page.WaitForRequestAsync(r => r.Url.Contains("worker.js")),
            Page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url.Contains("worker.js") }),
            Page.EvaluateAsync("() => window.w = new Worker('/worker/worker.js')"));
        Assert.That(request1.Url, Is.EqualTo(Server.Prefix + "/worker/worker.js"));
        Assert.That(request1, Is.SameAs(request2));
        var response = await request1.ResponseAsync();
        var text = await response.TextAsync();
        Assert.That(text, Does.Contain("console.log('hello from the worker');"));
    }

    [UpstreamTest("WO-018 should report worker script as network request after redirect")]
    public async Task ShouldReportWorkerScriptAsNetworkRequestAfterRedirect()
    {
        Info.Fixme(BrowserName == "chromium", "Chromium does not report the redirect because it is not plumbed to the worker target");

        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRedirect("/worker.js", "/worker2.js");
        Server.SetRoute("/worker2.js", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/javascript");
            res.End("console.log('hello from the worker');");
        });
        var (request, _, _) = await PromiseAll(
            Page.WaitForRequestAsync(r => r.Url.Contains("worker.js")),
            Page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text.Contains("hello from the worker") }),
            Page.EvaluateAsync("() => window.w = new Worker('/worker.js')"));
        Assert.That(request.Url, Is.EqualTo(Server.Prefix + "/worker.js"));
        var redirect = request.RedirectedTo;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect.Url, Is.EqualTo(Server.Prefix + "/worker2.js"));
        var response = await redirect.ResponseAsync();
        var text = await response.TextAsync();
        Assert.That(text, Does.Contain("console.log('hello from the worker');"));
    }

    [UpstreamTest("WO-019 should dispatch console messages when page has workers")]
    public async Task ShouldDispatchConsoleMessagesWhenPageHasWorkers()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await PromiseAll(
            Page.WaitForWorkerAsync(),
            Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['const x = 1;'], { type: 'application/javascript' })))"));
        var (message, _) = await PromiseAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync("() => console.log('foo')"));
        Assert.That(message.Text, Is.EqualTo("foo"));
    }

    [UpstreamTest("WO-020 should report and intercept network from nested worker")]
    public async Task ShouldReportAndInterceptNetworkFromNestedWorker()
    {
        Info.Fixme(BrowserName == "webkit", "https://github.com/microsoft/playwright/issues/27376");
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 143, "needs workerScriptLoaded event");

        await Page.RouteAsync("**/simple.json", async route =>
        {
            var json = new { foo = "not bar" };
            await route.FulfillAsync(new() { Json = json });
        });

        await Page.GotoAsync(Server.EmptyPage);
        var url = Server.Prefix + "/simple.json";
        var workers = new List<IWorker>();
        var messages = new List<string>();

        Page.Worker += (_, worker) => workers.Add(worker);
        Page.Console += (_, msg) => messages.Add(msg.Text);

        await Page.EvaluateAsync(@"url => new Worker(URL.createObjectURL(new Blob([`
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
    public async Task ShouldSupportExtraHttpHeaders()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 143, "needs workerScriptLoaded event");

        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["foo"] = "bar" });
        var (worker, request1, _) = await PromiseAll(
            Page.WaitForWorkerAsync(),
            Server.WaitForRequest("/worker/worker.js"),
            Page.GotoAsync(Server.Prefix + "/worker/worker.html"));
        var (request2, _) = await PromiseAll(
            Server.WaitForRequest("/one-style.css"),
            worker.EvaluateAsync<object>("url => fetch(url)", Server.Prefix + "/one-style.css"));
        Assert.That(request1.Headers["foo"], Is.EqualTo("bar"));
        Assert.That(request2.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("WO-022 should support offline")]
    public async Task ShouldSupportOffline()
    {
        Info.Fixme(BrowserName == "webkit", "flaky on all platforms");
        Info.Fixme(BrowserName == "firefox", "does not plumb setOffline into WorkerNavigator::OnLine");

        var (worker, _, _) = await PromiseAll(
            Page.WaitForWorkerAsync(),
            Page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text.Contains("hello from the worker") }),
            Page.GotoAsync(Server.Prefix + "/worker/worker.html"));
        await Page.Context.SetOfflineAsync(true);
        await Expect(async () => Assert.That(await worker.EvaluateAsync<bool>("() => navigator.onLine"), Is.EqualTo(false))).ToPassAsync();
        Assert.That(await worker.EvaluateAsync<string>("() => fetch('/one-style.css').catch(e => 'error')"), Is.EqualTo("error"));
        await Page.Context.SetOfflineAsync(false);
        await Expect(async () => Assert.That(await worker.EvaluateAsync<bool>("() => navigator.onLine"), Is.EqualTo(true))).ToPassAsync();
    }

    [UpstreamTest("WO-023 should resolve worker script allHeaders in main frame")]
    public async Task ShouldResolveWorkerScriptAllHeadersInMainFrame()
    {
        var (request, _) = await PromiseAll(
            Page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url == Server.Prefix + "/worker/worker.js" }),
            Page.GotoAsync(Server.Prefix + "/worker/worker.html"));
        var response = await request.ResponseAsync();
        var requestHeaders = await request.AllHeadersAsync();
        Assert.That(requestHeaders["host"], Is.Not.Empty);
        var responseHeaders = await response.AllHeadersAsync();
        Assert.That(responseHeaders["content-type"], Is.Not.Empty);
    }

    [UpstreamTest("WO-024 should resolve worker script allHeaders in iframe")]
    public async Task ShouldResolveWorkerScriptAllHeadersInIframe()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 151, "needs proper Network.requestWillBeSentExtraInfo");

        var (request, _) = await PromiseAll(
            Page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url == Server.Prefix + "/worker/worker.js" }),
            AttachFrame(Page, "frame1", Server.Prefix + "/worker/worker.html"));
        var response = await request.ResponseAsync();
        var requestHeaders = await request.AllHeadersAsync();
        Assert.That(requestHeaders["host"], Is.Not.Empty);
        var responseHeaders = await response.AllHeadersAsync();
        Assert.That(responseHeaders["content-type"], Is.Not.Empty);
    }

    [UpstreamTest("WO-025 should resolve worker script allHeaders in nested worker inside iframe")]
    public async Task ShouldResolveWorkerScriptAllHeadersInNestedWorkerInsideIframe()
    {
        Info.Fixme(BrowserName == "webkit", "cannot evaluate in nested worker");
        Info.Fixme(BrowserName == "firefox", "nested worker script request is not reported at all");

        var url = Server.Prefix + "/worker/worker.js";
        var (worker, _, _) = await PromiseAll(
            Page.WaitForWorkerAsync(),
            Page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url == url }),
            AttachFrame(Page, "frame1", Server.Prefix + "/worker/worker.html"));

        var (request, _) = await PromiseAll(
            Page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url == url }),
            worker.EvaluateAsync<object>(@"url => {
      self.w = new Worker(url);
    }", url));

        var response = await request.ResponseAsync();
        var headers = await response.AllHeadersAsync();
        Assert.That(headers["content-type"], Is.Not.Empty);
    }
}
