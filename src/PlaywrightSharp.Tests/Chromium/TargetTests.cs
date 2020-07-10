using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/chromium.spec.js</playwright-file>
    ///<playwright-describe>Chromium.startTracing</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class TargetTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public TargetTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>Chromium.targets should return all of the targets</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public void BrowserTargetsShouldReturnAllOfTheTargets()
        {
            // The pages will be the testing page and the original new tab page
            var targets = Browser.GetTargets();
            Assert.Contains(targets, target => target.Type == TargetType.Page
                && target.Url == TestConstants.AboutBlank);
            Assert.Contains(targets, target => target.Type == TargetType.Browser);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>Browser.pages should return all of the pages</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task BrowserPagesShouldReturnAllOfThePages()
        {
            // The pages will be the testing page and the original new tab page
            var allPages = await Context.GetPagesAsync();
            Assert.Single(allPages);
            Assert.Contains(Page, allPages);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should contain browser target</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public void ShouldContainBrowserTarget()
        {
            var targets = Browser.GetTargets();
            var browserTarget = targets.FirstOrDefault(target => target.Type == TargetType.Browser);
            Assert.NotNull(browserTarget);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should be able to use the default page in the browser</playwright-it>
        [Retry]
        public async Task ShouldBeAbleToUseTheDefaultPageInTheBrowser()
        {
            // The pages will be the testing page and the original newtab page
            var originalPage = (await Browser.DefaultContext.GetPagesAsync()).First();
            Assert.Equal("Hello world", await originalPage.EvaluateAsync<string>("() => ['Hello', 'world'].join(' ')"));
            Assert.NotNull(await originalPage.QuerySelectorAsync("body"));
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should report when a new page is created and closed</playwright-it>
        [Retry]
        public async Task ShouldReportWhenANewPageIsCreatedAndClosed()
        {
            var otherPageTask = Browser.WaitForTargetAsync(t => t.Url == TestConstants.CrossProcessUrl + "/empty.html")
                .ContinueWith(t => t.Result.GetPageAsync());

            await Task.WhenAll(
                otherPageTask,
                Page.EvaluateHandleAsync("url => window.open(url)", TestConstants.CrossProcessUrl + "/empty.html")
                );

            var otherPage = await otherPageTask.Result;
            Assert.Contains(TestConstants.CrossProcessUrl, otherPage.Url);

            Assert.Equal("Hello world", await otherPage.EvaluateAsync<string>("['Hello', 'world'].join(' ')"));
            Assert.NotNull(await otherPage.QuerySelectorAsync("body"));

            var allPages = await Context.GetPagesAsync();
            Assert.Contains(Page, allPages);
            Assert.Contains(otherPage, allPages);

            var closePageTaskCompletion = new TaskCompletionSource<IPage>();
            async void TargetDestroyedEventHandler(object sender, TargetChangedArgs e)
            {
                closePageTaskCompletion.SetResult(await e.Target.GetPageAsync());
                Browser.TargetDestroyed -= TargetDestroyedEventHandler;
            }
            Browser.TargetDestroyed += TargetDestroyedEventHandler;
            await otherPage.CloseAsync();
            Assert.Equal(otherPage, await closePageTaskCompletion.Task);

            allPages = await Task.WhenAll(Browser.GetTargets(Context).Select(target => target.GetPageAsync()));
            Assert.Contains(Page, allPages);
            Assert.DoesNotContain(otherPage, allPages);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should report when a service worker is created and destroyed</playwright-it>
        [Retry]
        public async Task ShouldReportWhenAServiceWorkerIsCreatedAndDestroyed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var createdTargetTaskCompletion = new TaskCompletionSource<ITarget>();
            void TargetCreatedEventHandler(object sender, TargetChangedArgs e)
            {
                createdTargetTaskCompletion.SetResult(e.Target);
                Browser.TargetCreated -= TargetCreatedEventHandler;
            }
            Browser.TargetCreated += TargetCreatedEventHandler;
            await Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/empty/sw.html");

            var createdTarget = await createdTargetTaskCompletion.Task;
            Assert.Equal(TargetType.ServiceWorker, createdTarget.Type);
            Assert.Equal(TestConstants.ServerUrl + "/serviceworkers/empty/sw.js", createdTarget.Url);

            var targetDestroyedTaskCompletion = new TaskCompletionSource<ITarget>();
            void TargetDestroyedEventHandler(object sender, TargetChangedArgs e)
            {
                targetDestroyedTaskCompletion.SetResult(e.Target);
                Browser.TargetDestroyed -= TargetDestroyedEventHandler;
            }
            Browser.TargetDestroyed += TargetDestroyedEventHandler;
            await Page.EvaluateAsync("window.registrationPromise.then(registration => registration.unregister())");
            Assert.Equal(createdTarget, await targetDestroyedTaskCompletion.Task);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should create a worker from a service worker</playwright-it>
        [Retry]
        public async Task ShouldCreateAWorkerFromAServiceWorker()
        {
            var workerTask = Browser.WaitForTargetAsync(t => t.Type == TargetType.ServiceWorker);
            await Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/empty/sw.html");

            var target = await workerTask;
            var worker = await Browser.GetServiceWorkerAsync(target);
            Assert.Equal("[object ServiceWorkerGlobalScope]", await worker.EvaluateAsync<string>("() => self.toString()"));
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should create a worker from a shared worker</playwright-it>
        [Retry]
        public async Task ShouldCreateAWorkerFromASharedWorker()
        {
            var workerTask = Browser.WaitForTargetAsync(t => t.Type == TargetType.SharedWorker);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"() =>
            {
                new SharedWorker('data:text/javascript,console.log(""hi"")');
            }");
            var target = await workerTask;
            var worker = await Browser.GetServiceWorkerAsync(target);
            Assert.Equal("[object SharedWorkerGlobalScope]", await worker.EvaluateAsync<string>("() => self.toString()"));
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should report when a target url changes</playwright-it>
        [Retry]
        public async Task ShouldReportWhenATargetUrlChanges()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            var changedTargetTaskCompletion = new TaskCompletionSource<ITarget>();
            void ChangedTargetEventHandler(object sender, TargetChangedArgs e)
            {
                changedTargetTaskCompletion.SetResult(e.Target);
                Browser.TargetChanged -= ChangedTargetEventHandler;
            }
            Browser.TargetChanged += ChangedTargetEventHandler;

            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/");
            var changedTarget = await changedTargetTaskCompletion.Task;
            Assert.Equal(TestConstants.CrossProcessUrl + "/", changedTarget.Url);

            changedTargetTaskCompletion = new TaskCompletionSource<ITarget>();
            Browser.TargetChanged += ChangedTargetEventHandler;
            await Page.GoToAsync(TestConstants.EmptyPage);
            changedTarget = await changedTargetTaskCompletion.Task;
            Assert.Equal(TestConstants.EmptyPage, changedTarget.Url);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should not report uninitialized pages</playwright-it>
        [Retry]
        public async Task ShouldNotReportUninitializedPages()
        {
            bool targetChanged = false;
            void Listener(object sender, TargetChangedArgs e) => targetChanged = true;
            Browser.TargetChanged += Listener;
            var targetCompletionTask = new TaskCompletionSource<ITarget>();
            void TargetCreatedEventHandler(object sender, TargetChangedArgs e)
            {
                targetCompletionTask.SetResult(e.Target);
                Browser.TargetCreated -= TargetCreatedEventHandler;
            }
            Browser.TargetCreated += TargetCreatedEventHandler;
            var newPageTask = Context.NewPageAsync();
            var target = await targetCompletionTask.Task;
            Assert.Equal(TestConstants.AboutBlank, target.Url);

            var newPage = await newPageTask;
            targetCompletionTask = new TaskCompletionSource<ITarget>();
            Browser.TargetCreated += TargetCreatedEventHandler;
            var evaluateTask = newPage.EvaluateHandleAsync("window.open('about:blank')");
            var target2 = await targetCompletionTask.Task;
            Assert.Equal(TestConstants.AboutBlank, target2.Url);
            await evaluateTask;
            await newPage.CloseAsync();
            Assert.False(targetChanged, "target should not be reported as changed");
            Browser.TargetChanged -= Listener;
        }

        /*
        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should not crash while redirecting if original request was missed</playwright-it>
        [Retry]
        public async Task ShouldNotCrashWhileRedirectingIfOriginalRequestWasMissed()
        {
            var serverResponseEnd = new TaskCompletionSource<bool>();
            var serverResponse = (HttpResponse)null;
            Server.SetRoute("/one-style.css", context => { serverResponse = context.Response; return serverResponseEnd.Task; });
            // Open a new page. Use window.open to connect to the page later.
            await Task.WhenAll(
              Page.EvaluateHandleAsync("url => window.open(url)", TestConstants.ServerUrl + "/one-style.html"),
              Server.WaitForRequest("/one-style.css")
            );
            // Connect to the opened page.
            var target = await Browser.WaitForTargetAsync(t => t.Url.Contains("one-style.html"));
            var newPage = await target.GetPageAsync();
            // Issue a redirect.
            serverResponse.Redirect("/injectedstyle.css");
            serverResponseEnd.SetResult(true);
            // Wait for the new page to load.
            await WaitEventAsync<PageLoadEventFiredChromiumEvent>(((ChromiumPage)((PlaywrightSharp.Page)newPage).Delegate).Client);
            // Cleanup.
            await newPage.CloseAsync();
        }
        */

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should have an opener</playwright-it>
        [Retry]
        public async Task ShouldHaveAnOpener()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var targetCreatedCompletion = new TaskCompletionSource<ITarget>();
            Browser.TargetCreated += (sender, e) => targetCreatedCompletion.TrySetResult(e.Target);
            await Page.GoToAsync(TestConstants.ServerUrl + "/popup/window-open.html");
            var createdTarget = await targetCreatedCompletion.Task;

            Assert.Equal(TestConstants.ServerUrl + "/popup/popup.html", (await createdTarget.GetPageAsync()).Url);
            Assert.Same(Browser.GetPageTarget(Page), createdTarget.Opener);
            Assert.Null(Browser.GetPageTarget(Page).Opener);
        }
    }
}
