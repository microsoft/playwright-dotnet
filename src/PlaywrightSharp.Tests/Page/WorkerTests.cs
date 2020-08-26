using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>worker.spec.js</playwright-file>
    ///<playwright-describe>Workers</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class WorkerTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WorkerTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>Page.Workers</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task PageWorkers()
        {
            await TaskUtils.WhenAll(
                Page.WaitForEvent<WorkerEventArgs>(PageEvent.Worker),
                Page.GoToAsync(TestConstants.ServerUrl + "/worker/worker.html"));
            var worker = Page.Workers.First();
            Assert.Contains("worker.js", worker.Url);

            Assert.Equal("worker function result", await worker.EvaluateAsync<string>("() => self['workerFunction']()"));

            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Empty(Page.Workers);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should emit created and destroyed events</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldEmitCreatedAndDestroyedEvents()
        {
            var workerCreatedTcs = new TaskCompletionSource<IWorker>();
            Page.Worker += (sender, e) => workerCreatedTcs.TrySetResult(e.Worker);

            var workerObj = await Page.EvaluateHandleAsync("() => new Worker(URL.createObjectURL(new Blob(['1'], {type: 'application/javascript'})))");
            var worker = await workerCreatedTcs.Task;
            var workerThisObj = await worker.EvaluateHandleAsync("() => this");
            var workerDestroyedTcs = new TaskCompletionSource<IWorker>();
            worker.Closed += (sender, e) => workerDestroyedTcs.TrySetResult((IWorker)sender);
            await Page.EvaluateAsync("workerObj => workerObj.terminate()", workerObj);
            Assert.Same(worker, await workerDestroyedTcs.Task);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => workerThisObj.GetPropertyAsync("self"));
            Assert.Contains("Most likely the worker has been closed.", exception.Message);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should report console logs</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportConsoleLogs()
        {
            var (message, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent<ConsoleEventArgs>(PageEvent.Console),
                Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], {type: 'application/javascript'})))")
            );

            Assert.Equal("1", message.Message.Text);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should have JSHandles for console logs</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldHaveJSHandlesForConsoleLogs()
        {
            var consoleTcs = new TaskCompletionSource<ConsoleMessage>();
            Page.Console += (sender, e) => consoleTcs.TrySetResult(e.Message);

            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1,2,3,this)'], {type: 'application/javascript'})))");
            var log = await consoleTcs.Task;
            Assert.Equal("1 2 3 JSHandle@object", log.Text);
            Assert.Equal(4, log.Args.Count());
            string json = await (await log.Args.ElementAt(3).GetPropertyAsync("origin")).GetJsonValueAsync<string>();
            Assert.Equal("null", json);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should evaluate</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldEvaluate()
        {
            var workerCreatedTask = Page.WaitForEvent<WorkerEventArgs>(PageEvent.Worker);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], {type: 'application/javascript'})))");

            await workerCreatedTask;
            Assert.Equal(2, await workerCreatedTask.Result.Worker.EvaluateAsync<int>("1+1"));
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should report errors</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportErrors()
        {
            var errorTcs = new TaskCompletionSource<string>();
            Page.PageError += (sender, e) => errorTcs.TrySetResult(e.Message);

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

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should clear upon navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldClearUponNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var workerCreatedTask = Page.WaitForEvent<WorkerEventArgs>(PageEvent.Worker);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
            var worker = (await workerCreatedTask).Worker;

            Assert.Single(Page.Workers);
            bool destroyed = false;
            worker.Closed += (sender, e) => destroyed = true;

            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.True(destroyed);
            Assert.Empty(Page.Workers);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should clear upon cross-process navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldClearUponCrossProcessNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var workerCreatedTask = Page.WaitForEvent<WorkerEventArgs>(PageEvent.Worker);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
            var worker = (await workerCreatedTask).Worker;

            Assert.Single(Page.Workers);
            bool destroyed = false;
            worker.Closed += (sender, e) => destroyed = true;

            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.True(destroyed);
            Assert.Empty(Page.Workers);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should report network activity</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportNetworkActivity()
        {
            var (worker, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent<WorkerEventArgs>(PageEvent.Worker),
                Page.GoToAsync(TestConstants.ServerUrl + "/worker/worker.html")
            );

            string url = TestConstants.ServerUrl + "/one-style.css";
            var requestTask = Page.WaitForRequestAsync(url);
            var responseTask = Page.WaitForResponseAsync(url);

            await worker.Worker.EvaluateAsync("url => fetch(url).then(response => response.text()).then(console.log)", url);

            await TaskUtils.WhenAll(requestTask, responseTask);

            Assert.Equal(url, requestTask.Result.Url);
            Assert.Equal(requestTask.Result, responseTask.Result.Request);
            Assert.True(responseTask.Result.Ok);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should report network activity on worker creation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should format number using context locale</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFormatNumberUsingContextLocale()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { Locale = "ru-RU" });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var (worker, _) = await TaskUtils.WhenAll(
                page.WaitForEvent<WorkerEventArgs>(PageEvent.Worker),
                page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], {type: 'application/javascript'})))"));

            Assert.Equal("10\u00A0000,2", await worker.Worker.EvaluateAsync<string>("() => (10000.20).toLocaleString()"));
        }
    }
}
