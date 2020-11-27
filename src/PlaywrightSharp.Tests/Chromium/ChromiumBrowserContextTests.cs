using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/chromium.spec.js</playwright-file>
    ///<playwright-describe>Chromium.startTracing</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ChromiumBrowserContextTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ChromiumBrowserContextTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should create a worker from a service worker</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCreateAWorkerFromAServiceWorker()
        {
            var workerTask = Context.WaitForEventAsync(ContextEvent.ServiceWorker);
            await Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/empty/sw.html");

            var worker = (await workerTask).Worker;
            Assert.Equal("[object ServiceWorkerGlobalScope]", await worker.EvaluateAsync<string>("() => self.toString()"));
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>serviceWorkers() should return current workers</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ServiceWorkersShouldReturnCurrentWorkers()
        {
            var (worker1, _) = await TaskUtils.WhenAll(
                Context.WaitForEventAsync(ContextEvent.ServiceWorker),
                Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/empty/sw.html"));

            Assert.Single(((IChromiumBrowserContext)Context).ServiceWorkers);

            var (worker2, _) = await TaskUtils.WhenAll(
                Context.WaitForEventAsync(ContextEvent.ServiceWorker),
                Page.GoToAsync(TestConstants.CrossProcessUrl + "/serviceworkers/empty/sw.html"));

            Assert.Equal(2, ((IChromiumBrowserContext)Context).ServiceWorkers.Length);
            Assert.Contains(worker1.Worker, ((IChromiumBrowserContext)Context).ServiceWorkers);
            Assert.Contains(worker2.Worker, ((IChromiumBrowserContext)Context).ServiceWorkers);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should not create a worker from a shared worker</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldNotCreateAWorkerFromASharedWorker()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            bool serviceWorkerCreated = false;
            ((IChromiumBrowserContext)Context).ServiceWorker += (sender, e) => serviceWorkerCreated = true;

            await Page.EvaluateAsync(@"() =>
            {
                new SharedWorker('data:text/javascript,console.log(""hi"")');
            }");
            Assert.False(serviceWorkerCreated);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should close service worker together with the context</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCloseServiceWorkerTogetherWithTheContext()
        {
            var (worker, _) = await TaskUtils.WhenAll(
                Context.WaitForEventAsync(ContextEvent.ServiceWorker),
                Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/empty/sw.html"));

            var messages = new List<string>();
            Context.Close += (sender, e) => messages.Add("context");
            worker.Worker.Close += (sender, e) => messages.Add("worker");

            await Context.CloseAsync();

            Assert.Equal("worker|context", string.Join("|", messages));
        }
    }
}
