using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext({bypassCSP})</playwright-describe>
    public class BypassCSPTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public BypassCSPTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({bypassCSP})</playwright-describe>
        ///<playwright-it>should bypass CSP meta tag</playwright-it>
        [Fact]
        public async Task ShouldBypassCSPmetatag()
        {
            // Make sure CSP prohibits addScriptTag.
            var page = await NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await page.AddScriptTagAsync(new AddTagOptions
            {
                Content = "window.__injected = 42;"
            }).ContinueWith(_ => Task.CompletedTask);
            Assert.Null(await page.EvaluateAsync("window.__injected"));

            // By-pass CSP and try one more time.
            page = await NewPageAsync(new BrowserContextOptions { BypassCSP = true });
            await page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await page.AddScriptTagAsync(new AddTagOptions
            {
                Content = "window.__injected = 42;"
            });
            Assert.Equal(42, await page.EvaluateAsync<int>("window.__injected"));
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({bypassCSP})</playwright-describe>
        ///<playwright-it>should bypass CSP header</playwright-it>
        [Fact]
        public async Task ShouldBypassCSPHeader()
        {
            // Make sure CSP prohibits addScriptTag.
            Server.SetCSP("/empty.html", "default-src 'self'");

            var page = await NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.AddScriptTagAsync(new AddTagOptions
            {
                Content = "window.__injected = 42;"
            }).ContinueWith(_ => Task.CompletedTask);
            Assert.Null(await page.EvaluateAsync("window.__injected"));

            // By-pass CSP and try one more time.
            page = await NewPageAsync(new BrowserContextOptions { BypassCSP = true });
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.AddScriptTagAsync(new AddTagOptions
            {
                Content = "window.__injected = 42;"
            });
            Assert.Equal(42, await page.EvaluateAsync<int>("window.__injected"));
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({bypassCSP})</playwright-describe>
        ///<playwright-it>should bypass after cross-process navigation</playwright-it>
        [Fact]
        public async Task ShouldBypassAfterCrossProcessNavigation()
        {
            var page = await NewPageAsync(new BrowserContextOptions { BypassCSP = true });
            await page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await page.AddScriptTagAsync(new AddTagOptions
            {
                Content = "window.__injected = 42;"
            });
            Assert.Equal(42, await page.EvaluateAsync<int>("window.__injected"));

            await page.GoToAsync(TestConstants.CrossProcessUrl + "/csp.html");
            await page.AddScriptTagAsync(new AddTagOptions
            {
                Content = "window.__injected = 42;"
            });
            Assert.Equal(42, await page.EvaluateAsync<int>("window.__injected"));
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({bypassCSP})</playwright-describe>
        ///<playwright-it>should bypass CSP in iframes as well</playwright-it>
        [Fact]
        public async Task ShouldBypassCSPInIframesAsWell()
        {
            var page = await NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            // Make sure CSP prohibits addScriptTag in an iframe.
            var frame = await FrameUtils.AttachFrameAsync(page, "frame1", TestConstants.ServerUrl + "/csp.html");
            await frame.AddScriptTagAsync(new AddTagOptions
            {
                Content = "window.__injected = 42;"
            }).ContinueWith(_ => Task.CompletedTask);
            Assert.Null(await frame.EvaluateAsync<int?>("() => window.__injected"));

            // By-pass CSP and try one more time.
            page = await NewPageAsync(new BrowserContextOptions { BypassCSP = true });
            await page.GoToAsync(TestConstants.EmptyPage);

            // Make sure CSP prohibits addScriptTag in an iframe.
            frame = await FrameUtils.AttachFrameAsync(page, "frame1", TestConstants.ServerUrl + "/csp.html");
            await frame.AddScriptTagAsync(new AddTagOptions
            {
                Content = "window.__injected = 42;"
            }).ContinueWith(_ => Task.CompletedTask);
            Assert.Equal(42, await frame.EvaluateAsync<int?>("() => window.__injected"));
        }
    }
}