using System;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForResponse</playwright-describe>
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class PageWaitForResponseTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForResponseTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForResponse</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (response, _) = await TaskUtils.WhenAll(
                Page.WaitForResponseAsync(TestConstants.ServerUrl + "/digits/2.png"),
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", response.Url);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForResponse</playwright-describe>
        ///<playwright-it>should respect timeout</playwright-it>
        [Fact]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => Page.WaitForEvent(PageEvent.Response, new WaitForEventOptions<ResponseEventArgs>
                {
                    Predicate = _ => false,
                    Timeout = 1
                }));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForResponse</playwright-describe>
        ///<playwright-it>should respect default timeout</playwright-it>
        [Fact]
        public async Task ShouldRespectDefaultTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => Page.WaitForEvent(PageEvent.Response, new WaitForEventOptions<ResponseEventArgs>
                {
                    Predicate = _ => false
                }));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForResponse</playwright-describe>
        ///<playwright-it>should work with predicate</playwright-it>
        [Fact]
        public async Task ShouldWorkWithPredicate()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (responseEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent(PageEvent.Response, new WaitForEventOptions<ResponseEventArgs> { Predicate = e => e.Response.Url == TestConstants.ServerUrl + "/digits/2.png" }),
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", responseEvent.Response.Url);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForResponse</playwright-describe>
        ///<playwright-it>should work with no timeout</playwright-it>
        [Fact]
        public async Task ShouldWorkWithNoTimeout()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (response, _) = await TaskUtils.WhenAll(
                Page.WaitForResponseAsync(TestConstants.ServerUrl + "/digits/2.png", new WaitForOptions { Timeout = 0 }),
                Page.EvaluateAsync<string>(@"() => setTimeout(() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }, 50)")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", response.Url);
        }
    }
}
