using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>worker.spec.js</playwright-file>
    ///<playwright-describe>Workers</playwright-describe>
    [Trait("Category", "chromium")]
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
        [Fact]
        public async Task PageWorkers()
        {
            await Task.WhenAll(
                Page.WaitForEvent<WorkerEventArgs>(PageEvent.WorkerCreated),
                Page.GoToAsync(TestConstants.ServerUrl + "/worker/worker.html"));
            var worker = Page.Workers[0];
            Assert.Contains("worker.js", worker.Url);

            Assert.Equal("worker function result", await worker.EvaluateAsync<string>("() => self['workerFunction']()"));

            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Empty(Page.Workers);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should emit created and destroyed events</playwright-it>
        [Fact]
        public async Task ShouldEmitCreatedAndDestroyedEvents()
        {
            var workerCreatedTcs = new TaskCompletionSource<IWorker>();
            Page.WorkerCreated += (sender, e) => workerCreatedTcs.TrySetResult(e.Worker);

            var workerObj = await Page.EvaluateHandleAsync("() => new Worker('data:text/javascript,1')");
            var worker = await workerCreatedTcs.Task;
            var workerDestroyedTcs = new TaskCompletionSource<IWorker>();
            Page.WorkerDestroyed += (sender, e) => workerDestroyedTcs.TrySetResult(e.Worker);
            await Page.EvaluateAsync("workerObj => workerObj.terminate()", workerObj);
            Assert.Same(worker, await workerDestroyedTcs.Task);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should report console logs</playwright-it>
        [Fact]
        public async Task ShouldReportConsoleLogs()
        {
            var (message, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent<ConsoleEventArgs>(PageEvent.Console),
                Page.EvaluateAsync("() => new Worker(`data:text/javascript,console.log(1)`)")
            );

            Assert.Equal("1", message.Message.Text);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should have JSHandles for console logs</playwright-it>
        [Fact]
        public async Task ShouldHaveJSHandlesForConsoleLogs()
        {
            var consoleTcs = new TaskCompletionSource<ConsoleMessage>();
            Page.Console += (sender, e) => consoleTcs.TrySetResult(e.Message);

            await Page.EvaluateAsync("() => new Worker(`data:text/javascript,console.log(1, 2, 3, this)`)");
            var log = await consoleTcs.Task;
            Assert.Equal("1 2 3 JSHandle@object", log.Text);
            Assert.Equal(4, log.Args.Count);
            string json = await (await log.Args[3].GetPropertyAsync("origin")).GetJsonValueAsync<string>();
            Assert.Equal("null", json);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should evaluate</playwright-it>
        [Fact]
        public async Task ShouldEvaluate()
        {
            var workerCreatedTask = Page.WaitForEvent<WorkerEventArgs>(PageEvent.WorkerCreated);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], {type: 'application/javascript'})))");

            await workerCreatedTask;
            Assert.Equal(2, await workerCreatedTask.Result.Worker.EvaluateAsync<int>("1+1"));
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should report errors</playwright-it>
        [Fact]
        public async Task ShouldReportErrors()
        {
            var errorTcs = new TaskCompletionSource<string>();
            Page.PageError += (sender, e) => errorTcs.TrySetResult(e.Message);

            await Page.EvaluateAsync("() => new Worker(`data:text/javascript, throw new Error('this is my error');`)");
            string errorLog = await errorTcs.Task;
            Assert.Contains("this is my error", errorLog);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should clear upon navigation</playwright-it>
        [Fact]
        public async Task ShouldClearUponNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var workerCreatedTask = Page.WaitForEvent<WorkerEventArgs>(PageEvent.WorkerCreated);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
            await workerCreatedTask;

            Assert.Single(Page.Workers);
            bool destroyed = false;
            Page.WorkerDestroyed += (sender, e) => destroyed = true;

            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.True(destroyed);
            Assert.Empty(Page.Workers);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should clear upon cross-process navigation</playwright-it>
        [Fact]
        public async Task ShouldClearUponCrossProcessNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var workerCreatedTask = Page.WaitForEvent<WorkerEventArgs>(PageEvent.WorkerCreated);
            await Page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(1)'], { type: 'application/javascript' })))");
            await workerCreatedTask;

            Assert.Single(Page.Workers);
            bool destroyed = false;
            Page.WorkerDestroyed += (sender, e) => destroyed = true;

            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.True(destroyed);
            Assert.Empty(Page.Workers);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should report network activity</playwright-it>
        [Fact]
        public async Task ShouldReportNetworkActivity()
        {
            var (worker, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent<WorkerEventArgs>(PageEvent.WorkerCreated),
                Page.GoToAsync(TestConstants.ServerUrl + "/worker/worker.html")
            );

            string url = TestConstants.ServerUrl + "/one-style.css";
            var requestTask = Page.WaitForRequestAsync(url);
            var responseTask = Page.WaitForResponseAsync(url);

            await worker.Worker.EvaluateAsync("url => fetch(url).then(response => response.text()).then(console.log)", url);

            await Task.WhenAll(requestTask, responseTask);

            Assert.Equal(url, requestTask.Result.Url);
            Assert.Equal(requestTask.Result.Response, responseTask.Result);
            Assert.True(responseTask.Result.Ok);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should report network activity on worker creation</playwright-it>
        [SkipBrowserAndPlatformFact(skipChromium: true)]
        public async Task ShouldReportNetworkActivityOnWorkerCreation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            string url = TestConstants.ServerUrl + "one-style.css";

            var requestTask = Page.WaitForRequestAsync(url);
            var responseTask = Page.WaitForResponseAsync(url);

            await Page.EvaluateAsync(@"url => new Worker(URL.createObjectURL(new Blob([`
                fetch(""${ url}"").then(response => response.text()).then(console.log);
              `], { type: 'application/javascript'})))", url);

            await Task.WhenAll(requestTask, responseTask);

            Assert.Equal(url, requestTask.Result.Url);
            Assert.Equal(requestTask.Result.Response, responseTask.Result);
            Assert.True(responseTask.Result.Ok);
        }

        ///<playwright-file>worker.spec.js</playwright-file>
        ///<playwright-describe>Workers</playwright-describe>
        ///<playwright-it>should report web socket activity</playwright-it>
        [Fact(Skip = "Skipped on Playwright")]
        public async Task ShouldReportWebSocketActivity()
        {
            var (worker, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent<WorkerEventArgs>(PageEvent.WorkerCreated),
                Page.GoToAsync(TestConstants.ServerUrl + "/worker/worker.html")
            );

            var log = new List<string>();
            var socketClosedTcs = new TaskCompletionSource<bool>();

            Page.Websocket += (sender, websocketEventArgs) =>
            {
                websocketEventArgs.Websocket.Open += (s, e) => log.Add($"open<{websocketEventArgs.Websocket.Url}");
                websocketEventArgs.Websocket.Close += (s, e) =>
                {
                    log.Add("close");
                    socketClosedTcs.TrySetResult(true);
                };
            };

            _ = worker.Worker.EvaluateAsync(
                @"(port) => {
                    const ws = new WebSocket('ws://localhost:' + port + '/ws');
                    ws.addEventListener('open', () => ws.close());
                }",
                TestConstants.Port);

            await socketClosedTcs.Task;
            Assert.Equal($"open < ws://localhost:{TestConstants.Port}/ws>:close", string.Join(":", log));
        }
    }
}
