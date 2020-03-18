using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mono.Unix;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Helpers.Linux;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/headful.spec.js</playwright-file>
    ///<playwright-describe>ChromiumHeadful</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class HeadfulTests : PlaywrightSharpBrowserBaseTest, IDisposable
    {
        readonly LaunchOptions _extensionOptions;

        /// <inheritdoc/>
        public HeadfulTests(ITestOutputHelper output) : base(output)
        {
            string extensionPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "simple-extension");

            _extensionOptions = TestConstants.GetDefaultBrowserOptions();
            _extensionOptions.Headless = false;
            _extensionOptions.Args = new[]
            {
                $"--disable-extensions-except={extensionPath}",
                $"--load-extension={extensionPath}"
            };
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        ///<playwright-file>chromium/headful.spec.js</playwright-file>
        ///<playwright-describe>ChromiumHeadful</playwright-describe>
        ///<playwright-it>background_page target type should be available</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task BackgroundPageTargetTypeShouldBeAvailable()
        {
            using var browserWithExtension = await Playwright.LaunchAsync(_extensionOptions);
            var page = await browserWithExtension.DefaultContext.NewPageAsync();
            var backgroundPageTarget = await browserWithExtension.WaitForTargetAsync(target => target.Type == TargetType.BackgroundPage);
            await page.CloseAsync();
            await browserWithExtension.CloseAsync();
        }

        ///<playwright-file>chromium/headful.spec.js</playwright-file>
        ///<playwright-describe>ChromiumHeadful</playwright-describe>
        ///<playwright-it>target.page() should return a background_page</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task TargetPageShouldReturnABackgroundPage()
        {
            using var browserWithExtension = await Playwright.LaunchAsync(_extensionOptions);
            var backgroundPageTarget = await browserWithExtension.WaitForTargetAsync(target => target.Type == TargetType.BackgroundPage);
            var page = await backgroundPageTarget.GetPageAsync();

            Assert.Equal(6, await page.EvaluateAsync<int>("() => 2 * 3"));
            Assert.Equal(42, await page.EvaluateAsync<int>("() => window.MAGIC"));
        }

        ///<playwright-file>chromium/headful.spec.js</playwright-file>
        ///<playwright-describe>ChromiumHeadful</playwright-describe>
        ///<playwright-it>OOPIF: should report google.com frame</playwright-it>
        [Fact(Skip = "Ignored on Playwright")]
        public async Task OOPIFShouldReportGoogleComFrame()
        {
            using var browser = await Playwright.LaunchAsync(TestConstants.GetHeadfulOptions());
            var page = await browser.DefaultContext.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.SetRequestInterceptionAsync(true);
            page.Request += async (sender, e) => await e.Request.RespondAsync(
                new ResponseData { Body = "{ body: 'YO, GOOGLE.COM'}" });
            await page.EvaluateHandleAsync(@"() => {
                    const frame = document.createElement('iframe');
                    frame.setAttribute('src', 'https://google.com/');
                    document.body.appendChild(frame);
                    return new Promise(x => frame.onload = x);
                }");
            await page.WaitForSelectorAsync("iframe[src=\"https://google.com/\"]");
            string[] urls = Array.ConvertAll(page.Frames, frame => frame.Url);
            Array.Sort(urls);
            Assert.Equal(new[] { TestConstants.EmptyPage, "https://google.com/" }, urls);
        }

        ///<playwright-file>chromium/headful.spec.js</playwright-file>
        ///<playwright-describe>ChromiumHeadful</playwright-describe>
        ///<playwright-it>OOPIF: should report google.com frame</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldOpenDevtoolsWhenDevtoolsTrueOptionIsGiven()
        {
            var headfulOptions = TestConstants.GetHeadfulOptions();
            headfulOptions.Devtools = true;
            using var browser = await Playwright.LaunchAsync(headfulOptions);
            var context = await browser.NewContextAsync();
            await Task.WhenAll(
                context.NewPageAsync(),
                browser.WaitForTargetAsync(target => target.Url.Contains("devtools://")));
        }
    }
}
