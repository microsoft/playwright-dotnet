using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ChromiumTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ChromiumTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("chromium/chromium.spec.ts", "chromium", "should create a worker from a service worker")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCreateAWorkerFromAServiceWorker()
        {
            var workerTask = Context.WaitForEventAsync(ContextEvent.ServiceWorker);
            await Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/empty/sw.html");

            var worker = (await workerTask).Worker;
            Assert.Equal("[object ServiceWorkerGlobalScope]", await worker.EvaluateAsync<string>("() => self.toString()"));
        }

        [PlaywrightTest("chromium/chromium.spec.ts", "chromium", "serviceWorkers() should return current workers")]
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

        [PlaywrightTest("chromium/chromium.spec.ts", "chromium", "should not create a worker from a shared worker")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldNotCreateAWorkerFromASharedWorker()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            bool serviceWorkerCreated = false;
            ((IChromiumBrowserContext)Context).ServiceWorker += (_, _) => serviceWorkerCreated = true;

            await Page.EvaluateAsync(@"() =>
            {
                new SharedWorker('data:text/javascript,console.log(""hi"")');
            }");
            Assert.False(serviceWorkerCreated);
        }

        [PlaywrightTest("chromium/chromium.spec.ts", "chromium", "should close service worker together with the context")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCloseServiceWorkerTogetherWithTheContext()
        {
            var (worker, _) = await TaskUtils.WhenAll(
                Context.WaitForEventAsync(ContextEvent.ServiceWorker),
                Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/empty/sw.html"));

            var messages = new List<string>();
            Context.Close += (_, _) => messages.Add("context");
            worker.Worker.Close += (_, _) => messages.Add("worker");

            await Context.CloseAsync();

            Assert.Equal("worker|context", string.Join("|", messages));
        }

        [PlaywrightTest("chromium/chromium.spec.ts", "chromium", "Page.route should work with intervention headers")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task PageRouteShouldWorkWithInterventionHeaders()
        {
            Server.SetRoute("/intervention", context => context.Response.WriteAsync($@"
              <script>
                document.write('<script src=""{TestConstants.CrossProcessHttpPrefix}/intervention.js"">' + '</scr' + 'ipt>');
              </script>
            "));
            Server.SetRedirect("/intervention.js", "/redirect.js");

            string interventionHeader = null;
            Server.SetRoute("/redirect.js", context =>
            {
                interventionHeader = context.Request.Headers["intervention"];
                return context.Response.WriteAsync("console.log(1);");
            });

            await Page.RouteAsync("*", (route) => route.ResumeAsync());

            await Page.GoToAsync(TestConstants.ServerUrl + "/intervention");

            Assert.Contains("feature/5718547946799104", interventionHeader);
        }
    }
}
