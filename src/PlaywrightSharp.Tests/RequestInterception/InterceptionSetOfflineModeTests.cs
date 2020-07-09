using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>Interception.setOfflineMode</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class InterceptionSetOfflineModeTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public InterceptionSetOfflineModeTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Interception.setOfflineMode</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWork()
        {
            await Page.SetOfflineModeAsync(true);
            var exception = Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.GoToAsync(TestConstants.EmptyPage));
            await Page.SetOfflineModeAsync(false);
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Interception.setOfflineMode</playwright-describe>
        ///<playwright-it>should emulate navigator.onLine</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldEmulateNavigatorOnLine()
        {
            Assert.True(await Page.EvaluateAsync<bool>("() => window.navigator.onLine"));
            await Page.SetOfflineModeAsync(true);
            Assert.False(await Page.EvaluateAsync<bool>("() => window.navigator.onLine"));
            await Page.SetOfflineModeAsync(false);
            Assert.True(await Page.EvaluateAsync<bool>("() => window.navigator.onLine"));
        }
    }
}
