/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

using System.Text.Json;

namespace Microsoft.Playwright.Tests;

public class WorkersTests : PageTestEx
{
    [PlaywrightTest("workers.spec.ts", "Page.workers")]
    public async Task PageWorkers()
    {
        await TaskUtils.WhenAll(
            Page.WaitForWorkerAsync(),
            Page.GotoAsync(Server.Prefix + "/worker/worker.html"));
        var worker = Page.Workers.First();
        StringAssert.Contains("worker.js", worker.Url);

        Assert.AreEqual("worker function result", await worker.EvaluateAsync<string>("() => self['workerFunction']()"));

        await Page.GotoAsync(Server.EmptyPage);
        Assert.IsEmpty(Page.Workers);
    }

    [PlaywrightTest("workers.spec.ts", "should emit created and destroyed events")]
    public async Task ShouldEmitCreatedAndDestroyedEvents()
    {
        var workerCreatedTcs = new TaskCompletionSource<IWorker>();
        Page.Worker += (_, e) => workerCreatedTcs.TrySetResult(e);

        var workerObj = await Page.EvaluateHandleAsync("() => new Worker(URL.createObjectURL(new Blob(['1'], {type: 'application/javascript'})))");
        var worker = await workerCreatedTcs.Task;
        var workerThisObj = await worker.EvaluateHandleAsync("() => this");
        var workerDestroyedTcs = new TaskCompletionSource<IWorker>();
        worker.Close += (sender, _) => workerDestroyedTcs.TrySetResult((IWorker)sender);
        await Page.EvaluateAsync("workerObj => workerObj.terminate()", workerObj);
        Assert.AreEqual(worker, await workerDestroyedTcs.Task);
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => workerThisObj.GetPropertyAsync("self"));
        StringAssert.IsMatch("(Worker was closed)|(Target closed)", exception.Message);
    }

    [PlaywrightTest("workers.spec.ts", "should report console logs")]
    public async Task ShouldReportConsoleLogs()
    {
        var (message, _) = await TaskUtils.WhenAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], {type: 'application/javascript'})))")
        );

        Assert.AreEqual("1", message.Text);
    }

    [PlaywrightTest("workers.spec.ts", "should have JSHandles for console logs")]
    public async Task ShouldHaveJSHandlesForConsoleLogs()
    {
        var consoleTcs = new TaskCompletionSource<IConsoleMessage>();
        Page.Console += (_, e) => consoleTcs.TrySetResult(e);

        await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1,2,3,this)'], {type: 'application/javascript'})))");
        var log = await consoleTcs.Task;
        if (TestConstants.IsFirefox)
        {
            Assert.AreEqual("1 2 3 JSHandle@object", log.Text);
        }
        else
        {
            Assert.AreEqual("1 2 3 DedicatedWorkerGlobalScope", log.Text);
        }
        Assert.AreEqual(4, log.Args.Count());
        string json = await (await log.Args.ElementAt(3).GetPropertyAsync("origin")).JsonValueAsync<string>();
        Assert.AreEqual("null", json);
    }

    [PlaywrightTest("workers.spec.ts", "should evaluate")]
    public async Task ShouldEvaluate()
    {
        var workerCreatedTask = Page.WaitForWorkerAsync();
        await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], {type: 'application/javascript'})))");

        await workerCreatedTask;
        Assert.AreEqual(2, await workerCreatedTask.Result.EvaluateAsync<int>("1+1"));
    }

    [PlaywrightTest("workers.spec.ts", "should report errors")]
    public async Task ShouldReportErrors()
    {
        var errorTcs = new TaskCompletionSource<string>();
        Page.PageError += (_, e) => errorTcs.TrySetResult(e);

        await Page.EvaluateAsync(@"() => new Worker(URL.createObjectURL(new Blob([`
              setTimeout(() => {
                // Do a console.log just to check that we do not confuse it with an error.
                console.log('hey');
                throw new Error('this is my error');
              })
            `], {type: 'application/javascript'})))");
        string errorLog = await errorTcs.Task;
        StringAssert.Contains("this is my error", errorLog);
    }

    [PlaywrightTest("workers.spec.ts", "should clear upon navigation")]
    public async Task ShouldClearUponNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var workerCreatedTask = Page.WaitForWorkerAsync();
        await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
        var worker = await workerCreatedTask;

        Assert.That(Page.Workers, Has.Count.EqualTo(1));
        bool destroyed = false;
        worker.Close += (_, _) => destroyed = true;

        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.True(destroyed);
        Assert.IsEmpty(Page.Workers);
    }

    [PlaywrightTest("workers.spec.ts", "should clear upon cross-process navigation")]
    public async Task ShouldClearUponCrossProcessNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var workerCreatedTask = Page.WaitForWorkerAsync();
        await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
        var worker = await workerCreatedTask;

        Assert.That(Page.Workers, Has.Count.EqualTo(1));
        bool destroyed = false;
        worker.Close += (_, _) => destroyed = true;

        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        Assert.True(destroyed);
        Assert.IsEmpty(Page.Workers);
    }

    [PlaywrightTest("workers.spec.ts", "should report network activity")]
    public async Task ShouldReportNetworkActivity()
    {
        var (worker, _) = await TaskUtils.WhenAll(
            Page.WaitForWorkerAsync(),
            Page.GotoAsync(Server.Prefix + "/worker/worker.html")
        );

        string url = Server.Prefix + "/one-style.css";
        var requestTask = Page.WaitForRequestAsync(url);
        var responseTask = Page.WaitForResponseAsync(url);

        await worker.EvaluateAsync<JsonElement>("url => fetch(url).then(response => response.text()).then(console.log)", url);

        await TaskUtils.WhenAll(requestTask, responseTask);

        Assert.AreEqual(url, requestTask.Result.Url);
        Assert.AreEqual(requestTask.Result, responseTask.Result.Request);
        Assert.True(responseTask.Result.Ok);
    }

    [PlaywrightTest("workers.spec.ts", "should report network activity on worker creation")]
    public async Task ShouldReportNetworkActivityOnWorkerCreation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        string url = Server.Prefix + "/one-style.css";

        var requestTask = Page.WaitForRequestAsync(url);
        var responseTask = Page.WaitForResponseAsync(url);

        await Page.EvaluateAsync(@"url => new Worker(URL.createObjectURL(new Blob([`
                fetch(""${url}"").then(response => response.text()).then(console.log);
              `], { type: 'application/javascript'})))", url);

        await TaskUtils.WhenAll(requestTask, responseTask);

        Assert.AreEqual(url, requestTask.Result.Url);
        Assert.AreEqual(requestTask.Result, responseTask.Result.Request);
        Assert.True(responseTask.Result.Ok);
    }

    [PlaywrightTest("workers.spec.ts", "should format number using context locale")]
    public async Task ShouldFormatNumberUsingContextLocale()
    {
        await using var context = await Browser.NewContextAsync(new() { Locale = "ru-RU" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var (worker, _) = await TaskUtils.WhenAll(
            page.WaitForWorkerAsync(),
            page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], {type: 'application/javascript'})))"));

        Assert.AreEqual("10\u00A0000,2", await worker.EvaluateAsync<string>("() => (10000.20).toLocaleString()"));
    }
}
