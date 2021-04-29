using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Testing.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class WorkersTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WorkersTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("workers.spec.ts", "Page.workers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PageWorkers()
        {
            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Worker),
                Page.GoToAsync(TestConstants.ServerUrl + "/worker/worker.html"));
            var worker = Page.Workers.First();
            Assert.Contains("worker.js", worker.Url);

            Assert.Equal("worker function result", await worker.EvaluateAsync<string>("() => self['workerFunction']()"));

            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Empty(Page.Workers);
        }

        [PlaywrightTest("workers.spec.ts", "should emit created and destroyed events")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Same(worker, await workerDestroyedTcs.Task);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => workerThisObj.GetPropertyAsync("self"));
            Assert.Contains("Most likely the worker has been closed.", exception.Message);
        }

        [PlaywrightTest("workers.spec.ts", "should report console logs")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportConsoleLogs()
        {
            var (message, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Console),
                Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], {type: 'application/javascript'})))")
            );

            Assert.Equal("1", message.Text);
        }

        [PlaywrightTest("workers.spec.ts", "should have JSHandles for console logs")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveJSHandlesForConsoleLogs()
        {
            var consoleTcs = new TaskCompletionSource<IConsoleMessage>();
            Page.Console += (_, e) => consoleTcs.TrySetResult(e);

            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1,2,3,this)'], {type: 'application/javascript'})))");
            var log = await consoleTcs.Task;
            Assert.Equal("1 2 3 JSHandle@object", log.Text);
            Assert.Equal(4, log.Args.Count());
            string json = await (await log.Args.ElementAt(3).GetPropertyAsync("origin")).JsonValueAsync<string>();
            Assert.Equal("null", json);
        }

        [PlaywrightTest("workers.spec.ts", "should evaluate")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldEvaluate()
        {
            var workerCreatedTask = Page.WaitForEventAsync(PageEvent.Worker);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], {type: 'application/javascript'})))");

            await workerCreatedTask;
            Assert.Equal(2, await workerCreatedTask.Result.EvaluateAsync<int>("1+1"));
        }

        [PlaywrightTest("workers.spec.ts", "should report errors")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Contains("this is my error", errorLog);
        }

        [PlaywrightTest("workers.spec.ts", "should clear upon navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClearUponNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var workerCreatedTask = Page.WaitForEventAsync(PageEvent.Worker);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
            var worker = await workerCreatedTask;

            Assert.Single(Page.Workers);
            bool destroyed = false;
            worker.Close += (_, _) => destroyed = true;

            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.True(destroyed);
            Assert.Empty(Page.Workers);
        }

        [Fact]
        public async Task WorkerShouldWaitOnClose()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var workerCreatedTask = Page.WaitForEventAsync(PageEvent.Worker);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
            var worker = await workerCreatedTask;

            Assert.Single(Page.Workers);

            var t = worker.WaitForCloseAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await t;
            Assert.Empty(Page.Workers);
        }

        [Fact]
        public async Task WorkerShouldFailOnTimeout()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var workerCreatedTask = Page.WaitForEventAsync(PageEvent.Worker);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
            var worker = await workerCreatedTask;

            Assert.Single(Page.Workers);

            var t = worker.WaitForCloseAsync(1);
            await Task.Delay(100);
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await Assert.ThrowsAsync<TimeoutException>(async () => await t);
        }

        [PlaywrightTest("workers.spec.ts", "should clear upon cross-process navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClearUponCrossProcessNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var workerCreatedTask = Page.WaitForEventAsync(PageEvent.Worker);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
            var worker = await workerCreatedTask;

            Assert.Single(Page.Workers);
            bool destroyed = false;
            worker.Close += (_, _) => destroyed = true;

            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.True(destroyed);
            Assert.Empty(Page.Workers);
        }

        [PlaywrightTest("workers.spec.ts", "should report network activity")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportNetworkActivity()
        {
            var (worker, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Worker),
                Page.GoToAsync(TestConstants.ServerUrl + "/worker/worker.html")
            );

            string url = TestConstants.ServerUrl + "/one-style.css";
            var requestTask = Page.WaitForRequestAsync(url);
            var responseTask = Page.WaitForResponseAsync(url);

            await worker.EvaluateAsync<JsonElement>("url => fetch(url).then(response => response.text()).then(console.log)", url);

            await TaskUtils.WhenAll(requestTask, responseTask);

            Assert.Equal(url, requestTask.Result.Url);
            Assert.Equal(requestTask.Result, responseTask.Result.Request);
            Assert.True(responseTask.Result.Ok);
        }

        [PlaywrightTest("workers.spec.ts", "should report network activity on worker creation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportNetworkActivityOnWorkerCreation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            string url = TestConstants.ServerUrl + "/one-style.css";

            var requestTask = Page.WaitForRequestAsync(url);
            var responseTask = Page.WaitForResponseAsync(url);

            await Page.EvaluateAsync(@"url => new Worker(URL.createObjectURL(new Blob([`
                fetch(""${url}"").then(response => response.text()).then(console.log);
              `], { type: 'application/javascript'})))", url);

            await TaskUtils.WhenAll(requestTask, responseTask);

            Assert.Equal(url, requestTask.Result.Url);
            Assert.Equal(requestTask.Result, responseTask.Result.Request);
            Assert.True(responseTask.Result.Ok);
        }

        [PlaywrightTest("workers.spec.ts", "should format number using context locale")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFormatNumberUsingContextLocale()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { Locale = "ru-RU" });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var (worker, _) = await TaskUtils.WhenAll(
                page.WaitForEventAsync(PageEvent.Worker),
                page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], {type: 'application/javascript'})))"));

            Assert.Equal("10\u00A0000,2", await worker.EvaluateAsync<string>("() => (10000.20).toLocaleString()"));
        }
    }
}
