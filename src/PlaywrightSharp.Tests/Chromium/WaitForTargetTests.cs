using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/chromium.spec.js</playwright-file>
    ///<playwright-describe>Chromium.waitForTarget</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class WaitForTargetTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WaitForTargetTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.waitForTarget</playwright-describe>
        ///<playwright-it>should wait for a target</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWaitForTarget()
        {
            var context = await NewContextAsync();
            var targetPromise = Browser.WaitForTargetAsync((target) => target.BrowserContext == context && target.Url == TestConstants.EmptyPage);
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var promiseTarget = await targetPromise;
            var targetPage = await promiseTarget.GetPageAsync();
            Assert.Equal(targetPage, page);
            await context.CloseAsync();
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.waitForTarget</playwright-describe>
        ///<playwright-it>should timeout waiting for a non-existent target</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldTimeoutWaitingForNonExistentTarget()
        {
            var context = await NewContextAsync();
            await Assert.ThrowsAsync<TimeoutException>(()
                => Browser.WaitForTargetAsync((target) => target.BrowserContext == context && target.Url == TestConstants.EmptyPage, new WaitForOptions { Timeout = 1 }));
            await context.CloseAsync();
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium.waitForTarget</playwright-describe>
        ///<playwright-it>should fire target events</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldFireTargetEvents()
        {
            var context = await NewContextAsync();
            var events = new List<string>();
            Browser.TargetCreated += (sender, e) => events.Add("CREATED: " + e.Target.Url);
            Browser.TargetChanged += (sender, e) => events.Add("CHANGED: " + e.Target.Url);
            Browser.TargetDestroyed += (sender, e) => events.Add("DESTROYED: " + e.Target.Url);
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.CloseAsync();
            Assert.Equal(new[] {
                $"CREATED: {TestConstants.AboutBlank}",
                $"CHANGED: {TestConstants.EmptyPage}",
                $"DESTROYED: {TestConstants.EmptyPage}"
            }, events);
            await context.CloseAsync();
        }
    }
}
